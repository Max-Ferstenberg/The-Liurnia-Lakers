using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public float approachSpeed = 1.5f;
    public float strafeSpeed = 0.75f;
    public float retreatSpeed = 0.75f;
    public float rotationSpeed = 3f;

    public float attackDistance = 1f;
    public float approachDuration = 2f;
    public float strafeTime = 1f;
    public float retreatTime = 1.5f;
    public float attackCooldown = 3.0f;

    private AudioSource enemyAudioSource;
    public AudioClip stunSound;
    public AudioClip deathSound;

    private Rigidbody rb;
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

    private GameObject hoop;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        enemyAudioSource = GetComponent<AudioSource>();
        hoop = transform.Find("Hoop").gameObject;

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
        enemyAudioSource.PlayOneShot(deathSound);
        StartCoroutine(DeathAnimation());
    }

    private IEnumerator DeathAnimation()
    {
        anim.Play(anim.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);
        yield return new WaitForSeconds(2f);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float fadeDuration = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    Color color = material.color;
                    color.a = alpha;
                    material.color = color;
                }
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (player == null || stunned)
            return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

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

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }
}
