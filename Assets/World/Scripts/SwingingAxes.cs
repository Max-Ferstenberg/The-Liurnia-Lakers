using UnityEngine;

public class SwingingAxes : MonoBehaviour
{
    // -------------------- Inspector Settings --------------------
    
    [Header("Axe Settings")]
    [Tooltip("The axes (in order: Axe1, Axe2, Axe3, Axe4) that will swing.")]
    public Transform[] axes = new Transform[4];

    [Tooltip("Swing amplitude in degrees")]
    public float amplitude = 60f;

    [Tooltip("Swing speed mult")]
    public float swingSpeed = 2f;

    [Tooltip("Local axis around which the axes swing. (0 = X, 1 = Y, 2 = Z)")]
    public int rotationAxis = 1;

    
    //initial local rotations of each axe
    private Quaternion[] initialRotations;


    void Start()
    {
        //Initialize array to hold rotations
        initialRotations = new Quaternion[axes.Length];
        for (int i = 0; i < axes.Length; i++)
        {
            //Store starting local rotation
            initialRotations[i] = axes[i].localRotation;
        }
    }

    void Update()
    {
        float t = Time.time * swingSpeed;

        //Group 1 uses the basic sine function
        float angleGroup1 = amplitude * Mathf.Sin(t);
        //Group 2 is phase-shifted by PI (i.e., 180 degrees) to swing in the opposite direction
        float angleGroup2 = amplitude * Mathf.Sin(t + Mathf.PI);

        //Two Euler angle vectors for rotation adjustments
        Vector3 eulerGroup1 = Vector3.zero;
        Vector3 eulerGroup2 = Vector3.zero;

        switch (rotationAxis)
        {
            case 0:
                eulerGroup1.x = angleGroup1;
                eulerGroup2.x = angleGroup2;
                break;
            case 1:
                eulerGroup1.y = angleGroup1;
                eulerGroup2.y = angleGroup2;
                break;
            case 2:
            default:
                eulerGroup1.z = angleGroup1;
                eulerGroup2.z = angleGroup2;
                break;
        }

        axes[0].localRotation = initialRotations[0] * Quaternion.Euler(eulerGroup1);
        axes[2].localRotation = initialRotations[2] * Quaternion.Euler(eulerGroup1);

        axes[1].localRotation = initialRotations[1] * Quaternion.Euler(eulerGroup2);
        axes[3].localRotation = initialRotations[3] * Quaternion.Euler(eulerGroup2);
    }
}