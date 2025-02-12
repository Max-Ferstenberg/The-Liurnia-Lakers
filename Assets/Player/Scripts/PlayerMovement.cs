using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cameraTransform;
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    private Vector3 velocity;
    private bool isGrounded;

    public Animator animator;
    public BallDribble ballDribbler; // hehe, haha, hue even

    private bool isSliding = false;
    public float slideSpeedMultiplier = 1.5f; 
    public float slideDuration = 1.0335f;

    public Vector3 GetVelocity()
    {
        return velocity;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            if (animator.GetBool("IsJumping")) {
                ballDribbler.isHeld = false;
                animator.SetBool("IsJumping", false);
            }
            if (animator.GetBool("SprintJump")) {
                ballDribbler.isHeld = false;
                animator.SetBool("SprintJump", false);
            }
        }

        if (isGrounded && !isSliding)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                StartCoroutine(Slide());
                return; 
            }
        }

        if (isSliding)
        {
            Gravity();
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical);

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDir = (camForward * vertical + camRight * horizontal);
        float inputMagnitude = moveDir.magnitude;

        animator.SetBool("IsSprinting", false);

        bool isWalking = inputDir.magnitude > 0.001f && velocity.y < 0;
        animator.SetBool("IsWalking", isWalking);

        if (inputMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.15f);
        }
        else
        {
            Vector3 camFwd = cameraTransform.forward;
            camFwd.y = 0;
            if (camFwd.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camFwd);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
            }
        }

        float currentSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= sprintMultiplier;
            animator.SetBool("IsSprinting", true);
        }

        velocity.x = moveDir.x * currentSpeed;
        velocity.z = moveDir.z * currentSpeed;

        controller.Move(velocity * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator.GetBool("IsSprinting"))
            {
                animator.SetBool("SprintJump", true);
                if (animator.GetBool("IsDribbling")) {
                    ballDribbler.isHeld = true;
                }
            }
            else
            {
                animator.SetBool("IsJumping", true);
                if (animator.GetBool("IsDribbling")) {
                    ballDribbler.isHeld = true;
                }
            }
        }

        Gravity();
    }

    void Gravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    IEnumerator Slide()
    {
        isSliding = true;
        if (animator.GetBool("IsDribbling")) {
                    ballDribbler.isHeld = true;
                }
        animator.SetBool("IsSliding", true);
        
        float timer = 0f;
        Vector3 slideDir = transform.forward;
        float slideSpeed = walkSpeed * slideSpeedMultiplier;
        
        while (timer < slideDuration)
        {
            controller.Move(slideDir * slideSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
        
        animator.SetBool("IsSliding", false);
        isSliding = false;
        ballDribbler.isHeld = false;
    }
}
