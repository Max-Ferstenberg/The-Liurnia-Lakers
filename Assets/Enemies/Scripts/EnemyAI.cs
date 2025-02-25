using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float approachSpeed = 1.5f;
    public float rotationSpeed = 3f;

    [Header("Attack & Timing Settings")]
    public float attackDistance = 1f;
    public float approachDuration = 2f;
    public float attackCooldown = 3.0f;

    [Header("Attack Trigger")]
    [Tooltip("If the player is within this radius, the enemy forces an attack (or queues one if already attacking).")]
    public float attackTriggerRadius = 2f;

    [Header("Force Attack Threshold")]
    [Tooltip("If the player is within this distance, the enemy will immediately enter attack state.")]
    public float forceAttackDistance = 2f;

    [Header("Steering / Pathfinding Settings")]
    [Tooltip("Radius for obstacle detection using SphereCast.")]
    public float obstacleDetectionRadius = 0.5f;
    [Tooltip("Distance ahead to check for obstacles.")]
    public float obstacleDetectionDistance = 3f;
    [Tooltip("Force applied to avoid obstacles.")]
    public float obstacleAvoidanceForce = 5f;

    [Tooltip("Radius to check for neighboring enemies for separation.")]
    public float separationRadius = 2f;
    [Tooltip("Force applied for enemy separation.")]
    public float separationForce = 3f;
    [Tooltip("LayerMask for other enemies.")]
    public LayerMask enemyMask;

    [Header("Ground Following Settings")]
    [Tooltip("Layers representing ground, stairs, or floors.")]
    public LayerMask groundMask;
    [Tooltip("How far down to check for ground.")]
    public float groundCheckDistance = 3f;
    [Tooltip("Speed at which the enemy adjusts its y-position.")]
    public float groundFollowSpeed = 5f;
    [Tooltip("Additional force applied to avoid edges.")]
    public float edgeAvoidanceForce = 10f;

    [Header("Global Pathfinding Settings")]
    public Grid grid;                           // Assign your Grid component here (from your empty GameObject with Grid.cs)
    public float pathRecalcInterval = 1f;         // How often to recalc the global path
    public float waypointThreshold = 0.5f;        // How close to a node before switching to the next
    private Vector3 lastSteering; // For smoothing
    private Vector3[] rayDirections;
    private Vector3 steering;
    private bool hasSightedPlayer = false;


    [Header("References")]
    public Transform player;
    public Animator anim;

    [Header("Obstacle & Aggro Settings")]
    [Tooltip("The maximum distance at which the enemy will approach/attack the player.")]
    public float aggroRange = 15f;
    [Tooltip("Layers considered obstacles (walls, etc.).")]
    public LayerMask obstacleMask;

    [Header("State Machine Animation")]
    [Tooltip("Damping time for animation parameters.")]
    public float dampTime = 0.2f;

    [Header("Ledge Detection Settings")]
    public float forwardLedgeDetectionDistance = 5f; // How far ahead to check for ledges
    public int ledgeDetectionSampleCount = 5;        // How many samples along that ray

    // Attack layer control (assumes your Attack layer is named "Attack" in the Animator)
    private const string attackLayerName = "Attack";

    // New state value for patrolling
    private enum State { Approach, Strafe, Retreat, Attack, Patrol }

    // New variables for patrol mode
    private Vector3 patrolDirection = Vector3.right; // initial patrol direction (horizontal)
    public float patrolSwitchInterval = 3f;            // how often to reverse direction
    private float patrolTimer = 3f;

    // For instant locking on when entering aggro range.
    private bool wasInAggroRange = false;
    private State currentState;
    private float stateTimer;
    private float attackTimer;
    private float strafeDirSign = 1f;
    private bool attackTriggered = false;
    private bool queuedAttack = false;
    private float previousDistance;

    private Rigidbody rb;
    private bool isCollidingWithPlayer = false;

    // Global path variables.
    private List<Node> currentPath;
    private int currentPathIndex;
    private float pathRecalcTimer;

    // Last known position of the player (updated when the enemy sees the player).
    private Vector3 lastKnownPlayerPos;

    // Line-of-sight ray origin offset.
    public float eyeHeight = 1.5f;

    // NEW: Minimum height above enemy to consider an obstacle (to ignore floors)
    public float minObstacleHeightOffset = 0.3f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (grid != null)
        {
            grid.Initialize(elevationThreshold, player);
        }
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
            else
                Debug.LogError("Player not found! Make sure your player is tagged as 'Player'.");
        }
        lastKnownPlayerPos = player.position;

        currentState = State.Approach;
        stateTimer = approachDuration;
        attackTimer = 0f;
        previousDistance = Vector3.Distance(transform.position, player.position);
        pathRecalcTimer = pathRecalcInterval;
        Debug.Log("EnemyAI started. Initial state: Approach");
    }

    void Update()
    {
        if (player == null)
        {
            Debug.LogError("Player is null! Path recalculation halted.");
            return;
        }
        
        // Determine if the player is in aggro range (using your CanSeePlayer() which uses aggroRange)
        bool inAggro = CanSeePlayer();
        
        // If the player has just entered aggro range, immediately lock on.
        if (inAggro)
        {
            if (!wasInAggroRange)
            {
                if(currentState == State.Patrol){
                    currentState = State.Approach;
                    stateTimer = approachDuration;
                }

                lastKnownPlayerPos = player.position;
                RecalculatePath(); // Instant recalculation upon entry
                pathRecalcTimer = pathRecalcInterval; // Reset timer for subsequent recalculations
                wasInAggroRange = true;
            } 
        }

        else
        {
            wasInAggroRange = false;
        }
        
        // Decrement timers.
        stateTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;
        pathRecalcTimer -= Time.deltaTime;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Only recalc periodically if the player is still in aggro range.
        if (pathRecalcTimer <= 0f && grid != null && inAggro)
        {
            RecalculatePath();
            pathRecalcTimer = pathRecalcInterval;
        }
        
        // --- State Machine Logic ---
        Collider[] hits = Physics.OverlapSphere(transform.position, attackDistance);
        bool playerInOverlap = false;
        foreach (Collider col in hits)
        {
            if (col.CompareTag("Player"))
            {
                playerInOverlap = true;
                break;
            }
        }
        
        if (playerInOverlap && attackTimer <= 0)
        {
            Debug.Log("OverlapSphere trigger: Forcing Attack state.");
            currentState = State.Attack;
            stateTimer = 0.5f;
            attackTriggered = false;
            queuedAttack = false;
        }
        else if (distance <= attackDistance && attackTimer <= 0)
        {
            Debug.Log("Immediate override: distance (" + distance + ") <= attackDistance (" + attackDistance + ").");
            currentState = State.Attack;
            stateTimer = 0.5f;
            attackTriggered = false;
            queuedAttack = false;
        }
        else if (distance <= forceAttackDistance)
        {
            Debug.Log("Force attack: distance (" + distance + ") <= forceAttackDistance (" + forceAttackDistance + ").");
            currentState = State.Attack;
            stateTimer = 0.5f;
            attackTriggered = false;
            queuedAttack = false;
        }
        else if (distance <= attackTriggerRadius)
        {
            if (currentState != State.Attack)
            {
                Debug.Log("Attack trigger: distance (" + distance + ") <= attackTriggerRadius (" + attackTriggerRadius + ").");
                currentState = State.Attack;
                stateTimer = 0.5f;
                attackTriggered = false;
                queuedAttack = false;
            }
            else
            {
                queuedAttack = true;
            }
        }
        else
        {
            // Normal state machine.
            switch (currentState)
            {
                case State.Approach:
                    if (distance <= attackDistance && attackTimer <= 0)
                    {
                        Debug.Log("Approach: distance (" + distance + ") <= attackDistance.");
                        currentState = State.Attack;
                        stateTimer = 0.5f;
                        attackTriggered = false;
                    }
                    else if (stateTimer <= 0)
                    {
                        if (Mathf.Abs(distance - previousDistance) < 0.05f && distance <= attackDistance)
                        {
                            Debug.Log("Approach: Distance nearly unchanged and within attackDistance.");
                            currentState = State.Attack;
                            stateTimer = 0.5f;
                            attackTriggered = false;
                        }
                        else if (distance < previousDistance)
                        {
                            stateTimer = approachDuration * 0.5f;
                            Debug.Log("Approach: Player getting closer. Resetting timer.");
                        }
                        else
                        {
                            stateTimer = approachDuration;
                            Debug.Log("Remaining in Approach.");
                        }
                    }
                    break;

                case State.Strafe:
                    if (stateTimer <= 0)
                    {
                        currentState = State.Approach;
                        stateTimer = approachDuration;
                        Debug.Log("Strafe finished. Switching back to Approach.");
                    }
                    break;

                case State.Retreat:
                    if (stateTimer <= 0)
                    {
                        currentState = State.Approach;
                        stateTimer = approachDuration;
                        Debug.Log("Retreat finished. Switching back to Approach.");
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
                        if (queuedAttack)
                        {
                            stateTimer = 0.5f;
                            queuedAttack = false;
                            attackTriggered = false;
                            Debug.Log("Queued attack: resetting attack state.");
                        }
                        else
                        {
                            attackTimer = attackCooldown;
                            currentState = State.Approach;
                            stateTimer = approachDuration;
                            Debug.Log("Attack complete. Returning to Approach.");
                        }
                    }
                    break;
            }
        }
        
        // If the enemy cannot see the player and there’s no active path, switch to Patrol mode.
        if (!inAggro && (currentPath == null || currentPath.Count == 0))
        {
            currentState = State.Patrol;
        }
        
        // Set the Attack Layer Weight.
        if (currentState == State.Attack)
            anim.SetLayerWeight(anim.GetLayerIndex("Attack"), 1f);
        else
            anim.SetLayerWeight(anim.GetLayerIndex("Attack"), 0f);
        
        // Update animator parameters based on state.
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
            case State.Patrol:
                // Set patrol-specific parameters (if any)
                anim.SetFloat("Vertical", 0.5f, dampTime, Time.deltaTime);
                anim.SetFloat("Horizontal", 0f, dampTime, Time.deltaTime);
                break;
        }
        
        previousDistance = distance;
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Prop"))
        {
            // Calculate rotation away from the obstacle.
            Vector3 collisionNormal = collision.contacts[0].normal;
            Quaternion rotation = Quaternion.FromToRotation(transform.forward, -collisionNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotationSpeed);
            
            // Apply a smoother avoidance force.
            rb.AddForce(collisionNormal * obstacleBumpForce, ForceMode.Force);
        }
        
        if (collision.gameObject.CompareTag("Player"))
        {
            isCollidingWithPlayer = true;
            Debug.Log("Collision Stay with Player.");
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;
        UpdatePathPriorities();

        Vector3 baseMovement = Vector3.zero;

        if (currentState == State.Patrol)
        {
            // In Patrol mode, simply move in the patrol direction.
            patrolTimer -= Time.fixedDeltaTime;
            if (patrolTimer <= 0)
            {
                patrolTimer = patrolSwitchInterval;
                patrolDirection = -patrolDirection; // Reverse direction periodically.
            }
            baseMovement = patrolDirection.normalized * approachSpeed;
            baseMovement = Vector3.Lerp(rb.linearVelocity.normalized, baseMovement.normalized, 0.2f) * approachSpeed;
        }
        else
        {
            // Normal path following.
            Vector3 targetPosition = lastKnownPlayerPos;
            if (currentPath != null && currentPath.Count > 0)
            {
                Node targetNode = currentPath[currentPathIndex];
                targetPosition = targetNode.worldPosition;
                if (Vector3.Distance(transform.position, targetPosition) < waypointThreshold)
                {
                    currentPathIndex++;
                    if (currentPathIndex >= currentPath.Count)
                    {
                        // Do not clear currentPath if the player is out of aggro range;
                        // continue following until the last known position is reached.
                        currentPath = null;
                    }
                }
            }
        
            // Compute global desired direction.
            Vector3 globalDir = (targetPosition - transform.position).normalized;
        
            // Compute local steering with enhanced ledge avoidance.
            Vector3 steering = ComputeSteering();
        
            // Blend the global direction with the steering (avoidance) vector.
            float avoidanceWeight = Mathf.Clamp01(steering.magnitude / (obstacleAvoidanceForce * 0.5f));
            Vector3 desiredDir = Vector3.Slerp(globalDir, steering.normalized, avoidanceWeight);
            float speedFactor = Mathf.Clamp01(1f - (steering.magnitude / (obstacleAvoidanceForce * 2f)));
            baseMovement = desiredDir * approachSpeed * speedFactor;
        }
        
        // Apply any stair movement constraints.
        if (IsOnStairs())
        {
            approachSpeed = Mathf.Clamp(approachSpeed * 1.2f, 1f, 3f);
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, 4f);
            return; // Skip vertical adjustment on stairs
        }
        
        // State-based movement override.
        if (currentState == State.Attack || isCollidingWithPlayer)
            baseMovement = Vector3.zero;
        
        // Rotate smoothly toward the movement direction.
        if (baseMovement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(baseMovement.normalized);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
        
        // Maintain upright orientation.
        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, 0);
        
        // Apply movement.
        rb.MovePosition(rb.position + baseMovement * Time.fixedDeltaTime);
        
        // Ground following and slope handling.
        FollowGround();
        
        // Additional slope stability force.
        RaycastHit groundHit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out groundHit, groundCheckDistance + 1f, groundMask))
        {
            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            if (slopeAngle > 45f)
            {
                rb.AddForce(-groundHit.normal * 50f);
            }
        }
    }


    [Header("Elevation Settings")]
    public float elevationThreshold = 1.5f; // Minimum height difference to use stairs
    public float stairApproachDistance = 3f;

    void UpdatePathPriorities()
    {
        if (grid == null || player == null) return;

        Node playerNode = grid.NodeFromWorldPoint(player.position);
        bool playerOnStairs = playerNode != null && playerNode.movementCost == 3f;

        foreach (Node node in grid.AllNodes)
        {
            if (node == null) continue;
            
            // Make stair endpoints highly desirable
            if (playerOnStairs && node.movementCost == 3f)
            {
                node.movementCost = 0.5f; // Extreme priority
            }
            else if (node.movementCost == 3f)
            {
                node.movementCost = 3f; // Reset
            }
        }
    }

    bool NeedsStairs()
    {
        // More robust stair detection
        float elevationDifference = player.position.y - transform.position.y;
        return Mathf.Abs(elevationDifference) > elevationThreshold && 
            Mathf.Abs(elevationDifference) < 3 * elevationThreshold;
    }

    Vector3 CheckForwardLedge(Vector3 forwardDir)
    {
        int missingGroundCount = 0;
        float sampleInterval = forwardLedgeDetectionDistance / ledgeDetectionSampleCount;

        for (int i = 1; i <= ledgeDetectionSampleCount; i++)
        {
            Vector3 samplePoint = transform.position + 
                forwardDir * (i * sampleInterval) + 
                Vector3.up * 0.5f; // Higher ray origin

            if (!Physics.Raycast(samplePoint, Vector3.down, groundCheckDistance + 0.5f, groundMask))
            {
                missingGroundCount++;
                Debug.DrawRay(samplePoint, Vector3.down * (groundCheckDistance + 0.5f), Color.red);
            }
        }

        // If more than 2/3 of the samples detect no ground, return an avoidance vector
        if (missingGroundCount > (2*ledgeDetectionSampleCount) / 6)
        {
            return -forwardDir * obstacleAvoidanceForce * 2f; // Stronger avoidance
        }

        return Vector3.zero;
    }

    bool IsOnStairs()
    {
        RaycastHit hit;
        return Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f, groundMask) && 
            hit.collider.CompareTag("Stairs");
    }

    [Header("Advanced Steering")]
    public float obstacleBumpForce = 2f;
    public float gapDetectionWidth = 0.5f;
    private Vector3 lastObstacleNormal;

    Vector3 ComputeSteering()
    {
        Vector3 steering = Vector3.zero;
        Vector3 pathDirection = GetPathDirection();
        
        // Existing lateral ledge detection (immediate vicinity)
        Vector3[] ledgeRays = {
            pathDirection,
            pathDirection + transform.right * 0.3f,
            pathDirection - transform.right * 0.3f
        };

        foreach (Vector3 rayDir in ledgeRays)
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            if (!Physics.Raycast(rayStart + rayDir * 0.5f, Vector3.down, groundCheckDistance, groundMask))
            {
                float avoidance = (1 - Vector3.Dot(rayDir.normalized, pathDirection.normalized)) * edgeAvoidanceForce;
                steering += -rayDir.normalized * avoidance;
                Debug.DrawRay(rayStart, rayDir * 2f, Color.yellow);
            }
        }

        // Enhanced obstacle detection with sphere cast
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, obstacleDetectionRadius, pathDirection, out hit,
                            obstacleDetectionDistance, obstacleMask))
        {
            lastObstacleNormal = hit.normal;
            float avoidanceStrength = (1 - (hit.distance / obstacleDetectionDistance)) * obstacleAvoidanceForce;
            Vector3 avoidanceDir = Vector3.Cross(hit.normal, Vector3.up).normalized;
            
            // Add bump rotation
            Quaternion rot = Quaternion.FromToRotation(pathDirection, avoidanceDir);
            pathDirection = rot * pathDirection;
            
            steering += avoidanceDir * avoidanceStrength;
        }

        // Narrow gap detection
        RaycastHit leftHit, rightHit;
        bool leftObstacle = Physics.Raycast(transform.position, -transform.right, out leftHit, gapDetectionWidth, obstacleMask);
        bool rightObstacle = Physics.Raycast(transform.position, transform.right, out rightHit, gapDetectionWidth, obstacleMask);
        
        if (leftObstacle && rightObstacle)
        {
            Vector3 gapCenter = (leftHit.point + rightHit.point) / 2;
            steering += (gapCenter - transform.position).normalized * obstacleAvoidanceForce;
        }
        
        // Forward ledge detection along a ray extending ahead
        Vector3 forwardLedgeSteering = CheckForwardLedge(pathDirection);
        steering += forwardLedgeSteering;
        
        if (steering.magnitude > edgeAvoidanceForce * 0.5f)
        {
            Quaternion safeRot = Quaternion.LookRotation(steering.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, safeRot, rotationSpeed * 2 * Time.fixedDeltaTime);
        }
        
        // Maintain 90% path following priority
        steering = (pathDirection * approachSpeed * 0.8f) + (steering * 0.2f);
        
        return Vector3.Lerp(lastSteering, steering, 0.9f);
    }

    Vector3 GetPathDirection()
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count) 
            return (lastKnownPlayerPos - transform.position).normalized;

        // Direct path to next node with look-ahead
        Vector3 waypointDir = (currentPath[currentPathIndex].worldPosition - transform.position).normalized;
        return waypointDir;
    }

    // Follow the ground so the enemy descends/ascends stairs gracefully.
    void FollowGround()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance + 1f, groundMask))
        {
            float targetY = hit.point.y;
            float newY = Mathf.Lerp(transform.position.y, targetY, groundFollowSpeed * Time.fixedDeltaTime);
            rb.position = new Vector3(rb.position.x, newY, rb.position.z);
        }
        else
        {
            // Emergency stop if no ground detected
            rb.linearVelocity = Vector3.zero;
        }
    }

    // Line-of-sight test.
    bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 direction = (player.position - origin).normalized;
        float maxViewDistance = aggroRange;
        int playerLayer = LayerMask.NameToLayer("Player");
        int combinedMask = obstacleMask | (1 << playerLayer);

        RaycastHit hit;
        bool canSee = false;
        if (Physics.Raycast(origin, direction, out hit, maxViewDistance, combinedMask))
        {
            if (hit.transform == player)
            {
                canSee = true;
                Debug.DrawRay(origin, direction * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(origin, direction * hit.distance, Color.red);
            }
        }
        else
        {
            Debug.DrawRay(origin, direction * maxViewDistance, Color.blue);
        }

        float verticalDifference = Mathf.Abs(player.position.y - origin.y);
        float verticalThreshold = 5f;
        if (verticalDifference > verticalThreshold)
        {
            Debug.Log("Vertical difference too high: " + verticalDifference);
            canSee = false;
        }

        return canSee;
    }

    // Global path recalculation.
    void RecalculatePath()
    {
        if (grid != null && player != null)
        {
            Node playerNode = grid.NodeFromWorldPoint(player.position);
            
            if (playerNode.movementCost == 3f) // Player is on stairs
            {
                // Find nearest stair entrypoint at the enemy's current elevation
                Node stairEntry = grid.FindStairEntrypoint(transform.position);
                
                // Path to stair entrypoint (flat ground)
                List<Node> pathToEntry = AStarPathfinder.FindPath(grid, transform.position, stairEntry.worldPosition);
                
                // Path from entrypoint to player (stairs)
                List<Node> pathFromEntry = AStarPathfinder.FindPath(grid, stairEntry.worldPosition, player.position);

                if (pathToEntry != null && pathFromEntry != null && 
                    pathToEntry.Count > 0 && pathFromEntry.Count > 0)
                {
                    currentPath = new List<Node>(pathToEntry);
                    currentPath.AddRange(pathFromEntry);
                    currentPathIndex = 0;
                }
            }
            else
            {
                // Default pathfinding
                currentPath = AStarPathfinder.FindPath(grid, transform.position, player.position);
                currentPathIndex = 0;
            }
        }
    }

    void OnDrawGizmos()
    {
        // Draw the A* path
        if (currentPath != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i].worldPosition, currentPath[i + 1].worldPosition);
                Gizmos.DrawSphere(currentPath[i].worldPosition, 0.2f);
            }
        }

        // Initialize rayDirections if null (prevents errors in Edit Mode)
        if (rayDirections == null)
        {
            rayDirections = new Vector3[]
            {
                transform.forward,
                transform.forward + transform.right,
                transform.forward - transform.right
            };
        }

        // Draw obstacle detection rays
        if (rayDirections != null)
        {
            Gizmos.color = Color.blue;
            foreach (Vector3 dir in rayDirections)
            {
                Gizmos.DrawRay(transform.position, dir * obstacleDetectionDistance);
            }
        }

        // Draw steering force (only if steering is initialized)
        if (steering != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, steering * 2f);
        }

        if (grid != null && grid.Nodes != null)
        {
            foreach (Node n in grid.Nodes)
            {
                if (n != null && n.movementCost == 3f)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (grid.nodeDiameter * 0.8f));
                    Debug.DrawLine(n.worldPosition, n.worldPosition + Vector3.up * 2, Color.cyan);
                }
            }
        }
    }

    // Collision callbacks to detect if enemy is in contact with the player.
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isCollidingWithPlayer = true;
            Debug.Log("Collision Enter with Player.");
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isCollidingWithPlayer = false;
            Debug.Log("Collision Exit with Player.");
        }
    }

    // Update last known player position.
    void LateUpdate()
    {
        if (player != null && CanSeePlayer())
        {
            lastKnownPlayerPos = player.position;
        }
    }
}