using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Powerup : MonoBehaviour
{

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer == 3)
        {
            collision.gameObject.GetComponent<RacerBehaviour>().IncreaseSpeed();
            Destroy(gameObject);
        }
    }
}
