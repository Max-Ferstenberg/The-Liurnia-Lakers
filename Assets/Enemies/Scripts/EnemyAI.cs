using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    // References
    public Transform player;             // Assign the player's transform in the Inspector (or find by tag)
    public Animator animator;            // The Animator controlling enemy animations

    // Aggro and distance parameters
    public float aggroRange = 10f;       // The enemy becomes active when the player is within this range
    public float maintainDistance = 3f;  // The enemy will try to stop moving forward when it gets within this distance
    public float attackRange = 0.2f;       // For attacking, enemy wants to get this close

    // Movement parameters
    public float moveSpeed = 5f;         // Base movement speed when pursuing
    public float wanderStrength = 0.5f;  // Amplitude of side-to-side oscillation
    public float wanderSpeed = 1f;       // Frequency of oscillation

    // Attack parameters
    public float attackCooldownMin = 6f; // Minimum time between attacks
    public float attackCooldownMax = 10f; // Maximum time between attacks
    private float attackTimer = 0f;      // Timer to count down until next attack

    // Internal state management
    private Rigidbody rb;
    private enum State { Idle, Pursuing, Attacking }
    private State currentState = State.Idle;
    private bool isAttacking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze X and Z rotations so the enemy stays upright.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }
        
        currentState = State.Idle;
        ResetAttackTimer();
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // If the player is within aggro range, switch from Idle to Pursuing.
        if (distanceToPlayer <= aggroRange)
        {
            if (currentState == State.Idle){
                currentState = State.Pursuing;
                animator.SetBool("IsWalking", true);
            }
        }
        else
        {
            currentState = State.Idle;
            animator.SetBool("IsWalking", false);
        }

        // While Pursuing, count down to the next attack.
        if (currentState == State.Pursuing)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                currentState = State.Attacking;
                isAttacking = true;
            }
        }

        // State-based behavior:
        switch (currentState)
        {
            case State.Idle:
                rb.linearVelocity = Vector3.zero;
                break;

            case State.Pursuing:
                PursuePlayer(distanceToPlayer);
                break;

            case State.Attacking:
                AttackPlayer(distanceToPlayer);
                break;
        }
    }

    void PursuePlayer(float distanceToPlayer)
    {
        // When far away, move toward the player with a wavy (oscillatory) path.
        if (distanceToPlayer > maintainDistance)
        {
            // Direct vector to the player.
            Vector3 direction = (player.position - transform.position).normalized;
            // Compute a perpendicular vector (to add a sine-based oscillation).
            Vector3 perp = Vector3.Cross(direction, Vector3.up);
            float wanderOffset = Mathf.Sin(Time.time * wanderSpeed) * wanderStrength;
            Vector3 offset = perp * wanderOffset;
            Vector3 targetDir = (direction + offset).normalized;

            // Smoothly rotate to face the target direction.
            if (targetDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.2f);
            }

            rb.linearVelocity = targetDir * moveSpeed;
        }
        else
        {
            // Within maintainDistance, stop moving.
            rb.linearVelocity = Vector3.zero;
        }
    }

    void AttackPlayer(float distanceToPlayer)
    {
        // Always smoothly face the player while attacking.
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.2f);
        }

        // If not within attack range, approach the player.
        if (distanceToPlayer > attackRange)
        {
            rb.linearVelocity = directionToPlayer * moveSpeed;
        }
        else
        {
            // Once close enough, stop moving.
            rb.linearVelocity = Vector3.zero;

            // Begin attack only once.
            if (isAttacking)
            {
                StartCoroutine(PerformAttack());
            }
        }
    }

    IEnumerator PerformAttack()
    {
        // Randomly select one of three attack triggers.
        int attackChoice = Random.Range(1, 4); // 1, 2, or 3
        animator.SetTrigger("Attack" + attackChoice);
        yield return new WaitForSeconds(0.01f);

        // After attacking, reset the attack timer and return to pursuing.
        isAttacking = false;
        ResetAttackTimer();
        currentState = State.Pursuing;
    }

    void ResetAttackTimer()
    {
        attackTimer = Random.Range(attackCooldownMin, attackCooldownMax);
    }
}