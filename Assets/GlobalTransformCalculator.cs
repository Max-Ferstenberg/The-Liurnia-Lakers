using UnityEngine;

public class GlobalTransformCalculator : MonoBehaviour
{
    // Public fields to view in the Inspector
    public Vector3 globalPosition;
    public Quaternion globalRotation;
    public Vector3 globalScale;

    void Start()
    {
        // Calculate global position/rotation/scale without altering the object
        CalculateGlobalTransform();
    }

    void Update()
    {
        // Optional: Update in real-time (e.g., for moving parents)
        CalculateGlobalTransform();
    }

    void CalculateGlobalTransform()
    {
        // 1. Calculate Global Position
        globalPosition = transform.parent != null 
            ? transform.parent.TransformPoint(transform.localPosition) 
            : transform.localPosition;

        // 2. Calculate Global Rotation
        globalRotation = transform.parent != null 
            ? transform.parent.rotation * transform.localRotation 
            : transform.localRotation;

        // 3. Calculate Global Scale
        globalScale = transform.parent != null 
            ? Vector3.Scale(transform.parent.lossyScale, transform.localScale) 
            : transform.localScale;
    }
}