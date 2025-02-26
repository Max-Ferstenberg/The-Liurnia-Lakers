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
    public bool isGrounded;
    
    public Animator animator;
    public BallDribble ballDribbler; // Your ball dribble component
    
    public bool isSliding = false;
    public float slideSpeedMultiplier = 1.5f;
    public float slideDuration = 1.0335f;
    
    public AudioSource audioSource;
    public AudioClip stepSound;
    
    // ---------------- Stamina System ----------------
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float sprintStaminaCost = 5f;
    public float jumpStaminaCost = 10f;
    public float slideStaminaCost = 20f;
    public float staminaRegenRate = 20f;
    public float staminaRegenDelay = 2f;
    private float staminaUseTimer = 0f;
    
    public SliderColorChanger staminaBar;
    // --------------------------------------------------
    
    // ---------------- Health & Knockback ----------------
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float knockbackForce = 10f;
    public SliderColorChanger healthBar;
    public bool ignoreKnockback = false;

    // ------------------------------------------------------
    
    public float knockbackDuration = 0.5f; // Minimum duration to block input
    public bool isKnockbackActive = false;
    
    // Damage cooldown: only one damage instance per knockback trigger
    private float lastDamageTime = -100f;
    public float damageCooldown = 0.5f;
    
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
        if (isKnockbackActive)
        {
            ApplyGravity();
            controller.Move(velocity * Time.deltaTime);
            return;
        }
        
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            if (animator.GetBool("IsJumping"))
            {
                step();
                animator.SetBool("IsJumping", false);
            }
            if (animator.GetBool("SprintJump"))
            {
                step();
                animator.SetBool("SprintJump", false);
            }
        }
        
        if (isGrounded && !isSliding)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (currentStamina >= slideStaminaCost)
                {
                    currentStamina -= slideStaminaCost;
                    staminaUseTimer = 0f;
                    if (staminaBar != null)
                        staminaBar.UpdateBar(currentStamina / maxStamina);
                    StartCoroutine(Slide());
                    return;
                }
                else
                {
                    // Sound effect?
                }
            }
        }
        
        if (isSliding)
        {
            ApplyGravity();
            return;
        }
        
        float horizontal = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, verticalInput);
        
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();
        
        Vector3 moveDir = (camForward * verticalInput + camRight * horizontal);
        float inputMagnitude = moveDir.magnitude;
        
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
        
        if (Input.GetKey(KeyCode.LeftShift) && currentStamina > 0f)
        {
            moveDir *= sprintMultiplier;
            animator.SetBool("IsSprinting", true);
            currentStamina = Mathf.Max(0f, currentStamina - sprintStaminaCost * Time.deltaTime);
            staminaUseTimer = 0f;
        }
        else
        {
            animator.SetBool("IsSprinting", false);
        }
        
        bool isWalking = inputDir.magnitude > 0.001f && velocity.y < 0;
        animator.SetBool("IsWalking", isWalking);
        
        velocity.x = moveDir.x * walkSpeed;
        velocity.z = moveDir.z * walkSpeed;
        
        controller.Move(velocity * Time.deltaTime);
        
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            if (currentStamina >= jumpStaminaCost)
            {
                currentStamina -= jumpStaminaCost;
                staminaUseTimer = 0f;
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator.GetBool("IsSprinting"))
                {
                    animator.SetBool("SprintJump", true);
                    if (animator.GetBool("IsDribbling"))
                        ballDribbler.isHeld = true;
                }
                else
                {
                    animator.SetBool("IsJumping", true);
                    if (animator.GetBool("IsDribbling"))
                        ballDribbler.isHeld = true;
                }
            }
        }
        
        ApplyGravity();
        
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetButtonDown("Jump"))
        {
            staminaUseTimer += Time.deltaTime;
            if (staminaUseTimer >= staminaRegenDelay)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            }
        }
        
        if (staminaBar != null)
        {
            staminaBar.UpdateBar(currentStamina / maxStamina);
        }
    }
    
    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    void step()
    {
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(stepSound);
    }
    
    IEnumerator Slide()
    {
        isSliding = true;
        if (animator.GetBool("IsDribbling"))
            ballDribbler.isHeld = true;
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
    }
    
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Time.time - lastDamageTime < damageCooldown)
            return;
        
        if (hit.gameObject.CompareTag("SpinningLogTrap"))
        {
            lastDamageTime = Time.time;
            if (!isKnockbackActive)
            {
                isKnockbackActive = true;
                TakeDamage(25, hit);
            }
        }
        else if (hit.gameObject.CompareTag("SwingingAxe"))
        {
            lastDamageTime = Time.time;
            if (!isKnockbackActive)
            {
                isKnockbackActive = true;
                TakeDamage(20, hit);
            }
        }
    }
    
    public void TakeDamage(float damage, ControllerColliderHit hit)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
            currentHealth = 0;
        if (healthBar != null)
            healthBar.UpdateBar(currentHealth / maxHealth);
        
        animator.Play("Knockback", 0, 0f);
        
        Vector3 knockbackDir;
        if (!ignoreKnockback)  // Only apply if not ignoring knockback.
        {
            if (hit != null)
            {
                knockbackDir = -new Vector3(velocity.x, 0, velocity.z);
                if (knockbackDir == Vector3.zero)
                {
                    knockbackDir = -transform.forward;
                }
            }
            else
            {
                knockbackDir = new Vector3(0, 0, -1);
            }
            knockbackDir.Normalize();
            
            velocity.x = knockbackDir.x * knockbackForce;
            velocity.z = knockbackDir.z * knockbackForce;
        }
        else
        {
            // When ignoring knockback, just zero out velocity.
            velocity.x = 0;
            velocity.z = 0;
        }
        
        StartCoroutine(KnockbackRecovery());
    }

    public void OverrideVelocity(Vector3 newVelocity)
    {
        velocity.x = 0;
        velocity.y = 0;
        velocity = newVelocity;
        ApplyGravity();
    }


    
    IEnumerator KnockbackRecovery()
    {
        float elapsed = 0f;
        while (elapsed < knockbackDuration || animator.GetCurrentAnimatorStateInfo(0).IsName("Knockback"))
        {
            elapsed += Time.deltaTime;
            ApplyGravity();
            controller.Move(velocity * Time.deltaTime);
            yield return null;
        }
        isKnockbackActive = false;
    }

    public void TakeDamageFromBoss(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
            currentHealth = 0;
        if (healthBar != null)
            healthBar.UpdateBar(currentHealth / maxHealth);
    }

}