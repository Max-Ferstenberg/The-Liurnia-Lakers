using UnityEngine;
using System.Collections;


// Preface for this script: A lot of this boils down to hardcoded, very specific values to facilitate one interaction that we thought would be funny and hoped would get a laugh out of some unsuspecting players


public class DoorKicker : MonoBehaviour
{
    // --- Animation & Timing Settings ---
    public Animator anim;                     
    public float kickDelay = 0.3f;             
    // --- Damage Settings ---
    public float kickDamage = 30f;          
    public float knockbackForce = 10f;   

    public float kickBurstSpeed = 10f;         
    public float kickBurstDuration = 0.3f;    
    public float jumpForce = 5f;         
    private bool isBursting = false;   

    // --- Damage Zone Settings ---
    public float damageRadius = 1.0f;          
    public Vector3 damageOffset = new Vector3(0, 0, 1.0f);

    // --- Control Flags ---
    private bool isKicking = false;            
    private bool kickTriggered = false;       
    private bool hasCollidedWithPlayer = false; 

    // --- Component and Player References ---
    private Rigidbody rb;                 
    private GameObject player;             

    // -------------------- Initialization --------------------
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // -------------------- Kick Triggering --------------------
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

    void FixedUpdate()
    {
        if (isKicking)
        {
            // Calculate the center of the damage zone in world space by transforming the local offset
            Vector3 damageCenter = transform.position + transform.TransformDirection(damageOffset);
            Collider[] hits = Physics.OverlapSphere(damageCenter, damageRadius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    //retrieve the PlayerMovement component either directly or from the parent
                    PlayerMovement pm = hit.GetComponent<PlayerMovement>();
                    if (pm == null)
                        pm = hit.GetComponentInParent<PlayerMovement>();
                    if (pm != null)
                    {
                        //Apply damage only once
                        if (!hasCollidedWithPlayer)
                        {
                            pm.ignoreKnockback = true;
                            pm.TakeDamage(kickDamage, null);
                            pm.ignoreKnockback = false;
                            hasCollidedWithPlayer = true;
                        }
                        
                        pm.isKnockbackActive = true;
                        
                        Vector3 customKnockback = new Vector3(0f, 0f, -5f);
                        
                        pm.OverrideVelocity(customKnockback);
                        
                        break; 
                    }
                }
            }
        }
    }

    public void EndKick()
    {
        isKicking = false; 
        kickTriggered = false; 
    }
}