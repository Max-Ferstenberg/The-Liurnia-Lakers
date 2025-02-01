using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;            // Reference to the player
    public float mouseSensitivity = 200f;

    // Vertical rotation value (controlled by mouse Y movement)
    // Clamped between -59 (looking down) and 59 (looking up)
    private float xRotation = 0f;

    // Arc endpoint parameters (tweak these in the Inspector)
    [Header("Arc Endpoints")]
    public float topHeight = 5f;          // When looking straight down, camera is directly above the player
    public float bottomHeight = 0.5f;     // When looking straight up, camera is near the player's feet
    public float backDistance = 4.5f;     // How far behind the player the camera sits at the bottom end of the arc

    // Control point tweak: adjusts the arc's curvature in a neutral position.
    [Header("Arc Control")]
    public float controlMultiplier = 0.75f; // Multiplies the backDistance for the control point's z offset

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Read mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Update vertical rotation and clamp to -59° to 59°
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -59f, 59f);

        // Map xRotation to a normalized parameter t in [0, 1]
        // t = 0 when xRotation == -59 (looking down) and t = 1 when xRotation == 59 (looking up)
        float t = Mathf.InverseLerp(-59f, 59f, xRotation);
        // Smooth the transition near the extremes to help avoid snapping
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        // Define the Bézier endpoints (relative to the player)
        Vector3 P0 = new Vector3(0, topHeight, 0);                    // When looking down: camera directly above the player
        Vector3 P2 = new Vector3(0, bottomHeight, -backDistance);         // When looking up: camera near the player's feet, behind them

        // Define the control point to shape the arc.
        Vector3 P1 = new Vector3(0, Mathf.Lerp(topHeight, bottomHeight, 0.5f), -backDistance * controlMultiplier);

        // Compute the quadratic Bézier offset using smoothT:
        // B(t) = (1-t)² * P0 + 2*(1-t)*t * P1 + t² * P2
        Vector3 offset = Mathf.Pow(1 - smoothT, 2) * P0 +
                         2 * (1 - smoothT) * smoothT * P1 +
                         Mathf.Pow(smoothT, 2) * P2;

        // Rotate the player horizontally based on mouseX
        player.Rotate(Vector3.up * mouseX);

        // Transform the offset by the player's orientation so that the arc follows the player
        Vector3 desiredPosition = player.position + player.TransformDirection(offset);
        transform.position = desiredPosition;

        // Compute look direction for the camera
        Vector3 lookDir = player.position - transform.position;
        // If the look direction is nearly vertical (i.e. its x and z are near zero), choose an alternate up vector
        Vector3 upVector = Vector3.up;
        if (Mathf.Abs(lookDir.x) < 0.01f && Mathf.Abs(lookDir.z) < 0.01f)
        {
            // Use the player's forward as an alternative "up" to avoid gimbal issues
            upVector = player.forward;
        }
        transform.rotation = Quaternion.LookRotation(lookDir, upVector);
    }
}