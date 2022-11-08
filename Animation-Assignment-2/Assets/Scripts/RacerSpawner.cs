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
            Instantiate(racerPrefab, start.position, Quaternion.Euler(90, 0, 0));
        }
    }
}
