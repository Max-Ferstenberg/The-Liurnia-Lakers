using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // Movement & combat parameters
    public float aggroRange = 10f;         // Enemy becomes active within this range.
    public float attackDistance = 2f;
    public float attackCooldown = 3.0f;
    public float attackDuration = 0.5f; // how long the attack state lasts

    // Two attack points for hit detection.
    public Transform attackPoint1;
    public Transform attackPoint2;
    public float attackRadius = 0.5f;    // radius of the hit detection sphere
    public float attackDamage = 20f;
    public float attackPointZOffset = 0.5f; // offset applied in the forward direction

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

    private float attackStateTimer = 0f;  // timer for how long we remain in the Attack state
    private Vector3 attackDirection;      // locked attack direction
    private bool damageApplied = false;

    public AudioSource enemyAudioSource;
    public AudioClip stunSound;
    public GameObject hoop;

    // Throttling NavMesh updates
    public float navUpdateInterval = 0.1f;
    private float navUpdateTimer = 0f;
    private Vector3 smoothedVelocity = Vector3.zero;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // Ensure the NavMeshAgent controls movement
        navAgent.updatePosition = true;
        navAgent.updateRotation = true;
        // Set explicit parameters (adjust as needed in Inspector)
        navAgent.speed = 5f;
        navAgent.acceleration = 8f;
        navAgent.angularSpeed = 120f;
        navAgent.stoppingDistance = 0.1f;

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Player not found! Make sure your player GameObject is tagged as 'Player'.");

        // Initialize state to Idle
        currentState = State.Idle;
        attackTimer = 0f;

        // If using a Rigidbody for collisions, ensure it's kinematic
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
            rb.isKinematic = true;

        // Ensure Animator Root Motion is off (so NavMeshAgent movement isn't overridden)
        anim.applyRootMotion = false;
    }

    void Update()
    {
        if (player == null)
            return;

        // Check if the player is within aggro range.
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > aggroRange)
        {
            currentState = State.Idle;
            attackTriggered = false;
            // Optionally, update animator to show idle behavior.
            anim.SetFloat("Vertical", 0f);
            anim.SetFloat("Horizontal", 0f);
            return;
        }
        else
        {
            // Player is within aggro range: switch to Approach if not attacking.
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

        // Update the cooldown timer
        attackTimer -= Time.deltaTime;

        // --- State Transitions ---
        if (currentState == State.Attack)
        {
            // While attacking, count down the attack state duration
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
                // Lock in attack direction toward the player.
                attackDirection = (player.position - transform.position).normalized;
                int attackIndex = Random.Range(1, 4);
                anim.SetTrigger("Attack" + attackIndex);
                attackTriggered = true;
                damageApplied = false;  // Reset damage flag for this attack
                attackTimer = attackCooldown;    // start the cooldown
                attackStateTimer = attackDuration; // remain in attack state for this duration
                Debug.Log("Enemy attacks! Attack " + attackIndex);
            }
            currentState = State.Attack;
        }
        else
        {
            currentState = State.Approach;
            attackTriggered = false;
        }

        // --- Animator Updates ---
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

        // Only move if not idle.
        if (currentState == State.Approach)
        {
            Vector3 targetPosition = player.position;
            navAgent.SetDestination(targetPosition);
        }
        else if (currentState == State.Attack)
        {
            // During attack, lock on: keep destination at current position.
            navAgent.SetDestination(transform.position);
        }
        else if (currentState == State.Idle)
        {
            // If idle, remain in place.
            navAgent.SetDestination(transform.position);
        }

        // Smooth out movement
        float smoothingFactor = 10f;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, navAgent.desiredVelocity, Time.fixedDeltaTime * smoothingFactor);
        float speedMultiplier = 5f;
        Vector3 movement = smoothedVelocity * Time.fixedDeltaTime * speedMultiplier;

        // Use NavMeshAgent.Move() for manual movement.
        navAgent.Move(movement);

        // If attacking, force rotation to lock on to the attack direction.
        if (currentState == State.Attack)
        {
            Quaternion targetRotation = Quaternion.LookRotation(attackDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }

    // This method is called via an Animation Event at the appropriate frame.
    public void DealDamage()
    {
        if (damageApplied)
            return; // Already applied damage this attack

        bool hitPlayer = false;
        
        // Check Attack Point 1
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
                        break; // Only one instance of damage per attack
                    }
                }
            }
        }
        
        // If not yet hit from attackPoint1, check Attack Point 2
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
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint1 != null)
        {
            Gizmos.color = Color.red;
            Vector3 adjustedAttackPos1 = attackPoint1.position + transform.forward * attackPointZOffset;
            Gizmos.DrawWireSphere(adjustedAttackPos1, attackRadius);
        }
        if (attackPoint2 != null)
        {
            Gizmos.color = Color.blue;
            Vector3 adjustedAttackPos2 = attackPoint2.position + transform.forward * attackPointZOffset;
            Gizmos.DrawWireSphere(adjustedAttackPos2, attackRadius);
        }
    }
}