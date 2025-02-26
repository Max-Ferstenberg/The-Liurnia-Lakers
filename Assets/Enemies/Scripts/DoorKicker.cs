using UnityEngine;
using System.Collections;

public class DoorKicker : MonoBehaviour
{
    // --- Animation & Timing ---
    public Animator anim;
    public float kickDelay = 0.3f;          // Delay after trigger before starting the kick

    // --- Damage Settings ---
    public float kickDamage = 30f;
    public float knockbackForce = 10f;      // Extra knockback force (if needed)

    // --- Burst (Jump + Forward) Settings ---
    public float kickBurstSpeed = 10f;        // The forward burst speed
    public float kickBurstDuration = 0.3f;      // Duration of the burst
    public float jumpForce = 5f;              // Upward force for the jump
    private bool isBursting = false;

    // --- Damage Zone Settings ---
    public float damageRadius = 1.0f;         // Radius for detecting the player during kick
    // Damage offset relative to enemy's position (in local space). 
    // For example, (0, 0, 1) will check 1 unit in front of the enemy.
    public Vector3 damageOffset = new Vector3(0, 0, 1.0f);

    // --- Control Flags ---
    private bool isKicking = false;           // True while the kick animation is active
    private bool kickTriggered = false;       // Prevent re-triggering until reset
    private bool hasCollidedWithPlayer = false; // Ensure damage is applied only once per kick

    // Reference to our Rigidbody
    private Rigidbody rb;
    // Cache the player that triggered the kick (if needed).
    private GameObject player;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Ensure the enemy stays upright.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // Called by a separate trigger (or other mechanism) when the player is about to pass through.
    // Note: The player parameter is cached, but we no longer use it to reorient the enemy.
    public void TriggerKick(GameObject playerObject)
    {
        if (!kickTriggered)
        {
            kickTriggered = true;
            player = playerObject;
            Invoke("StartKick", kickDelay);
        }
    }

    void StartKick()
    {
        // We no longer force the enemy to face the player.
        // The enemy will kick in the direction it is already facing.
        anim.SetTrigger("Kick");
        isKicking = true;
        hasCollidedWithPlayer = false;
    }

    // Called via an Animation Event at the moment the enemy should burst forward.
    public void DoKickBurst()
    {
        if (!isBursting)
        {
            isBursting = true;
            rb.linearVelocity = Vector3.zero;
            // The force is applied along the enemy's current forward direction.
            rb.AddForce(transform.forward * kickBurstSpeed, ForceMode.VelocityChange);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            StartCoroutine(StopBurst());
        }
    }

    private IEnumerator StopBurst()
    {
        yield return new WaitForSeconds(kickBurstDuration);
        isBursting = false;
    }

    // During the kick state, continuously check for the player in the damage zone.
    void FixedUpdate()
    {
        if (isKicking)
        {
            // Calculate the center of the damage/knockback zone in world space.
            Vector3 damageCenter = transform.position + transform.TransformDirection(damageOffset);
            Collider[] hits = Physics.OverlapSphere(damageCenter, damageRadius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerMovement pm = hit.GetComponent<PlayerMovement>();
                    if (pm == null)
                        pm = hit.GetComponentInParent<PlayerMovement>();
                    if (pm != null)
                    {
                        // Apply damage once per kick.
                        if (!hasCollidedWithPlayer)
                        {
                            pm.ignoreKnockback = true;
                            pm.TakeDamage(kickDamage, null);
                            pm.ignoreKnockback = false;
                            hasCollidedWithPlayer = true;
                            Debug.Log("DoorKicker: Damage applied without standard knockback.");
                        }
                        
                        // Set the player's knockback state to override any input.
                        pm.isKnockbackActive = true;
                        
                        // Define a custom knockback vector in global coordinates.
                        // In this case, we want a force along the global negative Z axis.
                        Vector3 customKnockback = new Vector3(0f, 0f, -5f);
                        
                        // Override the player's velocity so that this force is applied.
                        pm.OverrideVelocity(customKnockback);
                        
                        Debug.Log("DoorKicker: Custom knockback applied (overriding velocity).");
                        break; // Process only one player hit.
                    }
                }
            }
        }
    }


    // Called via an Animation Event at the end of the kick animation.
    public void EndKick()
    {
        isKicking = false;
        kickTriggered = false;
    }

    // Optional: visualize the damage zone in the Scene view.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Vector3 damageCenter = transform.position + transform.TransformDirection(damageOffset);
        Gizmos.DrawWireSphere(damageCenter, damageRadius);
    }
}