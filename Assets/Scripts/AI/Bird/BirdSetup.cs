using UnityEngine;

public class BirdSetup : EnemyAIBase
{
    private enum State { None, Idle, Patrol, Chase, Dive, Stunned }
    [Header("Bird Settings")] public float PatrolRadius = 3f; public float AttackPrepTime = 0.5f; public float LungeDuration = 0.4f;
    [Header("Lunge Tuning")] public float LungeTargetYOffset = -0.5f; public float LungeAccel = 1.2f;
    [Header("Ranges")] public float DetectRange = 10f; public float ChaseRange = 15f; public float AttackRange = 10f;
    [Header("Patrol Timing")] public float PatrolMoveTimeMin = 1f; public float PatrolMoveTimeMax = 2.5f;
    [Header("Hover Tuning")] public float HoverAmplitude = 0.6f; public float HoverFrequency = 1.2f; public float HoverDriftSpeed = 0.6f; public float HoverReturnSpeed = 4f; public float HoverMaxVy = 3f; public float HoverHeightAboveTarget = 3f; public float AttackStandoffDistance = 5f; public float MaxDiveSpeed = 9f;

    private State _state;
    private Vector2 _startPosition;
    private float _stateTimer, _idleDuration, _patrolDuration, _stunDuration, _hoverY, _diveTime, _diveVx, _defaultGravity;
    private int _direction;
    private bool _diving, _damaged;

    protected override void Awake() { base.Awake(); _startPosition = transform.position; _defaultGravity = Fsm.Rb != null ? Fsm.Rb.gravityScale : 0f; }
    private void Start() => ChangeState(State.Idle);
    private void Update()
    {
        switch (_state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Dive: UpdateDive(); break;
            case State.Stunned: UpdateStunned(); break;
        }
    }
    public override bool TryStun(float duration) { _stunDuration = Mathf.Max(_stunDuration, duration); _stateTimer = 0f; ChangeState(State.Stunned); return true; }
    private void ChangeState(State next)
    {
        if (_state == next) return;
        if (_state == State.Dive && Fsm.Rb != null)
        {
            Fsm.Rb.gravityScale = _defaultGravity;
            Fsm.Rb.linearVelocity = Vector2.zero;
        }
        _state = next; _stateTimer = 0f;
        switch (next)
        {
            case State.Idle: Fsm.StopMovement(); if (Fsm.Sr != null) Fsm.Sr.color = Color.white; _idleDuration = Random.Range(1f, 4f); break;
            case State.Patrol: if (Fsm.Sr != null) Fsm.Sr.color = Color.yellow; _patrolDuration = Random.Range(PatrolMoveTimeMin, PatrolMoveTimeMax); _direction = Random.value < .5f ? -1 : 1; _hoverY = _startPosition.y; break;
            case State.Chase: if (Fsm.Sr != null) Fsm.Sr.color = Color.red; break;
            case State.Dive: if (Fsm.Sr != null) Fsm.Sr.color = Color.magenta; Fsm.StopMovement(); _diving = false; _damaged = false; break;
            case State.Stunned: Fsm.StopMovement(); if (Fsm.Sr != null) Fsm.Sr.color = Color.blue; break;
        }
    }
    private void UpdateIdle() { _stateTimer += Time.deltaTime; if (_stateTimer >= _idleDuration) ChangeState(State.Patrol); }
    private void UpdatePatrol()
    {
        if (Fsm.Player != null && Vector2.Distance(transform.position, Fsm.Player.position) <= DetectRange) { ChangeState(State.Chase); return; }
        float offset = transform.position.x - _startPosition.x; if (Mathf.Abs(offset) >= PatrolRadius) _direction = (int)-Mathf.Sign(offset);
        Move(_direction, Fsm.Data != null ? Fsm.Data.PatrolSpeed : HoverDriftSpeed, _hoverY);
        _stateTimer += Time.deltaTime; if (_stateTimer >= _patrolDuration) ChangeState(State.Idle);
    }
    private void UpdateChase()
    {
        if (Fsm.Player == null) { ChangeState(State.Idle); return; }
        float distance = Vector2.Distance(transform.position, Fsm.Player.position); if (distance > ChaseRange) { ChangeState(State.Patrol); return; }
        float dx = Fsm.Player.position.x - transform.position.x;
        float maxDiveDistance = MaxDiveSpeed > 0f ? MaxDiveSpeed * LungeDuration : AttackRange;
        if (Vector2.Distance(transform.position, Fsm.Player.position) <= Mathf.Min(AttackRange, maxDiveDistance)) { ChangeState(State.Dive); return; }
        _hoverY = Fsm.Player.position.y + HoverHeightAboveTarget;
        Move(Mathf.Abs(dx) > AttackStandoffDistance ? (dx > 0 ? 1 : -1) : 0, Fsm.Data != null ? Fsm.Data.MoveSpeed : 2f, _hoverY);
        if (Fsm.Sr != null) Fsm.Sr.flipX = dx >= 0f;
    }
    private void UpdateDive()
    {
        if (Fsm.Player == null) { ChangeState(State.Idle); return; }
        if (!_diving) { _stateTimer += Time.deltaTime; _hoverY = Fsm.Player.position.y + HoverHeightAboveTarget; Move(0, 0, _hoverY); if (_stateTimer >= AttackPrepTime) StartDive(); return; }
        _stateTimer += Time.deltaTime;
        if (Fsm.Rb != null) Fsm.Rb.linearVelocity = ClampSpeed(new Vector2(_diveVx * Mathf.Lerp(1f, 1f + LungeAccel, _stateTimer / _diveTime), Fsm.Rb.linearVelocity.y));
        if (!_damaged && Vector2.Distance(transform.position, Fsm.Player.position) <= AttackRange) { Fsm.PerformAttack(AttackRange); _damaged = true; }
        if (_stateTimer >= _diveTime) ChangeState(State.Chase);
    }
    private void UpdateStunned() { _stateTimer += Time.deltaTime; if (_stateTimer >= _stunDuration) { _stunDuration = 0f; ChangeState(State.Chase); } }
    private void StartDive()
    {
        _diving = true; _stateTimer = 0f; _diveTime = Mathf.Max(.01f, LungeDuration);
        Vector2 start = transform.position;
        Vector2 target = new Vector2(Fsm.Player.position.x, Fsm.Player.position.y + LungeTargetYOffset);
        if (Fsm.Rb == null) return;
        Fsm.Rb.gravityScale = 0f;
        _diveVx = (target.x - start.x) / _diveTime;
        float vy = (target.y - start.y) / _diveTime;
        Fsm.Rb.linearVelocity = ClampSpeed(new Vector2(_diveVx, vy));
    }
    private void Move(int direction, float speed, float targetY) { if (Fsm.Rb == null) return; float vy = Mathf.Clamp((targetY + Mathf.Sin(Time.time * HoverFrequency) * HoverAmplitude - transform.position.y) * HoverReturnSpeed, -HoverMaxVy, HoverMaxVy); Fsm.Rb.linearVelocity = new Vector2(direction * speed, vy); if (Fsm.Sr != null && direction != 0) Fsm.Sr.flipX = direction > 0; }
    private Vector2 ClampSpeed(Vector2 velocity) => MaxDiveSpeed > 0f && velocity.magnitude > MaxDiveSpeed ? velocity.normalized * MaxDiveSpeed : velocity;
}
