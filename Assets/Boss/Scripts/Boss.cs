using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour
{
    public Transform player;
    public Animator animator; 
    public GameObject standingFirePrefab;
    public GameObject flyingFirePrefab;
    public GameObject meteorPrefab;
    public Transform mouthTransform;
    public Transform clawTransform;

    [Header("Tracking Settings")]
    [Tooltip("How fast the boss rotates (affects smoothing). Lower values make it less exact.")]
    public float turnSpeed = 30f;
    [Tooltip("Angular difference (in degrees) above which turning animation is triggered.")]
    public float turningThreshold = 10f;

    [Header("Aggro Settings")]
    [Tooltip("The maximum distance at which the boss will attack the player.")]
    public float aggroRange = 15f;
    [Tooltip("Layers considered obstacles for line-of-sight.")]
    public LayerMask obstacleMask;

    [Header("Attack Timing")]
    [Tooltip("Time (in seconds) between attacks.")]
    public float attackInterval = 3f;
    
    [Header("Attack Range Settings")]
    [Tooltip("Maximum distance for a claw attack.")]
    public float clawRange = 5f;
    [Tooltip("Distance beyond which the boss prefers standing fire breath.")]
    public float farDistanceThreshold = 10f;
    [Tooltip("Elevation difference (Y) to consider the player at similar elevation.")]
    public float elevationThreshold = 2f;
    
    [Header("Attack Randomness")]
    [Tooltip("Chance (0-1) to perform a Meteor Attack regardless of conditions.")]
    public float meteorAttackChance = 0.2f;
    
    [Header("Flying Sequence Durations")]
    public float flyUpDuration = 1.5f;
    public float flyingAttackDuration = 1.5f;
    public float flyDownDuration = 1.5f;

    [Header("Damage Values")]
    public float clawAttackDamage = 15f;
    public float standingFireDamage = 20f;
    public float flyingFireDamage = 20f;
    public float meteorDamage = 40f;

    [Header("Meteor Attack Effects")]
    [Tooltip("Sound cue for the meteor warning.")]
    public AudioSource audioSource;
    public AudioClip meteorWarningSound;

    [Header("Standing Fire Offset")]
    [Tooltip("Offset to apply when spawning the standing fire effect (e.g. lower the spawn position).")]
    public Vector3 standingFireOffset;

    private float attackTimer = 0f;

    public float originalGroundY;
    public float flyingHeightOffsetAdjustment = 0f; 

    private enum AttackType { ClawAttack, StandingFireBreath, FlyingFireBreath, MeteorAttack }

    public bool isFlying = false;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
        if (animator == null)
            animator = GetComponent<Animator>();

        originalGroundY = transform.position.y;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > aggroRange || !CanSeePlayer())
        {
            return;
        }
        
        Vector3 toPlayer = player.position - transform.position;
        Vector3 flatDirection = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection);
            float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
            bool turning = angleDiff > turningThreshold;
            animator.SetBool("IsTurning", turning);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime * 0.5f);
        }
        else
        {
            animator.SetBool("IsTurning", false);
        }
        
        animator.SetBool("IsFlying", isFlying);
        
        attackTimer += Time.deltaTime;
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
        if (Physics.SphereCast(origin, sphereRadius, direction, out hit, aggroRange, obstacleMask))
        {
            return (hit.transform == player);
        }
        return true;
    }

    public void AdjustFlyingHeight()
    {
        float currentMouthOffset = mouthTransform.position.y - transform.position.y;
        Vector3 newPos = transform.position;
        newPos.y = player.position.y - currentMouthOffset + flyingHeightOffsetAdjustment;
        transform.position = newPos;
    }
    
    private IEnumerator PerformAttack(AttackType attack)
    {
        switch (attack)
        {
            case AttackType.ClawAttack:
                if (isFlying)
                {
                    animator.SetTrigger("FlyDown");
                    yield return new WaitForSeconds(flyDownDuration);
                    isFlying = false;
                }
                animator.SetTrigger("ClawAttack");
                yield return new WaitForSeconds(0.5f);
                DealClawDamage(clawAttackDamage, clawRange);
                break;

            case AttackType.StandingFireBreath:
                if (isFlying)
                {
                    animator.SetTrigger("FlyDown");
                    yield return new WaitForSeconds(flyDownDuration);
                    isFlying = false;
                }
                animator.SetTrigger("StandingFireBreath");
                yield return new WaitForSeconds(1.5f);
                break;

            case AttackType.FlyingFireBreath:
                if (!isFlying)
                {
                    animator.SetTrigger("FlyUp");
                    yield return new WaitForSeconds(flyUpDuration);
                    isFlying = true;
                }
                animator.SetTrigger("FlyingFireBreath");
                yield return new WaitForSeconds(flyingAttackDuration);
                break;

            case AttackType.MeteorAttack:
                while (animator.GetCurrentAnimatorStateInfo(0).IsName("FlyUp") || animator.GetCurrentAnimatorStateInfo(0).IsName("FlyDown"))
                {
                    yield return null;
                }
                if (isFlying)
                {
                    yield return new WaitForSeconds(flyDownDuration);
                    isFlying = false;
                }
                animator.SetTrigger("MeteorAttack");
                if (meteorWarningSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(meteorWarningSound);
                }
                yield return new WaitForSeconds(1f);
                Vector3 targetPos = player.position;
                yield return new WaitForSeconds(0.4f);
                Instantiate(meteorPrefab, targetPos, Quaternion.identity);
                break; ;
        }
        yield return null;
    }

    void DealClawDamage(float damage, float effectiveRange)
    {
        float distanceClaw = Vector3.Distance(clawTransform.position, player.position);
        float distanceMouth = Vector3.Distance(mouthTransform.position, player.position);
        Debug.Log("Claw Damage Check -- Claw Distance: " + distanceClaw + ", Mouth Distance: " + distanceMouth + ", Range: " + effectiveRange);
        if (distanceClaw <= effectiveRange || distanceMouth <= effectiveRange)
        {
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                Debug.Log("Applying claw damage: " + damage);
                pm.TakeDamageFromBoss(damage);
            }
        }
        else
        {
            Debug.Log("Player not in contact with claw/mouth for damage.");
        }
    }

    private AttackType ChooseAttack()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        float elevDiff = player.position.y - transform.position.y;
        bool similarElev = Mathf.Abs(elevDiff) <= elevationThreshold;
        bool closeForClaw = distance <= clawRange && similarElev;
        bool farForStanding = distance >= farDistanceThreshold && similarElev;
        bool playerIsHigher = elevDiff > elevationThreshold;

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