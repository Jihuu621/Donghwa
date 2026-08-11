using UnityEngine;

public class RabbitSetup : EnemyAIBase
{
    private enum State { None, Idle, Patrol, Chase, Lunge, Stunned }

    [Header("Rabbit Settings")]
    public float PatrolRadius = 3f;
    public float HopForce = 2f;
    public float HopInterval = 0.6f;
    public float AttackPrepTime = 0.5f;
    public float LungeDuration = 0.4f;
    [Header("Lunge Tuning")]
    public float LungeDistanceMultiplier = 1.6f;
    public float LungeAccel = 1.2f;
    public float LungeArcLowFactor = 0.5f;
    [Header("Ranges")]
    public float DetectRange = 10f;
    public float ChaseRange = 15f;
    public float AttackRange = 1.5f;
    [Min(0f)] public float AbovePlayerHorizontalDeadZone = 0.12f;
    [Header("Patrol Timing")]
    public float PatrolMoveTimeMin = 1f;
    public float PatrolMoveTimeMax = 2.5f;
    [Header("Animation")]
    public string IdleAnimation = "Rabbit_Idle";
    public string AttackAnimation = "Rabbit_Attack";
    public string AttackTrigger = "Attack";
    [Header("Facing")]
    public bool DefaultFacingRight = false;

    private State _state;
    private Vector2 _startPosition;
    private float _stateTimer;
    private float _idleDuration;
    private float _patrolDuration;
    private float _hopTimer;
    private float _stunDuration;
    private int _direction;
    private bool _isLunging;
    private float _lungeTimer;
    private float _lungeTime;
    private float _lungeVx;

    protected override void Awake()
    {
        base.Awake();
        _startPosition = transform.position;
    }

    private void Start() => ChangeState(State.Idle);

    private void Update()
    {
        switch (_state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Lunge: UpdateLunge(); break;
            case State.Stunned: UpdateStunned(); break;
        }
    }

    public override bool TryStun(float duration)
    {
        _stunDuration = Mathf.Max(_stunDuration, duration);
        _stateTimer = 0f;
        ChangeState(State.Stunned);
        return true;
    }

    private void ChangeState(State next)
    {
        if (_state == next) return;
        _state = next;
        _stateTimer = 0f;

        switch (next)
        {
            case State.Idle:
                Fsm.StopMovement();
                if (Fsm.Sr != null) Fsm.Sr.color = Color.white;
                _idleDuration = Random.Range(1f, 4f);
                break;
            case State.Patrol:
                if (Fsm.Sr != null) Fsm.Sr.color = Color.yellow;
                _patrolDuration = Random.Range(PatrolMoveTimeMin, PatrolMoveTimeMax);
                _direction = Random.value < 0.5f ? -1 : 1;
                _hopTimer = 0f;
                SetFacing(_direction > 0);
                break;
            case State.Chase:
                if (Fsm.Sr != null) Fsm.Sr.color = Color.red;
                _hopTimer = 0f;
                break;
            case State.Lunge:
                if (Fsm.Sr != null) Fsm.Sr.color = Color.magenta;
                Fsm.StopMovement();
                _isLunging = false;
                _hopTimer = 0f;
                break;
            case State.Stunned:
                Fsm.StopMovement();
                if (Fsm.Sr != null) Fsm.Sr.color = Color.blue;
                break;
        }
    }

    private void UpdateIdle()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _idleDuration) ChangeState(State.Patrol);
    }

    private void UpdatePatrol()
    {
        if (TryStartChase()) return;
        float offset = transform.position.x - _startPosition.x;
        if (Mathf.Abs(offset) >= PatrolRadius) _direction = (int)-Mathf.Sign(offset);
        MoveAndHop(_direction, Fsm.Data != null ? Fsm.Data.PatrolSpeed : 1f);
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _patrolDuration) ChangeState(State.Idle);
    }

    private void UpdateChase()
    {
        if (Fsm.Player == null) { ChangeState(State.Idle); return; }
        float distance = Vector2.Distance(transform.position, Fsm.Player.position);
        if (distance > ChaseRange) { ChangeState(State.Patrol); return; }
        if (IsPlayerDirectlyAbove())
        {
            Fsm.StopMovement();
            return;
        }
        if (distance < AttackRange) { ChangeState(State.Lunge); return; }
        MoveAndHop(Fsm.Player.position.x > transform.position.x ? 1 : -1, Fsm.Data != null ? Fsm.Data.MoveSpeed : 2f);
    }

    private void UpdateLunge()
    {
        if (Fsm.Player == null) { ChangeState(State.Idle); return; }
        if (!_isLunging)
        {
            if (IsPlayerDirectlyAbove())
            {
                Fsm.StopMovement();
                return;
            }

            _stateTimer += Time.deltaTime;
            SetFacing(Fsm.Player.position.x > transform.position.x);
            _hopTimer += Time.deltaTime;
            if (Fsm.Rb != null && _hopTimer >= HopInterval)
            {
                Fsm.Rb.linearVelocity = new Vector2(Fsm.Rb.linearVelocity.x, HopForce);
                _hopTimer = 0f;
            }
            if (_stateTimer >= AttackPrepTime) StartLunge();
            return;
        }
        _lungeTimer += Time.deltaTime;
        if (Fsm.Rb != null) Fsm.Rb.linearVelocity = new Vector2(_lungeVx * Mathf.Lerp(1f, 1f + LungeAccel, _lungeTimer / _lungeTime), Fsm.Rb.linearVelocity.y);
        if (_lungeTimer < _lungeTime) return;
        Fsm.StopMovement();
        if (Vector2.Distance(transform.position, Fsm.Player.position) <= AttackRange)
        {
            Fsm.PerformAttack(AttackRange);
            ChangeState(State.Idle);
            return;
        }
        ChangeState(State.Chase);
    }

    private void UpdateStunned()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _stunDuration) { _stunDuration = 0f; ChangeState(State.Chase); }
    }

    private bool TryStartChase()
    {
        if (Fsm.Player == null || Vector2.Distance(transform.position, Fsm.Player.position) > DetectRange) return false;
        ChangeState(State.Chase);
        return true;
    }

    private bool IsPlayerDirectlyAbove()
    {
        if (Fsm.Player == null) return false;

        Vector2 offset = Fsm.Player.position - transform.position;
        return offset.y > 0f && Mathf.Abs(offset.x) <= AbovePlayerHorizontalDeadZone;
    }

    private void MoveAndHop(int direction, float speed)
    {
        if (Fsm.Rb == null) return;
        Fsm.Rb.linearVelocity = new Vector2(direction * speed, Fsm.Rb.linearVelocity.y);
        _hopTimer += Time.deltaTime;
        if (_hopTimer >= HopInterval) { Fsm.Rb.linearVelocity = new Vector2(Fsm.Rb.linearVelocity.x, HopForce); _hopTimer = 0f; }
        SetFacing(direction > 0);
    }

    private void StartLunge()
    {
        _isLunging = true;
        _lungeTimer = 0f;
        _lungeTime = Mathf.Max(0.01f, LungeDuration);
        float dx = (Fsm.Player.position.x - transform.position.x) * LungeDistanceMultiplier;
        _lungeVx = dx / _lungeTime;
        if (Fsm.Rb != null)
        {
            float gravity = Physics2D.gravity.y * Fsm.Rb.gravityScale;
            float vy = (-0.5f * gravity * _lungeTime) * Mathf.Clamp01(LungeArcLowFactor);
            Fsm.Rb.linearVelocity = new Vector2(_lungeVx, vy);
        }
        if (Fsm.Anim != null && !string.IsNullOrEmpty(AttackTrigger) && Fsm.Anim.HasParameterOfType(AttackTrigger, AnimatorControllerParameterType.Trigger)) Fsm.Anim.SetTrigger(AttackTrigger);
        else if (Fsm.Anim != null && !string.IsNullOrEmpty(AttackAnimation)) Fsm.Anim.Play(AttackAnimation);
    }

    private void SetFacing(bool faceRight)
    {
        Vector3 scale = transform.localScale;
        float x = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        scale.x = (DefaultFacingRight ? faceRight : !faceRight) ? x : -x;
        transform.localScale = scale;
    }

    private void OnValidate()
    {
        AbovePlayerHorizontalDeadZone = Mathf.Max(0f, AbovePlayerHorizontalDeadZone);
    }
}
