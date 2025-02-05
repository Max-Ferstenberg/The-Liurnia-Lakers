using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;  // The player transform to follow
    public Vector3 offset = new Vector3(0, 2, -4); // Base offset from the player
    public float mouseSensitivity = 100f;
    public float rotationSmoothTime = 0.1f; // Smoothing factor for rotation
    public float minPitch = -10f;  // Lower limit for looking down
    public float maxPitch = 45f;   // Upper limit for looking up

    private float yaw;    // Horizontal rotation (degrees)
    private float pitch;  // Vertical rotation (degrees)
    private float currentYaw;
    private float currentPitch;
    private float yawSmoothVelocity;
    private float pitchSmoothVelocity;

    void Start()
    {
        // Initialize yaw from the player's current rotation; set an initial pitch value.
        yaw = player.eulerAngles.y;
        pitch = 10f;
        currentYaw = yaw;
        currentPitch = pitch;

        // Lock the cursor for a smoother camera experience.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // Get mouse input (do not use Time.deltaTime here if you want frame-rate independent movement)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Update yaw and pitch based on mouse movement
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Smooth the yaw and pitch for a fluid camera motion
        currentYaw = Mathf.SmoothDamp(currentYaw, yaw, ref yawSmoothVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDamp(currentPitch, pitch, ref pitchSmoothVelocity, rotationSmoothTime);

        // Create a rotation quaternion based on the smoothed yaw and pitch.
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // Compute the desired camera position relative to the player using the rotation and offset.
        Vector3 desiredPosition = player.position + rotation * offset;

        // Add a slight height adjustment based on pitch for realism.
        // When looking up (pitch high) the camera can lower a bit; when looking down, it can raise slightly.
        float heightAdjustment = Mathf.Lerp(0.5f, -0.5f, Mathf.InverseLerp(minPitch, maxPitch, currentPitch));
        desiredPosition.y += heightAdjustment;

        // Update the camera's position and rotation.
        transform.position = desiredPosition;
        // Make the camera look at a point slightly above the player's position (e.g., at head height).
        Vector3 lookAtPoint = player.position + Vector3.up * 1.5f;
        transform.LookAt(lookAtPoint);
    }
}