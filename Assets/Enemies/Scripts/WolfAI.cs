using UnityEngine;
using System.Collections.Generic;

public class WolfAI : MonoBehaviour
{
    // -------------------- Aggro and Range Parameters --------------------
    public float aggroRange = 15f;  
    public float desiredRange = 7f;    
    public float attackDistance = 1.5f;   
    public float attackDuration = 1.0f;    
    public float attackDecisionInterval = 3f; 

    // -------------------- Movement Speeds --------------------
    public float walkSpeed = 1.5f;    
    public float runSpeed = 3f;          
    public float rotationSpeed = 5f;       

    // -------------------- Circling Behavior Weights --------------------
    public float approachWeight = 0.5f;     
    public float circleWeight = 1.0f;         

    // -------------------- Boid (Pack) Parameters --------------------
    public float boidNeighborRadius = 3f;    
    public float boidSeparationDistance = 1.5f;  
    public float boidSeparationWeight = 1.0f; 
    public float boidAlignmentWeight = 0.5f;   
    public float boidCohesionWeight = 0.5f;     

    // -------------------- Attack Damage Settings --------------------
    public Transform attackPoint;     
    public float attackRadius = 0.5f;   
    public float attackDamage = 20f;   
    public float attackPointZOffset = 0.5f;  
    private bool damageApplied = false; 

    // -------------------- Component References --------------------
    private Rigidbody rb;
    private Animator anim;
    private Transform player;

    // -------------------- State Machine --------------------
    private enum State { Idle, Walk, Run, Attack }
    private State currentState = State.Idle;
    private float attackDecisionTimer = 0f; 

    // -------------------- Attack State Variables --------------------
    private Vector3 attackDirection;
    private float attackTimer = 0f;   

    // -------------------- Global Control for Attacks --------------------
    private static int wolvesAttacking = 0; 

    private int circleDir = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        //Randomize circling direction to introduce variation in wolf behavior
        circleDir = Random.value < 0.5f ? 1 : -1;
    }

    void Update()
    {
        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                // Transition from Idle to Walk if the player is within aggro range
                if (distanceToPlayer <= aggroRange)
                {
                    currentState = State.Walk;
                    attackDecisionTimer = attackDecisionInterval;
                }
                break;

            case State.Walk:
                //If player moves out of range, revert to Idle
                if (distanceToPlayer > aggroRange)
                {
                    currentState = State.Idle;
                }
                else
                {
                    attackDecisionTimer -= Time.deltaTime;
                    if (attackDecisionTimer <= 0f && wolvesAttacking < 2)
                    {
                        //With a 50% chance, transition to Run state to start an attack (prevents too many wolves from attacking at once)
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
                    damageApplied = false;
                    attackDirection = (player.position - transform.position).normalized;
                    // Randomly trigger one of two attack animations
                    if (Random.value < 0.5f)
                        anim.SetTrigger("Attack1");
                    else
                        anim.SetTrigger("Attack2");
                }
                // If the player is too far, revert to Walk state
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

        //spped parameter is used for the wolf's state: idle (0), walking (0.5), running (1), or attacking (0)
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

    // -------------------- FixedUpdate for Movement --------------------
    void FixedUpdate()
    {
        if (player == null)
            return;

        Vector3 movement = Vector3.zero;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        //Determine movement based on the current state
        switch (currentState)
        {
            case State.Idle:
                movement = Vector3.zero;
                break;

            case State.Walk:
                float distanceError = distanceToPlayer - desiredRange;
                Vector3 approachForce = directionToPlayer * distanceError * approachWeight;
                //Generate a circling force perpendicular to the direction to the player, this way the pack will circle the player
                Vector3 circleForce = Vector3.Cross(Vector3.up, directionToPlayer) * circleDir * circleWeight;
                movement = (approachForce + circleForce) * walkSpeed;
                movement += Boid();
                break;

            case State.Run:
                //In Run state, the wolf charges toward the player
                movement = directionToPlayer * runSpeed + 0.3f * Boid();
                break;

            case State.Attack:
                //During an attack, the movement is locked to the attack direction
                movement = attackDirection * runSpeed;
                break;
        }

        //Rotate the wolf to face the direction of movement if it is significant
        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        //Update the wolf's position
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    // -------------------- Boids Algorithm for Pack Behavior --------------------
    //   1. Separation: Avoid crowding neighbors by steering away.
    //   2. Alignment: Steer towards the average heading of neighbors.
    //   3. Cohesion: Steer to move toward the average position of neighbors.
    Vector3 Boid()
    {
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int count = 0;

        //Retrieve all colliders within a specified radius (neighbors)
        Collider[] colliders = Physics.OverlapSphere(transform.position, boidNeighborRadius);
        foreach (Collider col in colliders)
        {
            //skip self
            if (col.gameObject == this.gameObject)
                continue;

            //Check if the neighbor is another wolf
            WolfAI otherWolf = col.GetComponent<WolfAI>();
            if (otherWolf != null)
            {
                count++;
                //Calculate separation force: the closer a neighbor, the stronger the repulsion
                Vector3 diff = transform.position - otherWolf.transform.position;
                float dist = diff.magnitude;
                if (dist < boidSeparationDistance && dist > 0)
                {
                    separation += diff.normalized / dist;
                }
                // lignment: accumulate the velocity of neighbors
                alignment += otherWolf.rb.linearVelocity;
                //Cohesion: accumulate the position of neighbors
                cohesion += otherWolf.transform.position;
            }
        }

        if (count > 0)
        {
            //Average the forces for separation, alignment, and cohesion
            separation /= count;
            alignment /= count;
            cohesion /= count;
            //Calculate the cohesion force vector, steer towards the group's center
            cohesion = (cohesion - transform.position);
        }

        //Combine the three forces with their respective weights
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
        //check if the player was hit
        Collider[] hits = Physics.OverlapSphere(adjustedAttackPos, attackRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerMovement pm = hit.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    pm.TakeDamage(attackDamage, null);
                    damageApplied = true; //prevents damage being dealt twice
                }
            }
        }
    }
}