using UnityEngine;
using System.Collections;

public class Elevator : MonoBehaviour
{
    [Header("Timing Settings")]
    [Tooltip("Time (in seconds) after which Sparks and Sides become active.")]
    public float sparksActivationTime = 1.5f;
    [Tooltip("Time (in seconds) after which the platform moves.")]
    public float moveTriggerTime = 2.0f;
    
    [Header("Movement Settings")]
    [Tooltip("The distance the platform moves upward (or downward) when activated.")]
    public float moveDistance = 5f;
    [Tooltip("The time (in seconds) it takes for the platform to move.")]
    public float climbTime = 1f;

    [Header("Magic Effect Child References")]
    [Tooltip("Reference to the Sparks child object (should be inactive by default).")]
    public GameObject sparks;
    [Tooltip("Reference to the Sides child object (should be inactive by default).")]
    public GameObject sides;

    // Internal state.
    private bool playerOnPlatform = false;
    private float timeOnPlatform = 0f;
    private bool isAtTop = false;
    private bool isMoving = false;
    private Vector3 originalPosition;
    private Transform playerTransform;

    // Flag to mark that the player has exited while the elevator is at the top.
    private bool hasExitedAtTop = false;
    // Controls whether lighting effects should activate.
    private bool shouldActivateEffects = false;

    void Start()
    {
        originalPosition = transform.position;
        if (sparks != null) sparks.SetActive(false);
        if (sides != null) sides.SetActive(false);
    }

    // Called by the ElevatorTrigger child when the player steps on the elevator.
    public void PlayerEntered(Collider other)
    {
        playerOnPlatform = true;
        timeOnPlatform = 0f;
        playerTransform = other.transform;
        playerTransform.parent = transform;
        // Allow lighting effects to activate when the player enters.
        shouldActivateEffects = true;
    }

    // Called by the ElevatorTrigger child when the player steps off the elevator.
    public void PlayerExited(Collider other)
    {
        playerOnPlatform = false;
        timeOnPlatform = 0f;
        if (playerTransform != null && playerTransform.parent == transform)
            playerTransform.parent = null;
        playerTransform = null;

        // If the elevator is at the top, note that the player has left.
        if (isAtTop)
            hasExitedAtTop = true;
        
        // Only turn off lighting effects if the elevator is not moving.
        if (!isMoving)
        {
            if (sparks != null) sparks.SetActive(false);
            if (sides != null) sides.SetActive(false);
        }
        // Prevent effects from reactivating until the next entry.
        shouldActivateEffects = false;
    }

    void Update()
    {
        // Only update if the player is on the platform and the elevator isn't moving.
        if (playerOnPlatform && !isMoving)
        {
            timeOnPlatform += Time.deltaTime;
            
            // Activate effects if allowed and the delay has passed.
            if (shouldActivateEffects && timeOnPlatform >= sparksActivationTime)
            {
                if (sparks != null && !sparks.activeSelf)
                    sparks.SetActive(true);
                if (sides != null && !sides.activeSelf)
                    sides.SetActive(true);
            }
            
            // Ascend if at the bottom.
            if (!isAtTop && timeOnPlatform >= moveTriggerTime)
            {
                StartCoroutine(MovePlatform(transform.position, originalPosition + Vector3.up * moveDistance, climbTime));
                isAtTop = true;
                timeOnPlatform = 0f;
            }
            // Descend only if at the top and the player had exited before re-entering.
            else if (isAtTop && hasExitedAtTop && timeOnPlatform >= moveTriggerTime)
            {
                StartCoroutine(MovePlatform(transform.position, originalPosition, climbTime));
                isAtTop = false;
                hasExitedAtTop = false;
                timeOnPlatform = 0f;
            }
        }
    }

    // Coroutine to move the platform smoothly.
    IEnumerator MovePlatform(Vector3 startPos, Vector3 endPos, float duration)
    {
        isMoving = true;
        
        // Ensure that the lighting effects are on while the elevator is moving.
        if (sparks != null) sparks.SetActive(true);
        if (sides != null) sides.SetActive(true);

        float elapsed = 0f;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody found on the elevator. Please add one and set it to Kinematic.");
            yield break;
        }
        
        while (elapsed < duration)
        {
            Vector3 newPos = Vector3.Lerp(startPos, endPos, elapsed / duration);
            rb.MovePosition(newPos);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.MovePosition(endPos);
        isMoving = false;
        timeOnPlatform = 0f;
        
        // Once movement is complete, turn off the lighting effects.
        if (sparks != null) sparks.SetActive(false);
        if (sides != null) sides.SetActive(false);
        shouldActivateEffects = false;
    }
}
