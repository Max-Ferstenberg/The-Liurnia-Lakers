using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour
{
    // -------------------- External References --------------------
    public Transform player;                
    public Animator animator;             
    public GameObject standingFirePrefab;     
    public GameObject flyingFirePrefab;  
    public GameObject meteorPrefab;      
    public Transform mouthTransform;      
    public Transform clawTransform;       

    // -------------------- Tracking Settings --------------------
    [Header("Tracking Settings")]
    [Tooltip("How fast the boss rotates (affects smoothing). Lower values make it less exact.")]
    public float turnSpeed = 30f;          
    [Tooltip("Angular difference (in degrees) above which turning animation is triggered.")]
    public float turningThreshold = 10f;   

    // -------------------- Aggro Settings --------------------
    [Header("Aggro Settings")]
    [Tooltip("The maximum distance at which the boss will attack the player.")]
    public float aggroRange = 15f;       
    [Tooltip("Layers considered obstacles for line-of-sight.")]
    public LayerMask obstacleMask;       

    // -------------------- Attack Timing --------------------
    [Header("Attack Timing")]
    [Tooltip("Time (in seconds) between attacks.")]
    public float attackInterval = 3f;    
    
    // -------------------- Attack Range Settings --------------------
    [Header("Attack Range Settings")]
    [Tooltip("Maximum distance for a claw attack.")]
    public float clawRange = 5f;       
    [Tooltip("Distance beyond which the boss prefers standing fire breath.")]
    public float farDistanceThreshold = 10f;
    [Tooltip("Elevation difference (Y) to consider the player at similar elevation.")]
    public float elevationThreshold = 2f; 
    
    // -------------------- Attack Randomness --------------------
    [Header("Attack Randomness")]
    [Tooltip("Chance (0-1) to perform a Meteor Attack regardless of conditions.")]
    public float meteorAttackChance = 0.2f;  
    
    // -------------------- Flying Sequence Durations --------------------
    [Header("Flying Sequence Durations")]
    public float flyUpDuration = 1.5f;
    public float flyingAttackDuration = 1.5f;
    public float flyDownDuration = 1.5f;
    
    // -------------------- Damage Values --------------------
    [Header("Damage Values")]
    public float clawAttackDamage = 15f;
    public float standingFireDamage = 20f;
    public float flyingFireDamage = 20f;
    public float meteorDamage = 40f;
    
    // -------------------- Meteor Attack Effects --------------------
    [Header("Meteor Attack Effects")]
    [Tooltip("Sound cue for the meteor warning.")]
    public AudioSource audioSource;
    public AudioClip meteorWarningSound;

    // -------------------- Standing Fire Offset --------------------
    [Header("Standing Fire Offset")]
    [Tooltip("Offset to apply when spawning the standing fire effect (e.g. lower the spawn position).")]
    public Vector3 standingFireOffset;

    private float attackTimer = 0f;
    public float originalGroundY;
    public float flyingHeightOffsetAdjustment = 0f;
    public bool isFlying = false; 

    private enum AttackType { ClawAttack, StandingFireBreath, FlyingFireBreath, MeteorAttack }

    void Start()
    {
        //If the player reference isn't assigned, attempt to find the player by tag
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
        //Ensure the animator component is assigned
        if (animator == null)
            animator = GetComponent<Animator>();

        originalGroundY = transform.position.y;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        //Do not proceed if the player is out of range or not visible
        if (distance > aggroRange || !CanSeePlayer())
        {
            return;
        }
        
        Vector3 toPlayer = player.position - transform.position;
        Vector3 flatDirection = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection);
            //Measure the angular difference between the current and target rotations
            float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
            bool turning = angleDiff > turningThreshold;
            animator.SetBool("IsTurning", turning);

            //rotate the boss toward the target rotation (player position)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime * 0.5f);
        }
        else
        {
            animator.SetBool("IsTurning", false);
        }
        
        animator.SetBool("IsFlying", isFlying);
        
        attackTimer += Time.deltaTime;
        //When timer exceeds the defined attack interval select and perform an attack
        if (attackTimer >= attackInterval)
        {
            AttackType chosenAttack = ChooseAttack();
            StartCoroutine(PerformAttack(chosenAttack));
            attackTimer = 0f;
        }
    }

    bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 direction = (player.position - origin).normalized;
        float sphereRadius = 0.5f;
        RaycastHit hit;
        //Cast a sphere to detect any obstacles between the boss and the player
        if (Physics.SphereCast(origin, sphereRadius, direction, out hit, aggroRange, obstacleMask))
        {
            //If the first object hit is the player, the boss can see the player
            return (hit.transform == player);
        }
        //If no obstacle is detected, assume the boss can see the player
        return true;
    }

    public void AdjustFlyingHeight()
    {
        float currentMouthOffset = mouthTransform.position.y - transform.position.y;
        Vector3 newPos = transform.position;
        //Adjust the boss's height so that the mouth aligns with the player's vertical position, so that the fire breath will hit the player
        newPos.y = player.position.y - currentMouthOffset + flyingHeightOffsetAdjustment;
        transform.position = newPos;
    }
        
    //Coroutine for attacks
    private IEnumerator PerformAttack(AttackType attack)
    {
        switch (attack)
        {
            case AttackType.ClawAttack:
                // If the boss is flying, initiate landing before clawing
                if (isFlying)
                {
                    animator.SetTrigger("FlyDown");
                    yield return new WaitForSeconds(flyDownDuration);
                    isFlying = false;
                }
                animator.SetTrigger("ClawAttack");
                yield return new WaitForSeconds(0.5f); //Allow time for the attack animation to reach the damage frame
                DealClawDamage(clawAttackDamage, clawRange);
                break;

            case AttackType.StandingFireBreath:
                //Ensure the boss is on the ground before using the standing fire attack
                if (isFlying)
                {
                    animator.SetTrigger("FlyDown");
                    yield return new WaitForSeconds(flyDownDuration);
                    isFlying = false;
                }
                animator.SetTrigger("StandingFireBreath");
                yield return new WaitForSeconds(1.5f); //Wait for the animation to complete
                break;

            case AttackType.FlyingFireBreath:
                //If not already flying, transition into flying state
                if (!isFlying)
                {
                    animator.SetTrigger("FlyUp");
                    yield return new WaitForSeconds(flyUpDuration);
                    isFlying = true;
                }
                animator.SetTrigger("FlyingFireBreath");
                yield return new WaitForSeconds(flyingAttackDuration); //Wait for the attack to complete
                break;

            case AttackType.MeteorAttack:
                //Wait until any fly transitions are complete before launching the meteor attack
                while (animator.GetCurrentAnimatorStateInfo(0).IsName("FlyUp") || animator.GetCurrentAnimatorStateInfo(0).IsName("FlyDown"))
                {
                    yield return null;
                }
                //Transitions back to standing
                if (isFlying)
                {
                    yield return new WaitForSeconds(flyDownDuration);
                    isFlying = false;
                }
                //Trigger the meteor attack animation
                animator.SetTrigger("MeteorAttack");
                if (meteorWarningSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(meteorWarningSound);
                }
                yield return new WaitForSeconds(1f); //Wait before launching the meteor
                Vector3 targetPos = player.position; //Aim directly at the player's current position (gives the player time to dodge after the sound cue)
                yield return new WaitForSeconds(0.4f);
                //Spawn the meteor at the target position
                Instantiate(meteorPrefab, targetPos, Quaternion.identity);
                break;
        }
        yield return null;
    }

    //Checks if the player is within melee range for a claw attack and applies damage if so
    void DealClawDamage(float damage, float effectiveRange)
    {
        //Calculate the distance from the claw and mouth to the player
        float distanceClaw = Vector3.Distance(clawTransform.position, player.position);
        float distanceMouth = Vector3.Distance(mouthTransform.position, player.position);
        //If the player is within effective range of either, apply damage.
        if (distanceClaw <= effectiveRange || distanceMouth <= effectiveRange)
        {
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.TakeDamageFromBoss(damage);
            }
        }
    }

    //Chooses an attack type based on the player's distance, elevation, and a random chance
    private AttackType ChooseAttack()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        //Calculate vertical difference between the player and the boss
        float elevDiff = player.position.y - transform.position.y;
        //Determine if the player is on a similar elevation as the boss
        bool similarElev = Mathf.Abs(elevDiff) <= elevationThreshold;
        //Determine if the player is within range for a melee attack
        bool closeForClaw = distance <= clawRange && similarElev;
        //Determine if the player is far enough for a standing fire attack
        bool farForStanding = distance >= farDistanceThreshold && similarElev;
        //Check if the player is higher than the boss
        bool playerIsHigher = elevDiff > elevationThreshold;

        //Randomly favor a meteor attack if chance criteria are met
        if (Random.value < meteorAttackChance)
            return AttackType.MeteorAttack;
        if (closeForClaw)
            return AttackType.ClawAttack;
        else if (playerIsHigher)
            return AttackType.FlyingFireBreath;
        else if (farForStanding)
            return AttackType.StandingFireBreath;
        else
            return AttackType.MeteorAttack;
    }

    //Uses boss's mouth transform to instantiate prefabs with hitboxes at the mouth to create a fire breathing effect
    public void SpawnStandingFireEffect()
    {
        if (standingFirePrefab != null && mouthTransform != null)
        {
            Quaternion spawnRotation = mouthTransform.rotation;
            Instantiate(standingFirePrefab, mouthTransform.position + standingFireOffset, spawnRotation, mouthTransform);
        }
    }

    public void SpawnFlyingFireEffect()
    {
        if (flyingFirePrefab != null && mouthTransform != null)
        {
            Quaternion spawnRotation = mouthTransform.rotation;
            Instantiate(flyingFirePrefab, mouthTransform.position, spawnRotation, mouthTransform);
        }
    }
}