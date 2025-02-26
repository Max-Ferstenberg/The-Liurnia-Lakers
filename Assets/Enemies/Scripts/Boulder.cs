using UnityEngine;

public class Boulder : MonoBehaviour
{
    // Damage and knockback settings for the boulder.
    public float boulderDamage = 40f;
    public float knockbackForce = 20f; // Custom knockback force

    // Permanent flag: once true, the boulder will no longer apply damage or knockback.
    private bool hasHitPlayer = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasHitPlayer)
            return; // Already hit the player before—do nothing.

        if (collision.gameObject.CompareTag("Player"))
        {
            // Try to get the player's movement script.
            PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();
            if (pm == null)
                pm = collision.gameObject.GetComponentInParent<PlayerMovement>();
            if (pm != null)
            {
                // Apply damage and disable standard knockback.
                pm.ignoreKnockback = true;
                pm.TakeDamage(boulderDamage, null);
                pm.ignoreKnockback = false;
                
                // Set the player's knockback state so input is overridden.
                pm.isKnockbackActive = true;
                
                // Define a custom knockback vector in global coordinates.
                // Here, we force the player's velocity along the global negative Z axis.
                Vector3 customKnockback = new Vector3(0f, 0f, knockbackForce);
                
                // Override the player's velocity so that this force is applied.
                pm.OverrideVelocity(customKnockback);
                            
                // Set the flag so that no further damage or knockback is applied.
                hasHitPlayer = true;
            }
        }
    }
}
