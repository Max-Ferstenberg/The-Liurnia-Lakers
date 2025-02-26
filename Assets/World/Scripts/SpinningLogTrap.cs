using UnityEngine;

public class SpinningLogTrap : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("If checked, the log will only spin and not move along the path.")]
    public bool stationary = false;

    [Header("Movement Settings")]
    [Tooltip("The transform representing the start point of movement.")]
    public Transform bottomPoint;
    [Tooltip("The transform representing the end point of movement.")]
    public Transform topPoint;
    [Tooltip("Speed multiplier for movement.")]
    public float travelSpeed = 1f;
    
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = 360f;

    [Header("Pivot Offset")]
    [Tooltip("Offset from the object's pivot to the desired movement anchor point. Adjust until the log lines up correctly.")]
    public Vector3 pivotOffset;

    private Vector3 initialPos;

    void Start()
    {
        initialPos = transform.position;
    }

    void Update()
    {
        // If the log isn't set to be stationary, update its position
        if (!stationary)
        {
            float t = Mathf.PingPong(Time.time * travelSpeed, 1f);
            Vector3 targetPos = Vector3.Lerp(bottomPoint.position, topPoint.position, t);
            transform.position = targetPos - pivotOffset;
        }
        else
        {
            // Keep the log at its initial position if stationary
            transform.position = initialPos;
        }
        
        // Always rotate the log around its pivot
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}