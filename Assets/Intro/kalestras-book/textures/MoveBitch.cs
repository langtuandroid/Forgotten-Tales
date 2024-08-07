using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBitch : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    public Axis moveAxis = Axis.X; // Default to moving along the X axis
    public float speed = 1.0f; // Speed of movement

    void Update()
    {
        Vector3 direction = Vector3.zero;

        switch (moveAxis)
        {
            case Axis.X:
                direction = Vector3.right;
                break;
            case Axis.Y:
                direction = Vector3.up;
                break;
            case Axis.Z:
                direction = Vector3.forward;
                break;
        }

        transform.Translate(direction * speed * Time.deltaTime);
    }
}
