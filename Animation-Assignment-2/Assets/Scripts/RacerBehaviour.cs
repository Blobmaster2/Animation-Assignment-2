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

    bool updatedVelocity;
    bool isVeering = false;

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
    int currentIndex = 0;
    int currentSample = 0;

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

        //transform.up = rb.velocity;

        if (isVeering)
        {
            Veer();
            timeSinceVeer += Time.deltaTime;
        }
    }

    private void UpdateCatmullTrack()
    {
        Vector3 currentPos = transform.position;
        int size = points.Count;
        distance += speed * Time.smoothDeltaTime;

        while (distance > table[currentIndex][currentSample].accumulatedDistance)
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
            rb.velocity = (CatmullRomFunc(p0, p1, p2, p3, GetAdjustedT()) - currentPos).normalized * speed;
            updatedVelocity = true;
            timeSinceVelocityUpdate = 0;
        }
        else
        {
            timeSinceVelocityUpdate += Time.deltaTime;
        }

        if (timeSinceVelocityUpdate > 0.5f)
        {
            updatedVelocity = false;
        }
    }

    float GetAdjustedT()
    {
        SamplePoint current = table[currentIndex][currentSample];
        SamplePoint next = table[currentIndex][currentSample + 1];

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
        rb.AddForce(new Vector3(Random.Range(rb.velocity.normalized.x - 0.5f, rb.velocity.normalized.x + 0.5f), 0, Random.Range(rb.velocity.normalized.z - 0.5f, rb.velocity.normalized.z + 0.5f)).normalized
            * veerStrength.Evaluate(timeSinceVeer / maxVeerTime));
    }

    IEnumerator WaitForVeer()
    {
        yield return new WaitForSeconds(Random.Range(6, 10));

        currentSample += 2;

        if (currentSample >= sampleRate - 1)
        {
            currentSample = 0;
            currentIndex++;
        }

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

    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.layer == 3)
        {
            Vector3 direction = transform.position - collision.transform.position;

            rb.AddForce(direction / Vector3.Distance(transform.position, collision.transform.position) * speed/3);

            Debug.Log($"{id} colliding with {collision.GetComponent<RacerBehaviour>().id}, applying force {direction / Vector3.Distance(transform.position, collision.transform.position) * 2}");
        }
    }
}
