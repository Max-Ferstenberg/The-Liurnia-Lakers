using UnityEngine;
using System.Collections;

public class BoulderPusher : MonoBehaviour
{
    public Animator anim;
    public float pushDelay = 0.3f;
    private Rigidbody rb;
    public GameObject boulder;
    public float boulderPushForce = 500f;
    
    private bool pushTriggered = false;
    public EnemyAI enemyAI;

    public RuntimeAnimatorController pushAnimator;
    public RuntimeAnimatorController fallbackAnimator;

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
                rb.WakeUp();
                //Apply a forward impulse along the enemy's forward direction
                rb.AddForce(transform.forward * boulderPushForce, ForceMode.Impulse);
                //Apply a torque to get the boulder rolling
                rb.AddTorque(transform.right * (boulderPushForce * 0.5f), ForceMode.Impulse);
            }
        }

        //fall back to standard enemy AI behaviour after the boulder has been pushed
        anim.runtimeAnimatorController = fallbackAnimator;
        enemyAI.enabled = true;
    }
}