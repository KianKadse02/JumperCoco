using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(WallDetection))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody body;
    private Collider bodyCollider;
    private WallDetection wallDetection;

    public float moveMentSpeed;
    public float jumpFactor;
    protected Vector3 currentInput;

    public float fallMultiplier = 2.5f;

    [Header("Look Settings")]
    public Camera playerCamera;
    public float lookSensitivity = 100f;
    private float xRotation = 0f;

    [Header("Jump Settings")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;
    public float groundCheckRadius = 0.12f;
    private bool isGrounded;
    private int jumpsRemaining;
    private int maxJumps = 2;

    [Tooltip("How much horizontal speed affects jump force (0 = no effect, 0.1 = 10% boost per unit of speed)")]
    public float momentumJumpBonus = 0.1f;

    [Header("Wall Run Settings")]
    public float wallRunSpeed = 8f;
    [Tooltip("Upward force to fight gravity while wall running")]
    public float wallRunGravityCounter = 5f;
    [Tooltip("How long can you wall run before falling (seconds)")]
    public float maxWallRunDuration = 2f;
    [Tooltip("Force applied when jumping off a wall")]
    public float wallJumpForce = 15f;
    [Tooltip("How much force pushes you away from wall when jumping")]
    public float wallJumpAwayForce = 8f;

    private bool isWallRunning = false;
    private float wallRunTimer = 0f;
    private Vector3 wallRunDirection; // Direction we're running along the wall

    [Header("Wall Climb Settings")]
    public float wallClimbSpeed = 5f;
    public float maxWallClimbDuration = 1.5f;
    private bool isWallClimbing = false;
    private float wallClimbTimer = 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        wallDetection = GetComponent<WallDetection>();
        jumpsRemaining = maxJumps;
    }

    private void FixedUpdate()
    {
        CheckGroundStatus();

        // Handle wall run physics
        if (isWallRunning)
        {
            UpdateWallRun();
        }
        // Handle wall climb physics
        else if (isWallClimbing)
        {
            UpdateWallClimb();
        }
        // Normal movement
        else
        {
            Vector3 moveDirection = transform.right * currentInput.x + transform.forward * currentInput.z;
            Vector3 horizontalVelocity = moveDirection * moveMentSpeed;
            body.linearVelocity = new Vector3(horizontalVelocity.x, body.linearVelocity.y, horizontalVelocity.z);
        }

        // Enhanced falling (only when not wall interacting)
        if (body.linearVelocity.y < 0 && !isWallRunning && !isWallClimbing)
        {
            body.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void UpdateWallRun()
    {
        wallRunTimer += Time.fixedDeltaTime;

        // Check if wall run should end
        if (wallRunTimer >= maxWallRunDuration || !wallDetection.IsNearRunnableWall())
        {
            StopWallRun();
            return;
        }

        // Calculate wall run direction (perpendicular to wall normal)
        Vector3 wallNormal = wallDetection.GetWallNormal();
        // Run along the wall, perpendicular to its surface
        wallRunDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;

        // Determine which direction to run based on player's current velocity
        if (Vector3.Dot(body.linearVelocity, wallRunDirection) < 0)
        {
            wallRunDirection = -wallRunDirection;
        }

        // Apply wall run movement
        Vector3 wallRunVelocity = wallRunDirection * wallRunSpeed;

        // Counter gravity with upward force (gradually weakens over time)
        float gravityCounterStrength = Mathf.Lerp(wallRunGravityCounter, 0, wallRunTimer / maxWallRunDuration);
        float upwardForce = gravityCounterStrength;

        body.linearVelocity = new Vector3(wallRunVelocity.x, upwardForce, wallRunVelocity.z);

        Debug.DrawRay(transform.position, wallRunDirection * 2f, Color.cyan);
    }

    private void UpdateWallClimb()
    {
        wallClimbTimer += Time.fixedDeltaTime;

        // Check if climb should end
        if (wallClimbTimer >= maxWallClimbDuration || !wallDetection.IsNearClimbableWall())
        {
            StopWallClimb();
            return;
        }

        // Climb upward with decreasing strength over time
        float climbStrength = Mathf.Lerp(wallClimbSpeed, 0, wallClimbTimer / maxWallClimbDuration);

        // Kill horizontal velocity, only move up
        body.linearVelocity = new Vector3(0, climbStrength, 0);
    }

    private void CheckGroundStatus()
    {
        if (bodyCollider == null)
        {
            isGrounded = false;
            return;
        }

        Vector3 spherePosition = body.position + Vector3.down * (bodyCollider.bounds.extents.y - 0.01f);
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
            StopWallRun();
            StopWallClimb();
        }

        Debug.DrawRay(spherePosition, Vector3.up * 0.05f, isGrounded ? Color.green : Color.red);
        Debug.DrawRay(spherePosition + Vector3.left * 0.03f, Vector3.right * 0.06f, isGrounded ? Color.green : Color.red);
    }

    private void OnMove(InputValue value)
    {
        currentInput = new Vector3(value.Get<Vector2>().x, 0, value.Get<Vector2>().y);
        print(currentInput.ToString());
    }

    private void OnLook(InputValue value)
    {
        Vector2 lookInput = value.Get<Vector2>();
        float mouseX = lookInput.x * lookSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * lookSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void OnJump()
    {
        // Wall jump (if already wall running/climbing)
        if (isWallRunning)
        {
            PerformWallJump();
            return;
        }

        if (isWallClimbing)
        {
            PerformWallJump();
            return;
        }

        // Start wall interaction (when airborne and near wall)
        if (!isGrounded)
        {
            if (wallDetection.IsNearRunnableWall())
            {
                StartWallRun();
                return;
            }

            if (wallDetection.IsNearClimbableWall())
            {
                StartWallClimb();
                return;
            }
        }
        /*
        // Normal jump logic
        if (jumpsRemaining <= 0) return;

        Vector3 horizontalVelocity = new Vector3(body.linearVelocity.x, 0, body.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        float momentumBonus = 1f + (currentSpeed * momentumJumpBonus);

        float finalJumpForce = jumpFactor * momentumBonus;
        body.AddForce(Vector3.up * finalJumpForce, ForceMode.Impulse);

        jumpsRemaining--;

        Debug.Log($"Jump! Remaining: {jumpsRemaining}, Momentum Bonus: {momentumBonus:F2}x");
        */
    }

    private void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = 0f;

        // Reset vertical velocity when starting wall run
        body.linearVelocity = new Vector3(body.linearVelocity.x, 0, body.linearVelocity.z);

        Debug.Log("Started WALL RUN!");
    }

    private void StopWallRun()
    {
        if (!isWallRunning) return;

        isWallRunning = false;
        wallRunTimer = 0f;
        Debug.Log("Stopped wall run");
    }

    private void StartWallClimb()
    {
        isWallClimbing = true;
        wallClimbTimer = 0f;

        // Reset velocity when starting climb
        body.linearVelocity = Vector3.zero;

        Debug.Log("Started WALL CLIMB!");
    }

    private void StopWallClimb()
    {
        if (!isWallClimbing) return;

        isWallClimbing = false;
        wallClimbTimer = 0f;
        Debug.Log("Stopped wall climb");
    }

    private void PerformWallJump()
    {
        Vector3 wallNormal = wallDetection.GetWallNormal();

        // Jump up and away from wall
        Vector3 jumpDirection = (Vector3.up + wallNormal).normalized;

        // Stop current wall interaction
        StopWallRun();
        StopWallClimb();

        // Apply jump force
        body.linearVelocity = Vector3.zero; // Reset velocity first
        body.AddForce(jumpDirection * wallJumpForce, ForceMode.Impulse);
        body.AddForce(wallNormal * wallJumpAwayForce, ForceMode.Impulse);

        Debug.Log("WALL JUMP!");
    }

    void Start() { }
    void Update() { }
}