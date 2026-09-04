using UnityEngine;

/// <summary>
/// Moves a rope endpoint around its starting point until it is selected by RopeManager.
/// Once locked, the endpoint becomes static so RopeBridge can use it as a normal anchor.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FloatingRopeAnchor : MonoBehaviour
{
    [Header("Floating area")]
    [SerializeField, Min(0.05f)] private float movementRadius = 1.5f;
    [SerializeField, Min(0.05f)] private float maximumTargetStepDistance = 0.75f;

    [Header("Dust motion")]
    [SerializeField, Min(0.01f)] private float maximumSpeed = 0.28f;
    [SerializeField, Min(0.01f)] private float movementSmoothTime = 1.2f;
    [SerializeField, Min(0.001f)] private float targetArrivalDistance = 0.03f;
    [SerializeField, Min(0f)] private float targetPause = 0.1f;

    [Header("Activation")]
    [SerializeField] private bool lockOnRopeSelection = true;

    private Rigidbody2D body;
    private Vector2 origin;
    private Vector2 targetPosition;
    private Vector2 movementVelocity;
    private float pauseRemaining;

    public bool IsLocked { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        origin = body != null ? body.position : (Vector2)transform.position;
    }

    private void Start()
    {
        if (IsLocked || body == null) return;

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        PickNextTarget(body.position);
    }

    private void FixedUpdate()
    {
        if (IsLocked) return;

        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        if (pauseRemaining > 0f)
        {
            pauseRemaining -= Time.fixedDeltaTime;
            if (pauseRemaining <= 0f) PickNextTarget(currentPosition);
            return;
        }

        Vector2 nextPosition = Vector2.SmoothDamp(
            currentPosition,
            targetPosition,
            ref movementVelocity,
            movementSmoothTime,
            maximumSpeed,
            Time.fixedDeltaTime);

        if (body != null)
        {
            body.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }

        if (Vector2.Distance(nextPosition, targetPosition) <= targetArrivalDistance)
        {
            MoveTo(targetPosition);
            movementVelocity = Vector2.zero;
            pauseRemaining = targetPause;
        }
    }

    /// <summary>
    /// Stops this endpoint at its current location. Calling this again is safe.
    /// </summary>
    public void LockInPlace()
    {
        if (IsLocked || !lockOnRopeSelection) return;

        IsLocked = true;
        if (body == null) return;

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Static;
    }

    private void PickNextTarget(Vector2 currentPosition)
    {
        Vector2 currentOffset = Vector2.ClampMagnitude(currentPosition - origin, movementRadius);
        Vector2 nextOffset = currentOffset;

        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector2 randomStep = Random.insideUnitCircle * maximumTargetStepDistance;
            if (randomStep.sqrMagnitude < 0.01f) continue;

            Vector2 candidate = currentOffset + randomStep;
            if (candidate.sqrMagnitude <= movementRadius * movementRadius)
            {
                nextOffset = candidate;
                break;
            }
        }

        // If all nearby random directions point outside the edge, move gently back
        // into the circle instead of sticking to its boundary.
        if ((nextOffset - currentOffset).sqrMagnitude < 0.01f)
        {
            nextOffset = Random.insideUnitCircle * movementRadius;
        }

        targetPosition = origin + nextOffset;
    }

    private void MoveTo(Vector2 position)
    {
        if (body != null) body.MovePosition(position);
        else transform.position = position;
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && IsLocked) return;

        Vector3 center = Application.isPlaying ? (Vector3)origin : transform.position;
        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.8f);
        Gizmos.DrawWireSphere(center, movementRadius);
    }
}
