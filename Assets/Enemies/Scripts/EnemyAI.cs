using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    // Existing movement parameters
    public float approachSpeed = 1.5f;
    public float strafeSpeed = 0.75f;
    public float retreatSpeed = 0.75f;
    public float rotationSpeed = 3f;

    public float attackDistance = 1f;
    public float approachDuration = 2f;
    public float strafeTime = 1f;
    public float retreatTime = 1.5f;
    public float attackCooldown = 3.0f;

    public AudioSource enemyAudioSource;
    public AudioClip stunSound;

    // New boid parameters
    public float boidNeighborRadius = 3f;
    public float boidSeparationDistance = 1.5f;
    public float boidSeparationWeight = 1.0f;
    public float boidAlignmentWeight = 0.5f;
    public float boidCohesionWeight = 0.5f;

    public Rigidbody rb; // Made public so other instances can read velocity
    private Animator anim;
    private Transform player;

    private float stateTimer;
    private float attackTimer;
    private float strafeDirSign = 1f;
    private bool attackTriggered = false;

    private enum State { Approach, Strafe, Retreat, Attack, Stunned }
    private State currentState;

    private bool stunned = false;
    public float stunDuration = 5f;
    private float stunTimer = 0f;

    public GameObject hoop;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Player not found! Make sure your player GameObject is tagged as 'Player'.");

        currentState = State.Approach;
        stateTimer = approachDuration;
        attackTimer = 0f;
    }

    void Update()
    {
        if (player == null)
            return;

        if (stunned)
        {
            stunTimer -= Time.deltaTime;
            BasketballHoop hoopScript = hoop.GetComponent<BasketballHoop>();
            if (stunTimer <= 0 && hoopScript.dunking == false)
            {
                stunned = false;
                currentState = State.Approach;
                stateTimer = approachDuration;
                anim.SetBool("IsStunned", false);
                hoop.SetActive(false);
            }
            return;
        }

        stateTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;

        float distance = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Approach:
                if (distance <= attackDistance && attackTimer <= 0)
                {
                    currentState = State.Attack;
                    stateTimer = 0.5f;
                    attackTriggered = false;
                }
                else if (stateTimer <= 0)
                {
                    if (Random.Range(0, 2) == 0)
                    {
                        currentState = State.Strafe;
                        stateTimer = strafeTime;
                        strafeDirSign = Random.value < 0.5f ? 1f : -1f;
                    }
                    else
                    {
                        currentState = State.Retreat;
                        stateTimer = retreatTime;
                    }
                }
                break;

            case State.Strafe:
                if (stateTimer <= 0)
                {
                    currentState = State.Approach;
                    stateTimer = approachDuration;
                }
                break;

            case State.Retreat:
                if (stateTimer <= 0)
                {
                    currentState = State.Approach;
                    stateTimer = approachDuration;
                }
                break;

            case State.Attack:
                if (!attackTriggered)
                {
                    int attackIndex = Random.Range(1, 4);
                    switch (attackIndex)
                    {
                        case 1:
                            anim.SetTrigger("Attack1");
                            break;
                        case 2:
                            anim.SetTrigger("Attack2");
                            break;
                        case 3:
                            anim.SetTrigger("Attack3");
                            break;
                    }
                    attackTriggered = true;
                    Debug.Log("Enemy attacks! Attack " + attackIndex);
                }
                if (stateTimer <= 0)
                {
                    attackTimer = attackCooldown;
                    currentState = State.Approach;
                    stateTimer = approachDuration;
                }
                break;
        }

        // Set animation parameters (unchanged)
        float dampTime = 0.2f;
        switch (currentState)
        {
            case State.Approach:
                anim.SetFloat("Vertical", 1f, dampTime, Time.deltaTime);
                anim.SetFloat("Horizontal", 0f, dampTime, Time.deltaTime);
                break;
            case State.Strafe:
                anim.SetFloat("Vertical", 0f, dampTime, Time.deltaTime);
                anim.SetFloat("Horizontal", (strafeDirSign > 0 ? 1f : -1f), dampTime, Time.deltaTime);
                break;
            case State.Retreat:
                anim.SetFloat("Vertical", -1f, dampTime, Time.deltaTime);
                anim.SetFloat("Horizontal", 0f, dampTime, Time.deltaTime);
                break;
            case State.Attack:
                anim.SetFloat("Vertical", 0f, dampTime, Time.deltaTime);
                anim.SetFloat("Horizontal", 0f, dampTime, Time.deltaTime);
                break;
            case State.Stunned:
                anim.SetFloat("Vertical", 0f, dampTime, Time.deltaTime);
                anim.SetFloat("Horizontal", 0f, dampTime, Time.deltaTime);
                break;
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Basketball"))
        {
            BallDribble ballDribble = collision.gameObject.GetComponent<BallDribble>();
            if (ballDribble != null && ballDribble.isHeld == false && ballDribble.lockedToHand == false && ballDribble.isDribbling == false)
            {
                Stun();
            }
        }
    }

    public void Stun()
    {
        enemyAudioSource.PlayOneShot(stunSound);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;
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


    void FixedUpdate()
    {
        if (player == null || stunned)
            return;

        // Rotate to face the player as before
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

        // Determine movement based on state
        Vector3 movement = Vector3.zero;
        switch (currentState)
        {
            case State.Approach:
                movement = directionToPlayer * approachSpeed;
                break;
            case State.Strafe:
                Vector3 strafeDirection = Vector3.Cross(directionToPlayer, Vector3.up) * strafeDirSign;
                movement = strafeDirection.normalized * strafeSpeed;
                break;
            case State.Retreat:
                movement = -directionToPlayer * retreatSpeed;
                break;
            case State.Attack:
                movement = Vector3.zero;
                break;
            case State.Stunned:
                movement = Vector3.zero;
                break;
        }

        // Compute boid forces from nearby enemies
        Vector3 boidForce = Boid();
        movement += boidForce; // blend boid force with current state movement

        // Move enemy based on the combined vector
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    Vector3 Boid()
    {
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int count = 0;

        // Look for other enemies in the neighborhood.
        Collider[] colliders = Physics.OverlapSphere(transform.position, boidNeighborRadius);
        foreach (Collider col in colliders)
        {
            if (col.gameObject == this.gameObject)
                continue;

            // Check if the other object has an EnemyAI script.
            EnemyAI otherEnemy = col.GetComponent<EnemyAI>();
            if (otherEnemy != null)
            {
                count++;
                Vector3 diff = transform.position - otherEnemy.transform.position;
                float dist = diff.magnitude;
                if (dist < boidSeparationDistance && dist > 0)
                {
                    separation += diff.normalized / dist;
                }
                // Use the other enemy's current velocity for alignment.
                alignment += otherEnemy.rb.linearVelocity;
                // For cohesion, add the neighbor's position.
                cohesion += otherEnemy.transform.position;
            }
        }

        if (count > 0)
        {
            separation /= count;
            alignment /= count;
            cohesion /= count;
            cohesion = (cohesion - transform.position);
        }

        // Weight the forces
        Vector3 force = (boidSeparationWeight * separation) +
                        (boidAlignmentWeight * alignment) +
                        (boidCohesionWeight * cohesion);

        return force;
    }
}