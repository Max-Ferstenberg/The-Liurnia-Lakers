using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public float aggroRange = 10f;       
    public float attackDistance = 2f;
    public float attackCooldown = 3.0f;

    private Transform attackPoint1;
    public Transform attackPoint2;
    public float attackRadius = 0.5f;      
    public float attackDamage = 20f;
    public float attackPointZOffset = 0.5f; 

    public AudioSource enemyAudioSource;
    public AudioClip stunSound;
    public AudioClip deathSound;

    private NavMeshAgent navAgent;
    private Animator anim;
    private Transform player;

    private float attackTimer;
    private bool attackTriggered = false;

    private enum State { Idle, Approach, Attack, Stunned }
    private State currentState;

    private bool stunned = false;
    public float stunDuration = 5f;
    private float stunTimer = 0f;

    private float attackStateTimer = 0f; 
    private Vector3 attackDirection;     
    private bool damageApplied = false;

    private GameObject hoop;

    public float navUpdateInterval = 0.1f; 
    private float navUpdateTimer = 0f;
    private Vector3 smoothedVelocity = Vector3.zero; 

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        enemyAudioSource = GetComponent<AudioSource>();
        hoop = transform.Find("Hoop").gameObject;

        navAgent.updatePosition = true;
        navAgent.updateRotation = true;
        navAgent.speed = 5f;
        navAgent.acceleration = 8f;
        navAgent.angularSpeed = 120f;
        navAgent.stoppingDistance = 0.1f;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        currentState = State.Idle;
        attackTimer = 0f;

        //force kinematic, otherwise rigidbody and navmesh agent don't work together
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
            rb.isKinematic = true;

        anim.applyRootMotion = false;
    }

    void Update()
    {
        if (player == null)
            return;

        //Check if the player is within aggro range
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > aggroRange)
        {
            currentState = State.Idle;
            attackTriggered = false;
            anim.SetFloat("Vertical", 0f);
            anim.SetFloat("Horizontal", 0f);
            return;
        }
        else
        {
            //Player is within aggro range: switch to Approach if not attacking
            if (currentState == State.Idle)
                currentState = State.Approach;
        }

        if (stunned)
        {
            stunTimer -= Time.deltaTime;
            BasketballHoop hoopScript = hoop.GetComponent<BasketballHoop>();
            if (stunTimer <= 0 && !hoopScript.dunking)
            {
                stunned = false;
                currentState = State.Approach;
                anim.SetBool("IsStunned", false);
                hoop.SetActive(false);
            }
            return;
        }

        attackTimer -= Time.deltaTime;

        if (currentState == State.Attack)
        {
            attackStateTimer -= Time.deltaTime;
            if (attackStateTimer <= 0f)
            {
                currentState = State.Approach;
                attackTriggered = false;
            }
        }
        else if (distance <= attackDistance && attackTimer <= 0)
        {
            if (!attackTriggered)
            {
                //Lock in attack direction toward the player
                attackDirection = (player.position - transform.position).normalized;
                int attackIndex = Random.Range(1, 4);
                anim.SetTrigger("Attack" + attackIndex);
                attackTriggered = true;
                damageApplied = false;  //ensure damage is only applied once to player
                attackTimer = attackCooldown;
            }
            currentState = State.Attack;
        }
        else
        {
            currentState = State.Approach;
            attackTriggered = false;
        }

        float dampTime = 0.2f;
        if (currentState == State.Approach)
        {
            anim.SetFloat("Vertical", 1f, dampTime, Time.deltaTime);
            anim.SetFloat("Horizontal", 0f, dampTime, Time.deltaTime);
        }
        else if (currentState == State.Attack)
        {
            anim.SetFloat("Vertical", 0f, dampTime, Time.deltaTime);
            anim.SetFloat("Horizontal", 0f, dampTime, Time.deltaTime);
        }
        else if (currentState == State.Idle)
        {
            anim.SetFloat("Vertical", 0f, dampTime, Time.deltaTime);
            anim.SetFloat("Horizontal", 0f, dampTime, Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (player == null || stunned)
            return;

        navUpdateTimer -= Time.fixedDeltaTime;
        if (navUpdateTimer > 0)
            return;
        navUpdateTimer = navUpdateInterval;

        if (currentState == State.Approach)
        {
            // In Approach state, set the destination to the player's current position
            Vector3 targetPosition = player.position;
            navAgent.SetDestination(targetPosition);
        }
        else if (currentState == State.Attack)
        {
            navAgent.SetDestination(transform.position);
        }
        else if (currentState == State.Idle)
        {
            navAgent.SetDestination(transform.position);
        }

        //Attempts to smooth out movement for the reason stated below (it kind of works, but not great)
        float smoothingFactor = 10f;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, navAgent.desiredVelocity, Time.fixedDeltaTime * smoothingFactor);
        float speedMultiplier = 5f;
        Vector3 movement = smoothedVelocity * Time.fixedDeltaTime * speedMultiplier;

        //This is not ideal, for the life of me I could not figure out why the navmesh agent was not moving, so I have had to force movement, which works, but the movement is really jittery in game because of this, I tried many, many, many things and this is the only thing that seemed to get it moving. I am certain I probably just overlooked something silly, but here we are.
        navAgent.Move(movement);

        // When attacking, force the enemy to face the locked attack direction
        if (currentState == State.Attack)
        {
            Quaternion targetRotation = Quaternion.LookRotation(attackDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }

    public void DealDamage()
    {
        if (damageApplied)
            return; 

        bool hitPlayer = false;
        
        //Check Attack Point 1 for collision
        if (attackPoint1 != null)
        {
            Vector3 adjustedAttackPos1 = attackPoint1.position + transform.forward * attackPointZOffset;
            Collider[] hits1 = Physics.OverlapSphere(adjustedAttackPos1, attackRadius);
            foreach (Collider hit in hits1)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerMovement pm = hit.GetComponent<PlayerMovement>();
                    if (pm != null)
                    {
                        pm.TakeDamage(attackDamage, null);
                        hitPlayer = true;
                        Debug.Log("Damage dealt to player from " + attackPoint1.name);
                        break;
                    }
                }
            }
        }
        
        //If no hit was detected from Attack Point 1, check Attack Point 2
        if (!hitPlayer && attackPoint2 != null)
        {
            Vector3 adjustedAttackPos2 = attackPoint2.position + transform.forward * attackPointZOffset;
            Collider[] hits2 = Physics.OverlapSphere(adjustedAttackPos2, attackRadius);
            foreach (Collider hit in hits2)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerMovement pm = hit.GetComponent<PlayerMovement>();
                    if (pm != null)
                    {
                        pm.TakeDamage(attackDamage, null);
                        hitPlayer = true;
                        Debug.Log("Damage dealt to player from " + attackPoint2.name);
                        break;
                    }
                }
            }
        }

        if (hitPlayer)
        {
            damageApplied = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Basketball"))
        {
            BallDribble ballDribble = collision.gameObject.GetComponent<BallDribble>();
            if (ballDribble != null && !ballDribble.isHeld && !ballDribble.lockedToHand && !ballDribble.isDribbling)
            {
                Stun();
            }
        }
    }

    public void Stun()
    {
        enemyAudioSource.PlayOneShot(stunSound);
        stunned = true;
        stunTimer = stunDuration;
        currentState = State.Stunned;
        anim.SetBool("IsStunned", true);
        hoop.SetActive(true);
    }

    public void Kill()
    {
        enemyAudioSource.PlayOneShot(deathSound);
        StartCoroutine(DeathAnimation());
    }

    private IEnumerator DeathAnimation()
    {
        anim.Play(anim.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);
        yield return new WaitForSeconds(2f);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float fadeDuration = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    Color color = material.color;
                    color.a = alpha;
                    material.color = color;
                }
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
