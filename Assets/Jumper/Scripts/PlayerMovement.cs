using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction landAction;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 10f;
    private Vector2 moveInput;
    private bool isSprinting;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float lookSensitivity = 1f;
    private Vector2 lookInput;
    private float cameraPitch = 0f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float gravity = -20f;
    public float fallMultiplier = 2.5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;
    public float groundCheckRadius = 0.4f;
    private bool isGrounded;
    private bool wasGrounded; // To detect when we just landed

    [Header("Dash Settings")]
    public float dashForce = 15f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 1f;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    [Header("Landing Settings")]
    public float landingTimeWindow = 0.5f; // Time before landing to press E
    public float landingDetectionHeight = 2f; // How far to raycast down
    public float stickLandingStopForce = 0.9f; // How much to reduce momentum (0-1)
    private bool isPreparingLanding; // Player pressed E in time window
    private bool canPrepareLanding; // Are we close enough to ground?

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerInput = GetComponent<PlayerInput>();

        // Get input actions
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Dash"];
        landAction = playerInput.actions["Land"];

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJump;
        dashAction.performed += OnDash;
        landAction.performed += OnLand;
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJump;
        dashAction.performed -= OnDash;
        landAction.performed -= OnLand;
    }

    private void Update()
    {
        // Read inputs
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>();
        isSprinting = playerInput.actions["Sprint"].IsPressed();

        // Handle camera look
        HandleCameraLook();

        // Update timers
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
                isDashing = false;
        }

        // Check if we can prepare landing (are we close to ground while falling?)
        CheckLandingWindow();

        // Store previous grounded state
        wasGrounded = isGrounded;
    }

    private void FixedUpdate()
    {
        CheckGroundStatus();

        // Reset landing prep if we landed
        if (isGrounded && !wasGrounded)
        {
            HandleLanding();
        }

        if (isDashing)
        {
            // During dash, physics is handled by dash force
            return;
        }

        HandleMovement();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        if (isDashing) return;

        // Calculate movement direction relative to camera
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Flatten camera directions (no vertical component)
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate desired movement direction
        Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            // Rotate player to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

            // Apply movement
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
            Vector3 targetVelocity = moveDirection * currentSpeed;

            // Keep vertical velocity, only change horizontal
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
        else
        {
            // Stop horizontal movement when no input
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    private void HandleCameraLook()
    {
        if (cameraTransform == null) return;

        // Horizontal rotation (rotate player body)
        float yaw = lookInput.x * lookSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * yaw);

        // Vertical rotation (rotate camera)
        cameraPitch -= lookInput.y * lookSensitivity * Time.deltaTime;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            // Enhanced falling
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector3.up * gravity * fallMultiplier * Time.fixedDeltaTime;
            }
            else
            {
                rb.linearVelocity += Vector3.up * gravity * Time.fixedDeltaTime;
            }
        }
    }

    private void CheckGroundStatus()
    {
        Vector3 spherePosition = transform.position + Vector3.down * (capsuleCollider.height / 2 - capsuleCollider.radius + groundCheckDistance);
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        // Debug visualization
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    private void CheckLandingWindow()
    {
        // Only check if we're falling and not grounded
        if (isGrounded || rb.linearVelocity.y >= 0)
        {
            canPrepareLanding = false;
            return;
        }

        // Raycast down to see if we're close to ground
        Vector3 rayStart = transform.position;
        float checkDistance = landingDetectionHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, checkDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            // Calculate time to impact based on current fall speed
            float timeToImpact = hit.distance / Mathf.Abs(rb.linearVelocity.y);

            canPrepareLanding = timeToImpact <= landingTimeWindow;

            // Debug visualization
            Debug.DrawRay(rayStart, Vector3.down * hit.distance, canPrepareLanding ? Color.yellow : Color.blue);
        }
        else
        {
            canPrepareLanding = false;
        }
    }

    private void HandleLanding()
    {
        if (isPreparingLanding)
        {
            // Successful stick landing!
            Vector3 velocity = rb.linearVelocity;
            velocity.x *= stickLandingStopForce;
            velocity.z *= stickLandingStopForce;
            rb.linearVelocity = velocity;

            Debug.Log("STICK LANDING! Momentum reduced.");
        }

        // Reset landing prep
        isPreparingLanding = false;
        canPrepareLanding = false;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded && !isDashing)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Reset vertical velocity
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log("JUMP!");
        }
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        // Can only dash if grounded and cooldown is ready
        if (!isGrounded || isDashing || dashCooldownTimer > 0)
            return;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // Dash in the direction player is facing
        Vector3 dashDirection = transform.forward;

        // Apply dash force
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Reset horizontal velocity
        rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);

        Debug.Log("DASH!");
    }

    private void OnLand(InputAction.CallbackContext context)
    {
        // Can only prepare landing if in the time window
        if (canPrepareLanding && !isGrounded)
        {
            isPreparingLanding = true;
            Debug.Log("Landing prepared! Hit ground now for stick landing.");
        }
        else if (isGrounded)
        {
            Debug.Log("Already grounded, can't prepare landing.");
        }
        else
        {
            Debug.Log("Too early! Get closer to ground.");
        }
    }

    // Visualize ground check in editor
    private void OnDrawGizmosSelected()
    {
        if (capsuleCollider == null) return;

        // Ground check sphere
        Gizmos.color = Color.red;
        Vector3 spherePosition = transform.position + Vector3.down * (capsuleCollider.height / 2 - capsuleCollider.radius + groundCheckDistance);
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);

        // Landing detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * landingDetectionHeight);
    }
}
