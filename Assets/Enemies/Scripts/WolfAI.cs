using UnityEngine;
using System.Collections.Generic;

public class WolfAI : MonoBehaviour
{
    // Aggro and range parameters
    public float aggroRange = 15f;          // When player is within this range, wolf becomes active.
    public float desiredRange = 7f;         // Ideal distance from player to circle.
    public float attackDistance = 1.5f;     // Distance at which the wolf will begin its attack.
    public float attackDuration = 1.0f;     // Duration of the attack state.
    public float attackDecisionInterval = 3f; // How often a wolf in Walk state may decide to attack.

    // Movement speeds
    public float walkSpeed = 1.5f;
    public float runSpeed = 3f;
    public float rotationSpeed = 5f;        // Speed at which the wolf rotates to face its movement direction.

    // Weights for circling behavior
    public float approachWeight = 0.5f;       // How strongly the wolf corrects its distance from the player.
    public float circleWeight = 1.0f;         // How strongly the wolf circles (perpendicular to the player).

    // Boid (pack) parameters for keeping the pack together
    public float boidNeighborRadius = 3f;
    public float boidSeparationDistance = 1.5f;
    public float boidSeparationWeight = 1.0f;
    public float boidAlignmentWeight = 0.5f;
    public float boidCohesionWeight = 0.5f;

    // Attack damage settings
    public Transform attackPoint;  // position of wolf’s mouth/claws
    public float attackRadius = 0.5f; // How big the hit detection sphere is
    public float attackDamage = 20f;
    public float attackPointZOffset = 0.5f;  // offset for hit detection sphere
    private bool damageApplied = false;

    // Components
    private Rigidbody rb;
    private Animator anim;
    private Transform player;

    // State machine: Idle, Walk (circling), Run (breaking formation), Attack (lunge)
    private enum State { Idle, Walk, Run, Attack }
    private State currentState = State.Idle;
    private float attackDecisionTimer = 0f; // Timer to periodically decide whether to attack

    // Attack state specifics
    private Vector3 attackDirection; // Once attack is triggered, lock in a direction.
    private float attackTimer = 0f;

    // Static counter to limit number of wolves attacking at once.
    private static int wolvesAttacking = 0;

    // Determines circling direction: +1 or -1.
    private int circleDir = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        // Keep the wolf upright.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Player not found! Ensure your player is tagged 'Player'.");

        // Randomize circling direction for variation.
        circleDir = Random.value < 0.5f ? 1 : -1;
    }

    void Update()
    {
        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // --- State Transitions ---
        switch (currentState)
        {
            case State.Idle:
                if (distanceToPlayer <= aggroRange)
                {
                    currentState = State.Walk;
                    attackDecisionTimer = attackDecisionInterval;
                }
                break;

            case State.Walk:
                if (distanceToPlayer > aggroRange)
                {
                    currentState = State.Idle;
                }
                else
                {
                    attackDecisionTimer -= Time.deltaTime;
                    if (attackDecisionTimer <= 0f && wolvesAttacking < 2)
                    {
                        if (Random.value < 0.5f)
                        {
                            currentState = State.Run;
                            wolvesAttacking++;
                        }
                        attackDecisionTimer = attackDecisionInterval;
                    }
                }
                break;

            case State.Run:
                if (distanceToPlayer <= attackDistance)
                {
                    currentState = State.Attack;
                    attackTimer = attackDuration;
                    damageApplied = false; // Reset flag at start of attack
                    attackDirection = (player.position - transform.position).normalized;
                    if (Random.value < 0.5f)
                        anim.SetTrigger("Attack1");
                    else
                        anim.SetTrigger("Attack2");
                }
                else if (distanceToPlayer > aggroRange)
                {
                    currentState = State.Walk;
                    wolvesAttacking = Mathf.Max(0, wolvesAttacking - 1);
                }
                break;

            case State.Attack:
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    currentState = State.Walk;
                    wolvesAttacking = Mathf.Max(0, wolvesAttacking - 1);
                }
                break;
        }

        // --- Animator Updates ---
        // Use a single "Speed" parameter: 0 for idle/attack, 0.5 for Walk, 1 for Run.
        float dampTime = 0.2f;
        float animSpeed = 0f;
        switch (currentState)
        {
            case State.Idle:
                animSpeed = 0f;
                break;
            case State.Walk:
                animSpeed = 0.5f;
                break;
            case State.Run:
                animSpeed = 1.0f;
                break;
            case State.Attack:
                animSpeed = 0f;
                break;
        }
        anim.SetFloat("Speed", animSpeed, dampTime, Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        Vector3 movement = Vector3.zero;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                movement = Vector3.zero;
                break;

            case State.Walk:
                float distanceError = distanceToPlayer - desiredRange;
                Vector3 approachForce = directionToPlayer * distanceError * approachWeight;
                Vector3 circleForce = Vector3.Cross(Vector3.up, directionToPlayer) * circleDir * circleWeight;
                movement = (approachForce + circleForce) * walkSpeed;
                movement += Boid();
                break;

            case State.Run:
                movement = directionToPlayer * runSpeed + 0.3f * Boid();
                break;

            case State.Attack:
                movement = attackDirection * runSpeed;
                break;
        }

        // Always face the direction of movement.
        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    // Boid forces for pack formation.
    Vector3 Boid()
    {
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int count = 0;

        Collider[] colliders = Physics.OverlapSphere(transform.position, boidNeighborRadius);
        foreach (Collider col in colliders)
        {
            if (col.gameObject == this.gameObject)
                continue;

            WolfAI otherWolf = col.GetComponent<WolfAI>();
            if (otherWolf != null)
            {
                count++;
                Vector3 diff = transform.position - otherWolf.transform.position;
                float dist = diff.magnitude;
                if (dist < boidSeparationDistance && dist > 0)
                {
                    separation += diff.normalized / dist;
                }
                alignment += otherWolf.rb.linearVelocity;
                cohesion += otherWolf.transform.position;
            }
        }

        if (count > 0)
        {
            separation /= count;
            alignment /= count;
            cohesion /= count;
            cohesion = (cohesion - transform.position);
        }

        Vector3 force = (boidSeparationWeight * separation) +
                        (boidAlignmentWeight * alignment) +
                        (boidCohesionWeight * cohesion);
        return force;
    }

    public void DealDamage()
    {
        if (damageApplied)
            return;

        Vector3 adjustedAttackPos = attackPoint.position + transform.forward * attackPointZOffset;
        Collider[] hits = Physics.OverlapSphere(adjustedAttackPos, attackRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerMovement pm = hit.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    pm.TakeDamage(attackDamage, null);
                    damageApplied = true; 
                }
            }
        }
    }


    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
