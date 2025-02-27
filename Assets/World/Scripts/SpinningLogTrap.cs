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
    public float travelSpeed = 1f;  //rate of traversal
    
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = 360f;

    [Header("Pivot Offset")]
    [Tooltip("Offset from the object's pivot to the desired movement anchor point. Adjust until the log lines up correctly.")]
    public Vector3 pivotOffset;   //Corrects the object's pivot to align with the intended anchor point

    private Vector3 initialPos;

    void Start()
    {
        initialPos = transform.position;
    }

    void Update()
    {
        if (!stationary)
        {
            //oscillates between 0 and 1 for speed
            float t = Mathf.PingPong(Time.time * travelSpeed, 1f);
            //interpolate between bottomPoint and topPoint using the parameter
            Vector3 targetPos = Vector3.Lerp(bottomPoint.position, topPoint.position, t);
            transform.position = targetPos - pivotOffset;
        }
        else
        {
            //maintain the initial recorded position if stationary is checked (used for pit traps)
            transform.position = initialPos;
        }
        
        //rotation about the object's local Y-axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}