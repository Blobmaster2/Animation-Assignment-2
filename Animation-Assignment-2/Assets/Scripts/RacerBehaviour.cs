using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RacerBehaviour : MonoBehaviour
{
    Rigidbody rb;
    List<Transform> points = new List<Transform>();
    public float speed = 1.0f;
    [Range(1, 32)]
    public int sampleRate = 16;
    public AnimationCurve veerStrength;
    public int id;
    public float speedMultiplier = 1;

    bool updatedVelocity;
    bool isVeering = false;
    bool catmullEnabled = true;

    float timeSinceVeer;
    float maxVeerTime;
    float timeSinceVelocityUpdate;

    [System.Serializable]
    class SamplePoint
    {
        public float samplePosition;
        public float accumulatedDistance;

        public SamplePoint(float samplePosition, float distanceCovered)
        {
            this.samplePosition = samplePosition;
            this.accumulatedDistance = distanceCovered;
        }
    }
    //list of segment samples makes it easier to index later
    //imagine it like List<SegmentSamples>, and segment sample is a list of SamplePoints
    List<List<SamplePoint>> table = new List<List<SamplePoint>>();

    float distance = 0f;
    float accumDistance = 0f;
    public int currentIndex = 0;
    public int currentSample = 0;

    private void Start()
    {
        StartCoroutine(WaitForVeer());

        ChangeColor();

        for (int i = 0; i < GameObject.Find("Track points").transform.childCount; i++)
        {
            points.Add(GameObject.Find("Track points").transform.GetChild(i).transform);
        }

        rb = GetComponent<Rigidbody>();

        //make sure there are 4 points, else disable the component
        if (points.Count < 4)
        {
            enabled = false;
        }

        int size = points.Count;
        //calculate the speed graph table
        Vector3 prevPos = points[0].position;

        for (int i = 0; i < size; i++)
        {
            List<SamplePoint> segment = new List<SamplePoint>();
            //calculate samples
            segment.Add(new SamplePoint(0f, accumDistance));

            for (int sample = 1; sample <= sampleRate; sample++)
            {
                float t = sample / (float)sampleRate;
                Vector3 currentPos = CatmullRomFunc(
                    points[(i - 1 + points.Count) % points.Count].position, 
                    points[i].position, 
                    points[(i + 1 + points.Count) % points.Count].position, 
                    points[(i + 2 + points.Count) % points.Count].position, 
                    t
                    );

                float tDistance = Vector3.Magnitude(prevPos - currentPos);
                prevPos = currentPos;

                accumDistance += tDistance;

                segment.Add(new SamplePoint(t, accumDistance));
            }
            table.Add(segment);
        }
    }

    private void ChangeColor()
    {
        Material instanceMaterial = new Material(GetComponent<MeshRenderer>().sharedMaterial);
        GetComponent<MeshRenderer>().material = instanceMaterial;

        int r = Random.Range(0, 256);
        int g = Random.Range(0, 256);
        int b = Random.Range(0, 256);

        instanceMaterial.color = new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    private void Update()
    {
        Vector3 velocityDir = rb.velocity.normalized;
        transform.rotation = Quaternion.Euler(90, Mathf.Atan2(velocityDir.x, velocityDir.z) * 180 / Mathf.PI, 0);

        UpdateCatmullTrack();

        if (isVeering)
        {
            Veer();
            timeSinceVeer += Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        if (rb.velocity.magnitude > speed * speedMultiplier)
        {
            rb.AddForce(-rb.velocity);
        }
    }

    private void UpdateCatmullTrack()
    {
        Vector3 currentPos = transform.position;
        int size = points.Count;
        distance += rb.velocity.magnitude * Time.smoothDeltaTime;

        while (distance > table[currentIndex]
            [currentSample].accumulatedDistance)
        {
            if (currentSample >= sampleRate - 1)
            {
                currentSample = 0;
                currentIndex++;
            }

            if (currentIndex >= size)
            {
                currentIndex = 0;
                currentSample = -1;
                distance = 0;
            }

            updatedVelocity = false;
            currentSample++;
        }

        Vector3 p0 = points[(currentIndex - 1 + points.Count) % points.Count].position;
        Vector3 p1 = points[currentIndex].position;
        Vector3 p2 = points[(currentIndex + 1) % points.Count].position;
        Vector3 p3 = points[(currentIndex + 2) % points.Count].position;

        if (!updatedVelocity)
        {
            Vector3 catmullVec = CatmullRomFunc(p0, p1, p2, p3, GetAdjustedT());

            if (catmullEnabled) rb.AddForce((catmullVec - currentPos).normalized * speed * Vector3.Distance(catmullVec, currentPos) * speedMultiplier);
            updatedVelocity = true;
            timeSinceVelocityUpdate = 0;
        }
        else
        {
            timeSinceVelocityUpdate += Time.deltaTime;
        }

        if (timeSinceVelocityUpdate > 0.25f)
        {
            updatedVelocity = false;
        }

        rb.AddRelativeForce(Vector3.forward * speed);
    }

    float GetAdjustedT()
    {
        SamplePoint current = table[currentIndex][currentSample];
        SamplePoint next;

        if (currentSample + 1 >= sampleRate - 1)
        {
            if (currentIndex + 1 >= points.Count)
            {
                next = table[0][0];
            }
            else
            {
                next = table[currentIndex + 1][0];
            }
        }
        else
        {
            next = table[currentIndex][currentSample + 1];
        }

        return Mathf.Lerp(current.samplePosition, next.samplePosition,
            (distance - current.accumulatedDistance) / (next.accumulatedDistance - current.accumulatedDistance)
        );
    }


    private void OnDrawGizmos()
    {
        Vector3 a, b, p0, p1, p2, p3;
        for (int i = 0; i < points.Count; i++)
        {
            a = points[i].position;
            p0 = points[(points.Count + i - 1) % points.Count].position;
            p1 = points[i].position;
            p2 = points[(i + 1) % points.Count].position;
            p3 = points[(i + 2) % points.Count].position;
            for (int j = 1; j <= sampleRate; ++j)
            {
                b = CatmullRomFunc(p0, p1, p2, p3, (float)j / sampleRate);
                Gizmos.DrawLine(a, b);
                a = b;
            }
        }
    }

    Vector3 CatmullRomFunc(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;
        return 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
    }

    void Veer()
    {
        bool isRight = Random.Range(0, 2) == 0 ? false : true;

        Vector3 dir = isRight ? Vector3.right : Vector3.left;

        rb.AddRelativeForce(dir * veerStrength.Evaluate(timeSinceVeer / maxVeerTime) * speed);

        Debug.Log(dir);
    }

    IEnumerator WaitForVeer()
    {
        yield return new WaitForSeconds(Random.Range(6, 10));

        StartCoroutine(WaitForStopVeer());
    }

    IEnumerator WaitForStopVeer()
    {
        isVeering = true;
        maxVeerTime = Random.Range(2, 4);

        yield return new WaitForSeconds(maxVeerTime);

        isVeering = false;
        StartCoroutine(WaitForVeer());
    }

    IEnumerator WaitForReenableCatmull()
    {
        catmullEnabled = false;

        yield return new WaitForSeconds(1f / (speed / 7.5f));

        catmullEnabled = true;
    }

    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.layer == 3)
        {
            EvadeRacers(collision);
        }

        if (collision.gameObject.layer == 9)
        {
            SeekPowerup(collision);

            if (catmullEnabled)
            {
                StartCoroutine(WaitForReenableCatmull());
            }
        }

        if (collision.gameObject.layer == 8)
        {
            EvadeObstacles(collision);

            if (catmullEnabled)
            {
                StartCoroutine(WaitForReenableCatmull());
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.layer == 9 || collision.gameObject.layer == 8)
        {
            catmullEnabled = true;
        }
    }

    void SeekPowerup(Collider powerup)
    {
        float distance = Vector3.Distance(transform.position, powerup.transform.position);
        Vector3 direction = (powerup.transform.position - transform.position).normalized;
        rb.AddForce(direction * speed);
    }

    void EvadeRacers(Collider racer)
    {
        Vector3 direction = (transform.position - racer.transform.position).normalized;
        rb.AddForce(direction * speed / Vector3.Distance(transform.position, racer.transform.position));
    }

    void EvadeObstacles(Collider obstacle)
    {
        Vector3 direction;
        if (Vector3.Cross(transform.up.normalized, (transform.position - obstacle.gameObject.transform.position).normalized).y > 0)
        {
            direction = Vector3.right;
            Debug.Log("turning right");
        }
        else
        {
            direction = Vector3.left;
            Debug.Log("turning left");
        }

        rb.AddRelativeForce(direction * speed);
    }
}
