using UnityEngine;
using System.Collections;

public class BoulderPusher : MonoBehaviour
{
    // Reference to this enemy's Animator.
    public Animator anim;
    // Delay after trigger before starting the push animation.
    public float pushDelay = 0.3f;
    private Rigidbody rb;
    // Reference to the boulder GameObject.
    public GameObject boulder;
    // The impulse force to apply to the boulder.
    public float boulderPushForce = 500f;
    
    private bool pushTriggered = false;
    public EnemyAI enemyAI;

    public RuntimeAnimatorController pushAnimator;
    public RuntimeAnimatorController fallbackAnimator;
    // Called by the trigger zone when the player enters.

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
    }

    public void TriggerPush()
    {
        if (!pushTriggered)
        {
            pushTriggered = true;
            Invoke("StartPush", pushDelay);
        }
    }
    
    void StartPush()
    {
        anim.SetTrigger("Kick");
    }
    
    public void PushBoulder()
    {
        if (boulder != null)
        {
            Rigidbody rb = boulder.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.WakeUp(); // Ensure the boulder is active.
                // Apply a forward impulse along the enemy's forward direction.
                rb.AddForce(transform.forward * boulderPushForce, ForceMode.Impulse);
                // Apply a torque to get the boulder rolling.
                // Adjust the multiplier (here 0.5f) as needed for your desired rolling effect.
                rb.AddTorque(transform.right * (boulderPushForce * 0.5f), ForceMode.Impulse);
            }
        }
        anim.runtimeAnimatorController = fallbackAnimator;
        enemyAI.enabled = true;
    }
}