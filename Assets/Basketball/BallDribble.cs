using UnityEngine;
using System.Collections;

public class BallDribble : MonoBehaviour
{
    [SerializeField] private Transform handTarget;
    [SerializeField] private float catchRadius;
    [SerializeField] private float pullForce;
    [SerializeField] private float pushForce;
    public KeyCode throwButton;
    public AudioSource bounceAudioSource;
    public AudioClip bounceSound;
    public float throwForce;
    private Rigidbody rb;
    private bool isPalmed = false;
    private bool isDribbling = false;
    private bool bounced = true;
    public bool isHeld = false;
    private bool lockedToHand = false;
    public PlayerMovement playerMovement;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (isDribbling == false) {
            rb.useGravity = true;
        }
        if (Input.GetKey(throwButton))
        {
            if (lockedToHand == true) {
                throwBall();
            }
            else if (isDribbling == true) {
                GrabBall(pullForce * 2, false);
                if (lockedToHand == true) {
                    transform.position = handTarget.position;
                    throwBall();
                }
            }
        }


        if (isHeld)
        {
            if (isPalmed)
            {
                isPalmed = false;
                rb.linearVelocity = playerMovement.GetVelocity();
            }
            GrabBall(pullForce * 2, false);
        }
        else 
        {
            if (isDribbling)
            {
                Vector3 newPosition = new Vector3(handTarget.position.x, Mathf.Min(handTarget.position.y, transform.position.y), handTarget.position.z);
                transform.position = newPosition;
                if (isPalmed)
                {
                    GrabBall(pullForce, false);
                }
            }
        }
    }

    public void TryCatchBall()
    {
        if (isDribbling == false)
        {
            CatchBall(pullForce / 5, catchRadius, catchRadius / 3, false, true);
        }
        else if (bounced == true)
        {
            CatchBall(pullForce, catchRadius * 3, catchRadius * 3, true, false);
        }
        else {
            ReleaseBall();
        }
    }

    private void CatchBall(float force, float verticalRange, float horizontalRange, bool catchAboveHand, bool isMagic)
    {
        Vector3 ballPosition = transform.position;
        Vector3 handPosition = handTarget.position;

        float horizontalDistance = Vector2.Distance(new Vector2(ballPosition.x, ballPosition.z), new Vector2(handPosition.x, handPosition.z));
        float verticalDistance = ballPosition.y - handPosition.y;


        if ((((verticalDistance <= 0) || (catchAboveHand == false)) && (Mathf.Abs(verticalDistance) <= verticalRange)) && (horizontalDistance <= horizontalRange)){
            isPalmed = true;
            rb.useGravity = false;
            isDribbling = true;
            GrabBall(force, isMagic);
        }
        else {
            ReleaseBall();
        }

    }

    public void BounceBall()
    {
        Vector3 releaseVelocity = playerMovement.GetVelocity();
        isPalmed = false;
        isHeld = false;
        lockedToHand = false;
        bounced = false;
        if (isDribbling) {
            releaseVelocity.y -= pushForce;
            rb.linearVelocity = releaseVelocity;
        }
        rb.useGravity = true;        
    }

    private void GrabBall(float force, bool isMagic)
    {
        bounced = true;
         if (Vector3.Distance(transform.position, handTarget.position) < 0.5)
            {
                lockedToHand = true;
            }

            if (lockedToHand == true) {
                transform.position = handTarget.position;
            }
            else {
                Vector3 direction = (handTarget.position - transform.position);
                rb.linearVelocity += direction * force;
            }
    }

    public void ReleaseBall()
    {
        if (isPalmed == true || isHeld == true) {
            BounceBall();
        }
        isHeld = false;
        lockedToHand = false;
        bounced = true;
        isDribbling = false;
    }

    public void throwBall()
    {
        isHeld = false;
        isPalmed = false;
        lockedToHand = false;
        isDribbling = false;
        rb.useGravity = true;
        rb.linearVelocity = playerMovement.GetVelocity();
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.Normalize();
        rb.AddForce(cameraForward * throwForce, ForceMode.Impulse);
    }


    private void OnCollisionEnter(Collision collision)
    {
        StartCoroutine(CheckBounce());
    }

    private IEnumerator CheckBounce()
    {
        yield return new WaitForFixedUpdate();
        if (rb.linearVelocity.y > 0 && isDribbling == true)
        {
            bounced = true;
        }
        float speed = rb.linearVelocity.magnitude;
        bounceAudioSource.volume = Mathf.Clamp(speed / 20f, 0.2f, 1f);
        bounceAudioSource.PlayOneShot(bounceSound);
    }

}