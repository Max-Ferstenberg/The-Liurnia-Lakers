using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player; 
    public Vector3 offset = new Vector3(0, 2, -4); 
    public float mouseSensitivity = 100f;
    public float rotationSmoothTime = 0.1f; 
    public float minPitch = -10f;  
    public float maxPitch = 45f; 

    private float yaw; 
    private float pitch; 
    private float currentYaw;
    private float currentPitch;
    private float yawSmoothVelocity;
    private float pitchSmoothVelocity;

    void Start()
    {
        yaw = player.eulerAngles.y;
        pitch = 10f;
        currentYaw = yaw;
        currentPitch = pitch;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        currentYaw = Mathf.SmoothDamp(currentYaw, yaw, ref yawSmoothVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDamp(currentPitch, pitch, ref pitchSmoothVelocity, rotationSmoothTime);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        Vector3 desiredPosition = player.position + rotation * offset;

        float heightAdjustment = Mathf.Lerp(0.5f, -0.5f, Mathf.InverseLerp(minPitch, maxPitch, currentPitch));
        desiredPosition.y += heightAdjustment;

        transform.position = desiredPosition;
        Vector3 lookAtPoint = player.position + Vector3.up * 1.5f;
        transform.LookAt(lookAtPoint);
    }
}