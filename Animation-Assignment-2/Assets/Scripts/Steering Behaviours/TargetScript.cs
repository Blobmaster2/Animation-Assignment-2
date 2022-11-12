using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetScript : MonoBehaviour
{
    public GameObject seekObject;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        rb.velocity = Vector3.right * Mathf.Cos(Time.realtimeSinceStartup) * 10 + Vector3.up * Mathf.Sin(Time.realtimeSinceStartup) * 10;
    }
}
