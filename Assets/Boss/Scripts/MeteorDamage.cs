using System.Collections;
using UnityEngine;

public class MeteorDamage : MonoBehaviour
{
    [Tooltip("Damage to deal when the player enters the meteor area.")]
    public float damage = 30f;
    
    [Tooltip("Delay (in seconds) before the collider becomes active.")]
    public float damageDelay = 0.5f;
    
    private bool hasDealtDamage = false;
    private bool isReady = false;
    
    IEnumerator Start()
    {
        yield return new WaitForSeconds(damageDelay);
        isReady = true;
        yield return new WaitForSeconds(2.5f);
        Destroy(gameObject);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!isReady) return;
        
        if (!hasDealtDamage && other.CompareTag("Player"))
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.TakeDamageFromBoss(damage);
                hasDealtDamage = true;
            }
        }
    }
    
    public void ResetDamage()
    {
        hasDealtDamage = false;
    }
}
