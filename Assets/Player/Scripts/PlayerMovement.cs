using UnityEngine;
using System.Collections;

// Ensure that the GameObject has a CharacterController component attached.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    //Transforms
    public CharacterController controller;       
    public Transform cameraTransform;           
    
    //Movement settings
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    private Vector3 velocity;
    public bool isGrounded;
    
    //Animation
    public Animator animator;
    public BallDribble ballDribbler;

    //Dodging
    public bool isSliding = false;
    public float slideSpeedMultiplier = 1.5f;
    public float slideDuration = 1.0335f;          // Duration of the slide animation
    
    //Audio
    public AudioSource audioSource;             
    public AudioClip stepSound;             

    //Stamina System
    public float maxStamina = 100f;             
    public float currentStamina = 100f;     
    public float sprintStaminaCost = 5f;        
    public float jumpStaminaCost = 10f;     
    public float slideStaminaCost = 20f;    
    public float staminaRegenRate = 20f;   
    public float staminaRegenDelay = 2f;    
    private float staminaUseTimer = 0f; 
    
    public SliderColorChanger staminaBar;
    
    //Health & Knockback (Player Damage/Lose Conditions)
    public float maxHealth = 100f;             
    public float currentHealth = 100f; 
    public float knockbackForce = 10f;
    public SliderColorChanger healthBar; 
    public bool ignoreKnockback = false; 

    
    public float knockbackDuration = 0.5f;       //Minimum time duration to block player input after a knockback
    public bool isKnockbackActive = false;
    
    //ensures damage is only applied once
    private float lastDamageTime = -100f;
    public float damageCooldown = 0.5f;
    
    //Getter for velocity, used globally in other scripts
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
        //If knockback is active (i.e. player has been hit), apply gravity and update movement without further input
        if (isKnockbackActive)
        {
            ApplyGravity();
            controller.Move(velocity * Time.deltaTime);
            return;
        }
        
        //Prevents mid air jumping
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
        
        //Initiate slide
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
            }
        }
        
        //During a slide apply gravity forcibly as well as horizontal forces
        if (isSliding)
        {
            ApplyGravity();
            return;
        }
        
        //Retrieve input values for horizontal and vertical axes
        float horizontal = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, verticalInput);
        
        //Calculate movement directions relative to the camera
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();
        
        //Compute final dir
        Vector3 moveDir = (camForward * verticalInput + camRight * horizontal);
        float inputMagnitude = moveDir.magnitude;
        
        //Rotate player to face the movement direction, avoids unecessary animations
        if (inputMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.15f);
        }
        else
        {
            //default to aligning with the camera forward direction
            Vector3 camFwd = cameraTransform.forward;
            camFwd.y = 0;
            if (camFwd.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camFwd);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
            }
        }
        
        //Check if the sprint key is held and there is available stamina
        if (Input.GetKey(KeyCode.LeftShift) && currentStamina > 0f)
        {
            moveDir *= sprintMultiplier;                 //Increase movement speed for sprinting
            animator.SetBool("IsSprinting", true);         //Update animator state
            currentStamina = Mathf.Max(0f, currentStamina - sprintStaminaCost * Time.deltaTime);
            staminaUseTimer = 0f;
        }
        else
        {
            animator.SetBool("IsSprinting", false);
        }
        
        //is the player is walking? (moving on the ground without jumping)
        bool isWalking = inputDir.magnitude > 0.001f && velocity.y < 0;
        animator.SetBool("IsWalking", isWalking);
        
        //Set horizontal movement velocity
        velocity.x = moveDir.x * walkSpeed;
        velocity.z = moveDir.z * walkSpeed;
        
        //Move the character controller based on the calculated velocity
        controller.Move(velocity * Time.deltaTime);
        
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            if (currentStamina >= jumpStaminaCost)
            {
                currentStamina -= jumpStaminaCost;
                staminaUseTimer = 0f;
                //Calculate the upward velocity
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                //Check if the player is sprinting to determine which jump animation to play
                if (animator.GetBool("IsSprinting"))
                {
                    animator.SetBool("SprintJump", true);
                    if (animator.GetBool("IsDribbling"))
                        ballDribbler.isHeld = true;  //Maintain ball control during sprint jump
                }
                else
                {
                    animator.SetBool("IsJumping", true);
                    if (animator.GetBool("IsDribbling"))
                        ballDribbler.isHeld = true;  //Maintain ball control during jump
                }
            }
        }
        
        //Force gravity
        ApplyGravity();
        
        //Regenerate stamina when the player is not sprinting or jumping
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetButtonDown("Jump"))
        {
            staminaUseTimer += Time.deltaTime;
            if (staminaUseTimer >= staminaRegenDelay)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            }
        }
        
        //Update the stamina bar UI
        if (staminaBar != null)
        {
            staminaBar.UpdateBar(currentStamina / maxStamina);
        }
    }
    
    //Applies gravity to the player's velocity and moves the controller
    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    //Plays a randomized step sound effect
    void step()
    {
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(stepSound);
    }
    
    //Coroutine handling the sliding movement
    IEnumerator Slide()
    {
        isSliding = true;
        //Ensure ball dribbling state persists during a slide
        if (animator.GetBool("IsDribbling"))
            ballDribbler.isHeld = true;
        animator.SetBool("IsSliding", true);
        
        float timer = 0f;
        Vector3 slideDir = transform.forward;              //Slide in the direction the player is facing.
        float slideSpeed = walkSpeed * slideSpeedMultiplier; //slide speed
        
        //Continue sliding for the defined duration
        while (timer < slideDuration)
        {
            controller.Move(slideDir * slideSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
        
        //End sliding state and reset animator parameter
        animator.SetBool("IsSliding", false);
        isSliding = false;
    }
    
    //Detect collisions with environment or traps
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //Prevent repeated damage in a short interval
        if (Time.time - lastDamageTime < damageCooldown)
            return;
        
        //Check collision with specific trap types
        if (hit.gameObject.CompareTag("SpinningLogTrap"))
        {
            lastDamageTime = Time.time;
            if (!isKnockbackActive)
            {
                isKnockbackActive = true;
                TakeDamage(25, hit); //Apply damage specific to Spinning Log Trap
            }
        }
        else if (hit.gameObject.CompareTag("SwingingAxe"))
        {
            lastDamageTime = Time.time;
            if (!isKnockbackActive)
            {
                isKnockbackActive = true;
                TakeDamage(20, hit); //Apply damage specific to Swinging Axe
            }
        }
    }
    
    //Applies damage and initiates knockback
    public void TakeDamage(float damage, ControllerColliderHit hit)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
            currentHealth = 0;
        //Update UI
        if (healthBar != null)
            healthBar.UpdateBar(currentHealth / maxHealth);
        
        animator.Play("Knockback", 0, 0f);
        
        Vector3 knockbackDir;
        //Apply knockback only if not ignoring it (used for certain specific traps where knockback has to be overridden)
        if (!ignoreKnockback)
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
            
            //Knockback force
            velocity.x = knockbackDir.x * knockbackForce;
            velocity.z = knockbackDir.z * knockbackForce;
        }
        else
        {
            // If ignoring knockback, zero out horizontal velocity
            velocity.x = 0;
            velocity.z = 0;
        }
        
        StartCoroutine(KnockbackRecovery());
    }

    //Forces an override of player's current velocity for traps and knockback effects (it is just coincidental that every time this is required it is along the Z-axis, so I just hardcoded it here to avoid issues)
    public void OverrideVelocity(Vector3 newVelocity)
    {
        velocity.x = 0;
        velocity.y = 0;
        velocity = newVelocity;
        ApplyGravity();
    }
    
    //Coroutine for getting up after being knocked down and stabilizing forces/immunity period
    IEnumerator KnockbackRecovery()
    {
        float elapsed = 0f;
        //Continue recovery until the knockback duration has passed and the animation is no longer playing
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