using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    
    public Transform player;
    public Vector3 offset = new Vector3(0, 2, -4);
    public float mouseSensitivity = 100f;
    public float rotationSmoothTime = 0.1f;
    public float minPitch = -90f;
    public float maxPitch = 90f;

    
    private float yaw;
    private float pitch;
    private float currentYaw;
    private float currentPitch;
    private float yawSmoothVelocity;
    private float pitchSmoothVelocity;
    private bool cameraLocked = false;

    
    void Start()
    {
        yaw = player.eulerAngles.y;
        pitch = 10f;
        currentYaw = yaw;
        currentPitch = pitch;

        // Lock the cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    
    void LateUpdate()
    {
        if (!cameraLocked)
        {
            // Capture mouse input scaled by sensitivity and frame time
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yaw += mouseX;
            pitch -= mouseY;
            //Clamp pitch to prevent excessive vertical rotation
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            //interpolate the current yaw and pitch toward the target values based on mouse position
            currentYaw = Mathf.SmoothDamp(currentYaw, yaw, ref yawSmoothVelocity, rotationSmoothTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, pitch, ref pitchSmoothVelocity, rotationSmoothTime);

            //Build a rotation from the smoothed pitch and yaw values
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

            //Calculate the desired camera position by applying the offset rotated with the computed rotation
            Vector3 desiredPosition = player.position + rotation * offset;

            //Adjust camera height based on the current pitch
            float heightAdjustment = Mathf.Lerp(0.5f, -0.5f, Mathf.InverseLerp(minPitch, maxPitch, currentPitch));
            desiredPosition.y += heightAdjustment;

            // Define a point slightly above the player (eye-level) as the start of the linecast
            Vector3 fromPosition = player.position + Vector3.up * 1.5f;
            RaycastHit hit;
            // Set finalPosition to desiredPosition, but adjust if a collision is detected
            Vector3 finalPosition = desiredPosition;
            // Perform a linecast between the player's eye-level and the desired camera position
            if (Physics.Linecast(fromPosition, desiredPosition, out hit, ~0, QueryTriggerInteraction.Ignore))
            {
                // If an obstruction is detected, set the camera position to just outside the obstacle
                finalPosition = hit.point + hit.normal * 0.3f;
            }
            transform.position = finalPosition;
            // Make the camera look at a point above the player's position
            Vector3 lookAtPoint = player.position + Vector3.up * 1.5f;
            transform.LookAt(lookAtPoint);
        }
    }
    
    //Called when the player dies
    public void FallDeathCam()
    {
        cameraLocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //Retain the camera's current position
        Vector3 lockedPosition = transform.position;
        transform.position = lockedPosition;
        //Disable mouse sensitivity to freeze camera rotation
        mouseSensitivity = 0f;
        //Begin a coroutine to smoothly rotate the camera to focus on the player
        StartCoroutine(LookToPlayer());
    }

    //Coroutine to interpolate the camera's rotation to look at the player
    private IEnumerator LookToPlayer()
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        //Define target a point at the player's eye-level
        Vector3 targetPosition = player.position + Vector3.up * 1.5f;
        //Calculate the rotation needed to look from the camera's position to the player
        Quaternion targetRotation = Quaternion.LookRotation(player.position - transform.position);

        float time = 0f;
        float duration = 1f; 

        //interpolate rotation over the defined duration
        while (time < duration)
        {
            time += Time.deltaTime;
            //the camera position from the player is kept constant
            transform.position = Vector3.Lerp(startPosition, startPosition, time / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, time / duration);
            yield return null;
        }
    }

    //Coroutine stub for a mode to continuously follow the player while the camera remains locked
    //it maintains the offset and keeps looking at the player
    private IEnumerator FollowPlayer()
    {
        Vector3 offsetSmooth = offset;
        //update the camera's position and orientation
        while (cameraLocked)
        {
            Vector3 targetPosition = player.position + offsetSmooth;
            transform.LookAt(player.position + Vector3.up * 1.5f);
            yield return null;
        }
    }
}
