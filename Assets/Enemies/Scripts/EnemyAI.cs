using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    // References
    public Transform player;             // Assign the player's transform in the Inspector (or tag the player and find it at Start)
    public Animator animator;            // The Animator controlling enemy animations

    // Aggro and distance parameters
    public float aggroRange = 20f;       // The enemy becomes active when the player is within this range
    public float maintainDistance = 5f;  // The enemy will try to stop moving forward when it gets within this distance
    public float attackRange = 3f;       // For attacking, enemy wants to get this close

    // Movement parameters
    public float moveSpeed = 3f;         // Base movement speed when pursuing
    public float wanderStrength = 0.5f;  // Controls the amplitude of the side-to-side oscillation
    public float wanderSpeed = 1f;       // Controls how quickly the oscillation happens

    // Attack parameters
    public float attackCooldownMin = 3f; // Minimum time between attacks
    public float attackCooldownMax = 6f; // Maximum time between attacks
    private float attackTimer = 0f;      // Timer to count down until next attack

    // Internal state management
    private Rigidbody rb;
    private enum State { Idle, Pursuing, Attacking }
    private State currentState = State.Idle;
    private bool isAttacking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }
        
        // Start in Idle; attack timer is set
        currentState = State.Idle;
        ResetAttackTimer();
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // If the player is within aggro range, switch to pursuing state.
        if (distanceToPlayer <= aggroRange)
        {
            if (currentState == State.Idle)
            {
                currentState = State.Pursuing;
            }
        }
        else
        {
            currentState = State.Idle;
        }

        // Only pursue/attack if in an active state.
        if (currentState == State.Pursuing)
        {
            // Decrease attack timer while pursuing.
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                // Switch to attack state.
                currentState = State.Attacking;
                isAttacking = true;
            }
        }

        // Handle behavior based on state.
        switch (currentState)
        {
            case State.Idle:
                // Optionally, have idle behavior here (or simply zero velocity).
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
        // If the enemy is too far (greater than maintainDistance), pursue the player with some non-linear "wander".
        if (distanceToPlayer > maintainDistance)
        {
            // Basic direction toward player.
            Vector3 direction = (player.position - transform.position).normalized;

            // Compute a wander (oscillatory) offset perpendicular to the direction.
            Vector3 perp = Vector3.Cross(direction, Vector3.up);
            // Use a sine wave (based on time) for a smooth oscillation.
            float wanderOffset = Mathf.Sin(Time.time * wanderSpeed) * wanderStrength;
            Vector3 offset = perp * wanderOffset;

            // Combine direction and offset and normalize.
            Vector3 targetDir = (direction + offset).normalized;

            // Smoothly rotate to face target direction.
            if (targetDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
            }

            rb.linearVelocity = targetDir * moveSpeed;
        }
        else
        {
            // Within maintainDistance: hold position (or optionally circle the player)
            rb.linearVelocity = Vector3.zero;
        }
    }

    void AttackPlayer(float distanceToPlayer)
    {
        // In the attack state, if the enemy isn't close enough, move in toward the player.
        if (distanceToPlayer > attackRange)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            // Once in attack range, stop moving and trigger an attack.
            rb.linearVelocity = Vector3.zero;
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        // Only allow one attack at a time.
        if (!isAttacking)
            yield break;

        // Randomly select one of three attacks with equal probability.
        int attackChoice = Random.Range(1, 4); // returns 1, 2, or 3
        animator.SetTrigger("Attack" + attackChoice);

        // Wait for the attack animation to finish.
        // (You might use the actual length of your attack animation here; we'll assume 1 second for this example.)
        yield return new WaitForSeconds(1f);

        // Reset the attack state.
        isAttacking = false;
        ResetAttackTimer();
        currentState = State.Pursuing;
    }

    void ResetAttackTimer()
    {
        // Randomly choose the next attack interval.
        attackTimer = Random.Range(attackCooldownMin, attackCooldownMax);
    }
}
