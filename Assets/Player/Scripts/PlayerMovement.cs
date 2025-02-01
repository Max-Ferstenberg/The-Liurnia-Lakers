using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cameraTransform; // Reference to the CameraController's transform (or its parent object if needed)
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f; // (Optional, if jumping is needed)

    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Get input for horizontal movement
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Get camera forward and right vectors, but ignore the y component
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        // Determine movement direction relative to the camera
        Vector3 moveDirection = camForward * moveZ + camRight * moveX;
        moveDirection.Normalize();

        // Move the player using the CharacterController
        controller.Move(moveDirection * speed * Time.deltaTime);

        // Gravity (if needed)
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Ensure a slight downward force keeps the player grounded
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Optional: jump logic can be added here if desired
    }
}