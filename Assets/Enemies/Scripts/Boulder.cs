using UnityEngine;


//Literally just a script that will deal damage to the player and launch them backwards if they are hit by the moving boulder, this is used in one place in the game
public class Boulder : MonoBehaviour
{
    public float boulderDamage = 40f;
    public float knockbackForce = 20f; 

    private bool hasHitPlayer = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasHitPlayer)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();
            if (pm == null)
                pm = collision.gameObject.GetComponentInParent<PlayerMovement>();
            if (pm != null)
            {
                pm.ignoreKnockback = true;
                pm.TakeDamage(boulderDamage, null);
                pm.ignoreKnockback = false;
                
                pm.isKnockbackActive = true;
                
                Vector3 customKnockback = new Vector3(0f, 0f, knockbackForce);
                
                pm.OverrideVelocity(customKnockback);
                            
                hasHitPlayer = true;
            }
        }
    }
}
