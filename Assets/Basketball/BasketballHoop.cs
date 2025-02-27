using UnityEngine;

public class BasketballHoop : MonoBehaviour
{
    public bool dunking = false;
    public float detectionRadius = 2f;
    public float dunkHeightThreshold = 1f;

    private Transform playerTransform;
    private PlayerMovement playerMovement;
    private Animator playerAnimator;
    private EnemyAI parentEnemy;
    private MeshCollider hoopCollider;
    private EndingHandler endingHandler;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        playerTransform = playerObj.transform;
        playerMovement = playerObj.GetComponent<PlayerMovement>();
        playerAnimator = playerObj.GetComponent<Animator>();
        parentEnemy = GetComponentInParent<EnemyAI>();
        hoopCollider = GetComponent<MeshCollider>();
        endingHandler = FindObjectOfType<EndingHandler>();
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float heightDifference = playerTransform.position.y - transform.position.y;
        if (distanceToPlayer <= detectionRadius && playerMovement.isGrounded == false && heightDifference >= dunkHeightThreshold && dunking == false)
        {
            TriggerDunk();
        }
    }

    void TriggerDunk()
    {
        dunking = true;
        playerMovement.enabled = false;
        playerMovement.controller.enabled = false;
        playerTransform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Quaternion hoopRotation = transform.rotation;
        playerTransform.rotation = Quaternion.Euler(0, hoopRotation.eulerAngles.y + 180f, 0);
        playerAnimator.SetTrigger("Dunk");
        if (hoopCollider != null)
        {
            hoopCollider.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Basketball"))
        {
            if (parentEnemy != null)
            {
                parentEnemy.Kill();
            }
            else if (dunking == true)
            {
                endingHandler.DunkWin();
            }
            else
            {
                endingHandler.ScoreWin();
            }
        }
    }

    public void EndDunk()
    {
        dunking = false;
        playerMovement.enabled = true;
        playerMovement.controller.enabled = true;
        if (hoopCollider != null)
        {
            hoopCollider.enabled = true;
        }
    }
}




