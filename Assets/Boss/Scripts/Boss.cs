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

    [Header("Tracking Settings")]
    [Tooltip("How fast the boss rotates (affects smoothing). Lower values make it legiss exact.")]
    public float turnSpeed = 30f;
    [Tooltip("Angular difference (in degrees) above which turning animation is triggered.")]
    public float turningThreshold = 10f;

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

    public bool isFlying = false;

    private float attackTimer = 0f;

    private enum AttackType { ClawAttack, StandingFireBreath, FlyingFireBreath, MeteorAttack }

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

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
                animator.SetTrigger("FlyDown");
                yield return new WaitForSeconds(flyDownDuration);
                isFlying = false;
                break;

            case AttackType.MeteorAttack:
                if (isFlying)
                {
                    animator.SetTrigger("FlyDown");
                    yield return new WaitForSeconds(flyDownDuration);
                    isFlying = false;
                }
                animator.SetTrigger("MeteorAttack");
                yield return new WaitForSeconds(1.5f);
                break;
        }
        yield return null;
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
            GameObject effect = Instantiate(standingFirePrefab, mouthTransform.position, spawnRotation, mouthTransform);
        }
    }

    public void SpawnFlyingFireEffect()
    {
        if (flyingFirePrefab != null && mouthTransform != null)
        {
            Quaternion spawnRotation = mouthTransform.rotation;
            GameObject effect = Instantiate(flyingFirePrefab, mouthTransform.position, spawnRotation, mouthTransform);
        }
    }

    public void SpawnMeteorEffect()
    {
        if (meteorPrefab != null && player != null)
        {
            // Meteor effect spawns at the player's position.
            Instantiate(meteorPrefab, player.position, Quaternion.identity);
        }
    }

}