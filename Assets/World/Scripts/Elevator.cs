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

    private bool playerOnPlatform = false;
    private float timeOnPlatform = 0f;
    private bool isAtTop = false;
    private bool isMoving = false;
    private Vector3 originalPosition;
    private Transform playerTransform;

    private bool hasExitedAtTop = false;
    private bool shouldActivateEffects = false;

    void Start()
    {
        originalPosition = transform.position;
        if (sparks != null) sparks.SetActive(false);
        if (sides != null) sides.SetActive(false);
    }

    // Triggered when the player enters the elevator trigger collider (handled in a different script)
    public void PlayerEntered(Collider other)
    {
        playerOnPlatform = true;
        timeOnPlatform = 0f;
        playerTransform = other.transform;
        playerTransform.parent = transform; //Parent the player to the elevator
        shouldActivateEffects = true; //activation of effects
    }

    public void PlayerExited(Collider other)
    {
        playerOnPlatform = false;
        timeOnPlatform = 0f; // Reset timer on exit
        if (playerTransform != null && playerTransform.parent == transform)
            playerTransform.parent = null; //Detach player from elevator
        playerTransform = null;

        if (isAtTop) //Mark that the player exited at the top
            hasExitedAtTop = true;
        
        //Disable effects if the elevator is stationary
        if (!isMoving)
        {
            if (sparks != null) sparks.SetActive(false);
            if (sides != null) sides.SetActive(false);
        }
        shouldActivateEffects = false; //Prevent further effect activation until re-entry
    }

    void Update()
    {
        if (playerOnPlatform && !isMoving)
        {
            timeOnPlatform += Time.deltaTime;
            
            if (shouldActivateEffects && timeOnPlatform >= sparksActivationTime)
            {
                if (sparks != null && !sparks.activeSelf)
                    sparks.SetActive(true);
                if (sides != null && !sides.activeSelf)
                    sides.SetActive(true);
            }
            
            //Initiate upward movement if at the bottom and time threshold met
            if (!isAtTop && timeOnPlatform >= moveTriggerTime)
            {
                StartCoroutine(MovePlatform(transform.position, originalPosition + Vector3.up * moveDistance, climbTime));
                isAtTop = true;
                timeOnPlatform = 0f;
            }
            //Initiate downward movement if at the top, player exited, and time threshold met
            else if (isAtTop && hasExitedAtTop && timeOnPlatform >= moveTriggerTime)
            {
                StartCoroutine(MovePlatform(transform.position, originalPosition, climbTime));
                isAtTop = false;
                hasExitedAtTop = false;
                timeOnPlatform = 0f;
            }
        }
    }

    //interpolates the platform's position between two points over a set duration
    IEnumerator MovePlatform(Vector3 startPos, Vector3 endPos, float duration)
    {
        isMoving = true;
        
        //visual effects during movement
        if (sparks != null) sparks.SetActive(true);
        if (sides != null) sides.SetActive(true);

        float elapsed = 0f;
        Rigidbody rb = GetComponent<Rigidbody>();
        
        //linear interpolation between start and end positions
        while (elapsed < duration)
        {
            Vector3 newPos = Vector3.Lerp(startPos, endPos, elapsed / duration);
            rb.MovePosition(newPos);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.MovePosition(endPos); //final position
        isMoving = false;
        timeOnPlatform = 0f;
        
        //Deactivate visual effects after movement
        if (sparks != null) sparks.SetActive(false);
        if (sides != null) sides.SetActive(false);
        shouldActivateEffects = false;
    }
}