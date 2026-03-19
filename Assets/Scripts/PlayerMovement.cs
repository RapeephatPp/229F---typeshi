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

    [Header("Game Feel")]
    [SerializeField] Camera playerCamera;
    [SerializeField] float normalFOV = 60f;
    [SerializeField] float runFOV = 80f;
    [SerializeField] float dashFOV = 95f; 
    [SerializeField] float fovTransitionSpeed = 10f;
    [SerializeField] float landShakeDuration = 0.15f;
    [SerializeField] float landShakeMagnitude = 0.2f;

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
    Vector3 cameraBaseLocalPos;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        currentSpeed = walkSpeed;
        cameraBaseLocalPos = playerCamera.transform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if(playerCamera != null) playerCamera.fieldOfView = normalFOV;
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
    }

    void HandleGroundCheck()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            jumpsRemaining = maxJumps; 
        }

        if (isGrounded && !wasGrounded && velocity.y < -10f)
        {
            StartCoroutine(CameraShake(landShakeDuration, landShakeMagnitude));
        }
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
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
                    
                    // Only apply crouch slowness if NOT sliding
                    if (Input.GetKey(KeyCode.LeftControl)) targetSpeed = crouchSpeed;
                    
                    moveDirection = inputDir;
                }

                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedTransitionTime * Time.deltaTime);
                if (currentSpeed < 0.1f) currentSpeed = 0f;
            }
            else
            {
                // Air Movement: Retain speed, do not slow down when crouching
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
        // 1. Handle Height Scaling (Works both Mid-air and Grounded)
        float targetHeight = Input.GetKey(KeyCode.LeftControl) ? originalHeight * crouchScaleY : originalHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // 2. Continuous Slide Check (Triggers exactly when landing if holding Ctrl)
        if (Input.GetKey(KeyCode.LeftControl) && isGrounded && !isSliding)
        {
            // If moving fast enough (lower threshold so it's more forgiving)
            if (currentSpeed > walkSpeed + 0.5f) 
            {
                isSliding = true;
                
                // Boost to slide speed, but keep momentum if already going faster (e.g. from dash)
                currentSpeed = Mathf.Max(currentSpeed, slideSpeed); 

                float x = Input.GetAxisRaw("Horizontal");
                float z = Input.GetAxisRaw("Vertical");
                Vector3 inputDir = (transform.right * x + transform.forward * z).normalized;
                
                slideDirection = inputDir.magnitude > 0.1f ? inputDir : transform.forward; 
            }
        }

        // Cancel slide when key released
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
    }

    void HandleGameFeel()
    {
        if (playerCamera == null) return;

        float targetFOV = normalFOV;
        
        if (isDashing) targetFOV = dashFOV;
        else if (isSliding) targetFOV = runFOV + 5f;
        else if (currentSpeed > walkSpeed + 1f && Input.GetAxis("Vertical") > 0) targetFOV = runFOV;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }

    IEnumerator CameraShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            playerCamera.transform.localPosition = cameraBaseLocalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        playerCamera.transform.localPosition = cameraBaseLocalPos;
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
}