using UnityEngine;

public class Boulder : MonoBehaviour
{
    // Damage and knockback settings for the boulder.
    public float boulderDamage = 40f;
    public float knockbackForce = 10f; // Custom knockback force
    
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            // Try to get the player's movement script.
            PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();
            if(pm == null)
                pm = collision.gameObject.GetComponentInParent<PlayerMovement>();
            if(pm != null)
            {
                // Apply damage.
                pm.TakeDamage(boulderDamage, null);
                // Override the player's velocity.
                Vector3 customKnockback = new Vector3(knockbackForce, 0f, 0f);
                pm.OverrideVelocity(customKnockback);
                Debug.Log("Boulder: Player hit, custom knockback applied.");
            }
        }
    }
}