using UnityEngine;

public class WallDetection : MonoBehaviour
{
    [Header("Wall Detection Settings")]
    public LayerMask runnableWallLayer;
    public LayerMask climbableWallLayer;

    [Tooltip("Capsule radius (0.5) + extra detection range (0.2)")]
    public float detectionDistance = 0.7f; // 0.5 radius + 0.2 buffer

    private Rigidbody body;
    private bool isAirborne;

    // Detection results
    private bool isNearRunnableWall;
    private bool isNearClimbableWall;
    private Vector3 wallNormal; // Direction the wall is facing (useful later for wallrun direction)

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Only check when airborne
        CheckIfAirborne();

        if (isAirborne)
        {
            CheckForWalls();
        }
    }

    private void CheckIfAirborne()
    {
        // Simple airborne check - you might want to reference PlayerMovement's isGrounded later
        isAirborne = body.linearVelocity.y != 0;
    }

    private void CheckForWalls()
    {
        // Reset detection
        isNearRunnableWall = false;
        isNearClimbableWall = false;

        // Check 4 directions: forward, back, left, right
        Vector3[] directions = new Vector3[]
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };

        foreach (Vector3 direction in directions)
        {
            // Check for Runnable Wall
            if (Physics.Raycast(transform.position, direction, out RaycastHit runnableHit, detectionDistance, runnableWallLayer))
            {
                isNearRunnableWall = true;
                wallNormal = runnableHit.normal;
                Debug.Log($"Detected RUNNABLE WALL in direction: {direction}, Distance: {runnableHit.distance:F2}");
                Debug.DrawRay(transform.position, direction * detectionDistance, Color.blue);
            }

            // Check for Climbable Wall
            if (Physics.Raycast(transform.position, direction, out RaycastHit climbableHit, detectionDistance, climbableWallLayer))
            {
                isNearClimbableWall = true;
                wallNormal = climbableHit.normal;
                Debug.Log($"Detected CLIMBABLE WALL in direction: {direction}, Distance: {climbableHit.distance:F2}");
                Debug.DrawRay(transform.position, direction * detectionDistance, Color.green);
            }
        }

        // Draw rays even when not hitting (for debugging)
        if (!isNearRunnableWall && !isNearClimbableWall)
        {
            foreach (Vector3 direction in directions)
            {
                Debug.DrawRay(transform.position, direction * detectionDistance, Color.red);
            }
        }
    }

    // Public getters for PlayerMovement to use later
    public bool IsNearRunnableWall() => isNearRunnableWall;
    public bool IsNearClimbableWall() => isNearClimbableWall;
    public Vector3 GetWallNormal() => wallNormal;
}
