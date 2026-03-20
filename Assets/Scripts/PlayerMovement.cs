using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Core Movement")]
    [SerializeField] float walkSpeed = 7f;
    [SerializeField] float runSpeed = 15f;
    [SerializeField] float crouchSpeed = 4f;
    [SerializeField] float speedTransitionTime = 5f;
    [SerializeField] float gravity = -30f;
    [SerializeField] float jumpHeight = 2.5f;
    [SerializeField] int maxJumps = 2; 

    [Header("Slide Settings")]
    [SerializeField] float slideSpeed = 18f;
    [SerializeField] float slideDrag = 5f; 
    [SerializeField] float crouchScaleY = 0.5f; 
    [SerializeField] float crouchTransitionSpeed = 10f; 
    Vector3 slideDirection; 

    [Header("Dash Settings")]
    [SerializeField] KeyCode dashKey = KeyCode.Q;
    [SerializeField] float dashSpeed = 25f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashCooldown = 1f;

    [Header("Ledge Climbing & Vaulting")]
    [SerializeField] float ledgeClimbSpeed = 10f;
    [SerializeField] float forwardRayLength = 1f;
    [SerializeField] float downwardRayHeight = 1.5f;
    [SerializeField] float ledgeVaultBoost = 1.5f;
    
    [Header("Game Feel - Particle Speed Lines")]
    [SerializeField] ParticleSystem speedLinesParticle;
    [SerializeField] float minSpeedToShowLines = 14f; 
    [SerializeField] float maxParticleEmission = 100f; 
    [SerializeField] float fadeSpeed = 5f;
    
    [Header("Game Feel - FOV & Shake")]
    [SerializeField] Camera playerCamera;
    [SerializeField] float normalFOV = 60f;
    [SerializeField] float runFOV = 80f;
    [SerializeField] float dashFOV = 95f; 
    [SerializeField] float fovTransitionSpeed = 10f;
    [SerializeField] float landShakeDuration = 0.15f;
    [SerializeField] float landShakeMagnitude = 0.2f;

    [Header("Game Feel - Camera Tilt")]
    [SerializeField] float maxTiltAngle = 4f;
    [SerializeField] float tiltTransitionSpeed = 8f;

    [Header("Game Feel - Head Bob & Impact")]
    [SerializeField] float idleBobSpeed = 2f;     
    [SerializeField] float idleBobAmount = 0.02f; 
    [SerializeField] float walkBobSpeed = 12f;
    [SerializeField] float walkBobAmount = 0.05f;
    [SerializeField] float runBobSpeed = 18f;
    [SerializeField] float runBobAmount = 0.1f;
    [SerializeField] float jumpDipAmount = 0.15f;
    [SerializeField] float landDipAmount = 0.25f;

    [Header("Camera & Input")]
    [SerializeField] float mouseSensitivity = 100f;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.4f;
    [SerializeField] LayerMask groundMask;

    // Internal Variables
    CharacterController controller;
    Vector3 velocity;
    Vector3 moveDirection;
    bool isGrounded;
    bool wasGrounded;
    bool isClimbing;
    bool isSliding;
    bool isDashing;
    int jumpsRemaining;
    float currentSpeed;
    float xRotation = 0f;
    float originalHeight;
    float nextDashTime;
    
    // Camera Effect Variables
    Vector3 cameraBaseLocalPos;
    Vector3 currentShakeOffset;
    float headBobTimer;
    float currentTilt;
    float currentDipY;
    float currentParticleEmission = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        currentSpeed = walkSpeed;
        cameraBaseLocalPos = playerCamera.transform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if(playerCamera != null) playerCamera.fieldOfView = normalFOV;
        
        if (speedLinesParticle != null)
        {
            var emission = speedLinesParticle.emission;
            emission.rateOverTime = 0f;
            speedLinesParticle.Play();
        }
    }

    void Update()
    {
        if (isClimbing) return; 

        HandleGroundCheck();
        HandleLook();
        
        if (!isDashing) 
        {
            HandleMovement();
            HandleCrouchAndSlide();
            HandleJump();
        }

        HandleDash();
        HandleGameFeel();
        ApplyGravity();
        HandleSpeedLines();
        
    }

    void HandleGroundCheck()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Check impacts FIRST, before we reset the downward velocity!
        if (isGrounded && !wasGrounded)
        {
            if (velocity.y < -5f) 
            {
                currentDipY = -landDipAmount; // Trigger Land Dip
            }
            if (velocity.y < -7f)
            {
                StartCoroutine(CameraShakeRoutine(landShakeDuration, landShakeMagnitude));
            }
        }

        // safely reset the Y velocity to stick to the ground
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            jumpsRemaining = maxJumps; 
        }
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply X rotation (Mouse Y) and Z rotation (Tilt)
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = (transform.right * x + transform.forward * z).normalized;

        if (isSliding)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, crouchSpeed, slideDrag * Time.deltaTime);
            if (currentSpeed <= crouchSpeed + 0.5f) isSliding = false;
            
            moveDirection = slideDirection;
        }
        else
        {
            if (isGrounded)
            {
                float targetSpeed = 0f;

                if (inputDir.magnitude > 0.1f)
                {
                    targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
                    if (Input.GetKey(KeyCode.LeftControl)) targetSpeed = crouchSpeed;
                    
                    moveDirection = inputDir;
                }

                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedTransitionTime * Time.deltaTime);
                if (currentSpeed < 0.1f) currentSpeed = 0f;
            }
            else
            {
                if (inputDir.magnitude > 0.1f)
                {
                    moveDirection = Vector3.Lerp(moveDirection, inputDir, 5f * Time.deltaTime).normalized;
                }
            }
        }

        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    void HandleCrouchAndSlide()
    {
        float targetHeight = Input.GetKey(KeyCode.LeftControl) ? originalHeight * crouchScaleY : originalHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        if (Input.GetKey(KeyCode.LeftControl) && isGrounded && !isSliding)
        {
            if (currentSpeed > walkSpeed + 0.5f) 
            {
                isSliding = true;
                currentSpeed = Mathf.Max(currentSpeed, slideSpeed); 

                float x = Input.GetAxisRaw("Horizontal");
                float z = Input.GetAxisRaw("Vertical");
                Vector3 inputDir = (transform.right * x + transform.forward * z).normalized;
                
                slideDirection = inputDir.magnitude > 0.1f ? inputDir : transform.forward; 
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isSliding = false;
        }
    }

    void HandleDash()
    {
        if (Input.GetKeyDown(dashKey) && Time.time >= nextDashTime && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 dashDir = (transform.right * x + transform.forward * z).normalized;
        
        if (dashDir.magnitude < 0.1f) dashDir = transform.forward;

        velocity.y = 0f; 

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            controller.Move(dashDir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        currentSpeed = runSpeed;
        moveDirection = dashDir;
        isDashing = false;
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            if (TryClimbLedge()) return;

            if (jumpsRemaining > 0)
            {
                if (isSliding) currentSpeed += 3f; 
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpsRemaining--;
                isSliding = false; 
                
                currentDipY = -jumpDipAmount; // Trigger Jump Dip
            }
        }
        else if (Input.GetButton("Jump"))
        {
            TryClimbLedge();
        }
    }
    
    bool TryClimbLedge()
    {
        if (isGrounded) return false;

        Vector3 chestPos = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(chestPos, transform.forward, out RaycastHit wallHit, forwardRayLength, groundMask))
        {
            Vector3 downStart = wallHit.point + (transform.forward * 0.1f) + (Vector3.up * downwardRayHeight);
            
            if (Physics.Raycast(downStart, Vector3.down, out RaycastHit ledgeHit, downwardRayHeight, groundMask))
            {
                if (ledgeHit.point.y > transform.position.y)
                {
                    StartCoroutine(ClimbLedgeRoutine(ledgeHit.point));
                    return true; 
                }
            }
        }
        return false; 
    }

    IEnumerator ClimbLedgeRoutine(Vector3 targetPosition)
    {
        isClimbing = true;
        velocity = Vector3.zero; 

        Vector3 endPosition = targetPosition + Vector3.up * (controller.height / 2f);
        float climbProgress = 0f;
        Vector3 startPosition = transform.position;

        controller.enabled = false;

        while (climbProgress < 1f)
        {
            climbProgress += Time.deltaTime * ledgeClimbSpeed;
            transform.position = Vector3.Lerp(startPosition, endPosition, climbProgress);
            yield return null;
        }

        controller.enabled = true;
        isClimbing = false;

        velocity.y = Mathf.Sqrt(ledgeVaultBoost * -2f * gravity);
        currentSpeed = runSpeed; 
        moveDirection = transform.forward;
        currentDipY = -jumpDipAmount; // Dip effect after vaulting
    }

    void HandleGameFeel()
    {
        if (playerCamera == null) return;

        // 1. FOV Adjustment
        float targetFOV = normalFOV;
        if (isDashing) targetFOV = dashFOV;
        else if (isSliding) targetFOV = runFOV + 5f;
        else if (currentSpeed > walkSpeed + 1f && Input.GetAxis("Vertical") > 0) targetFOV = runFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);

        // 2. Camera Tilt (Strafe)
        float targetTilt = 0f;
        if (moveDirection.magnitude > 0.1f && !isClimbing)
        {
            float xInput = Input.GetAxisRaw("Horizontal");
            targetTilt = -xInput * maxTiltAngle;
        }
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltTransitionSpeed * Time.deltaTime);
        
        // 3. Head Bobbing & Breathing (Idle)
        float bobOffset = 0f;
        
        if (isGrounded && !isSliding && !isDashing)
        {
            if (currentSpeed > 0.5f)
            {
                // Moving Bob
                float speedMult = (Input.GetKey(KeyCode.LeftShift)) ? runBobSpeed : walkBobSpeed;
                float amountMult = (Input.GetKey(KeyCode.LeftShift)) ? runBobAmount : walkBobAmount;
                headBobTimer += Time.deltaTime * speedMult;
                bobOffset = Mathf.Sin(headBobTimer) * amountMult;
            }
            else
            {
                // Idle Bob (Breathing)
                headBobTimer += Time.deltaTime * idleBobSpeed;
                bobOffset = Mathf.Sin(headBobTimer) * idleBobAmount;
            }
        }
        else 
        { 
            // Reset mid-air or during slide
            headBobTimer = 0f; 
        }

        // 4. Impact Dip Recovery (Lerp back to 0)
        currentDipY = Mathf.Lerp(currentDipY, 0f, Time.deltaTime * 10f);

        Vector3 smoothTargetPos = cameraBaseLocalPos + new Vector3(0, bobOffset + currentDipY, 0);
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, smoothTargetPos, Time.deltaTime * 15f);
        playerCamera.transform.localPosition += currentShakeOffset;
    }

    IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            currentShakeOffset = new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentShakeOffset = Vector3.zero;
    }

    void ApplyGravity()
    {
        if (controller == null || !controller.enabled) return; 

        if (!isDashing && !isClimbing) 
        {
            velocity.y += gravity * Time.deltaTime;
        }
        controller.Move(velocity * Time.deltaTime);
    }
    
    void HandleSpeedLines()
    {
        if (speedLinesParticle == null) return;
        
        if (!speedLinesParticle.isPlaying)
        {
            speedLinesParticle.Play();
        }

        float targetEmission = 0f;
        
        // FIX: Removed 'isGrounded' and input checks. 
        // Now it only cares about your actual speed, so lines stay during jumps and air-strafing!
        if (currentSpeed >= minSpeedToShowLines)
        {
            float speedRange = Mathf.Max(0.1f, runSpeed - minSpeedToShowLines);
            float speedFactor = (currentSpeed - minSpeedToShowLines) / speedRange;
            
            targetEmission = Mathf.Lerp(0f, maxParticleEmission, speedFactor);
        }
        else if (isDashing) 
        {
            targetEmission = maxParticleEmission * 1.5f; 
        }
        
        currentParticleEmission = Mathf.Lerp(currentParticleEmission, targetEmission, fadeSpeed * Time.deltaTime);
        
        var emission = speedLinesParticle.emission;
        emission.rateOverTime = currentParticleEmission;
    }
    
    public float GetCurrentSpeed() 
    {
        return currentSpeed;
    }

    public string GetMovementState()
    {
        if (isDashing) return "DASHING";
        if (isClimbing) return "VAULTING";
        if (isSliding) return "SLIDING";
        if (!isGrounded) return "IN AIR";
        if (Input.GetKey(KeyCode.LeftControl)) return "CROUCHING";
        if (currentSpeed > walkSpeed + 0.5f) return "RUNNING";
        if (currentSpeed > 0.5f) return "WALKING";
        return "IDLE";
    }
}