using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RacerSpawner : MonoBehaviour
{
    public int racerCount;
    public GameObject racerPrefab;
    public Material racerMaterial;
    public Transform start;

    void Awake()
    {
        for (int i = 0; i < racerCount; i++)
        {
            var racer = Instantiate(racerPrefab, start.position + new Vector3(i, 0, 0), Quaternion.Euler(90, 0, 0));
            racer.GetComponent<RacerBehaviour>().id = i;
        }
    }
}
