using UnityEngine;
using System.Collections;

public class SmoothFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float velocity;

    private float zOffset;
    private Vector3 smoothing;
    private Vector3 goal;

    void Start()
    {
        zOffset = transform.position.z;
    }

    void LateUpdate()
    {
        goal = Vector3.SmoothDamp(transform.position, target.position, ref smoothing, velocity);
        goal.z = zOffset;

        transform.position = goal;
    }
}