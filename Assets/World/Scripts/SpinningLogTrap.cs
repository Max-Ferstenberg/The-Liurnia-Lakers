using UnityEngine;

public class SpinningLogTrap : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The transform representing the bottom point (only its Y position is used).")]
    public Transform bottomPoint;
    [Tooltip("The transform representing the top point (only its Y position is used).")]
    public Transform topPoint;
    [Tooltip("Speed multiplier for vertical movement.")]
    public float travelSpeed = 1f;
    
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = 360f;

    private Vector3 initialPos;

    void Start()
    {
        initialPos = transform.position;
    }

    void Update()
    {
        float t = Mathf.PingPong(Time.time * travelSpeed, 1f);
        float newY = Mathf.Lerp(bottomPoint.position.y, topPoint.position.y, t);
        transform.position = new Vector3(initialPos.x, newY, initialPos.z);
        
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}