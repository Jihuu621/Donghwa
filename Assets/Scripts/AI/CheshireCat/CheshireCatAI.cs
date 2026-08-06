using UnityEngine;

public class CheshireCatAI : EnemyAIBase
{
    public enum State
    {
        None,
        Idle,
        SmokeEnter,
        Teleport,
        ScratchWindup,
        ScratchDash,
        Barrage,
        CloneSetup,
        CloneAttack,
        DashWindup,
        Dash,
        Recovery,
        Stunned
    }

    private enum Pattern { TeleportCycle, Clones, Dash }

    [Header("Pattern Timing")]
    [SerializeField] private float idleDuration = 1f;
    [SerializeField] private float smokeDuration = 0.35f;
    [SerializeField] private float recoveryDuration = 0.8f;
    [SerializeField] private int teleportCountMin = 3;
    [SerializeField] private int teleportCountMax = 6;

    [Header("Teleport")]
    [SerializeField] private float teleportDistanceMin = 3f;
    [SerializeField] private float teleportDistanceMax = 6f;

    [Header("Scratch")]
    [SerializeField] private float scratchRange = 2f;
    [SerializeField] private float scratchWindupDuration = 1f;
    [SerializeField] private float scratchDashDuration = 0.25f;
    [SerializeField] private float scratchDashSpeed = 12f;

    [Header("Dash")]
    [SerializeField] private float dashWindupDuration = 0.45f;
    [SerializeField] private float dashDuration = 0.4f;
    [SerializeField] private float dashSpeed = 14f;

    public State CurrentState { get; private set; }
    public bool IsSmokeForm { get; private set; }

    private Pattern _pattern;
    private float _stateTimer;
    private float _stunDuration;
    private int _teleportCount;
    private int _teleportsCompleted;
    private Vector2 _attackTarget;
    private Color _normalColor;

    protected override void Awake()
    {
        base.Awake();
        _normalColor = Fsm.Sr != null ? Fsm.Sr.color : Color.white;
    }

    private void Start()
    {
        ChangeState(State.Idle);
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.SmokeEnter: UpdateSmokeEnter(); break;
            case State.Teleport: UpdateTeleport(); break;
            case State.ScratchWindup: UpdateScratchWindup(); break;
            case State.ScratchDash: UpdateScratchDash(); break;
            case State.Barrage: UpdateBarrage(); break;
            case State.CloneSetup: UpdateCloneSetup(); break;
            case State.CloneAttack: UpdateCloneAttack(); break;
            case State.DashWindup: UpdateDashWindup(); break;
            case State.Dash: UpdateDash(); break;
            case State.Recovery: UpdateRecovery(); break;
            case State.Stunned: UpdateStunned(); break;
        }
    }

    public override bool TryStun(float duration)
    {
        if (IsSmokeForm)
        {
            return false;
        }

        _stunDuration = Mathf.Max(_stunDuration, duration);
        _stateTimer = 0f;
        ChangeState(State.Stunned);
        return true;
    }

    private void ChangeState(State next)
    {
        if (CurrentState == next)
        {
            return;
        }

        if (CurrentState == State.ScratchDash || CurrentState == State.Dash)
        {
            Fsm.StopMovement();
        }

        CurrentState = next;
        _stateTimer = 0f;

        switch (next)
        {
            case State.Idle:
                SetSmokeForm(false);
                Fsm.StopMovement();
                break;
            case State.SmokeEnter:
                SetSmokeForm(true);
                Fsm.StopAllMovement();
                break;
            case State.ScratchWindup:
            case State.DashWindup:
                Fsm.StopMovement();
                break;
            case State.Stunned:
                SetSmokeForm(false);
                Fsm.StopMovement();
                break;
        }
    }

    private void UpdateIdle()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer < idleDuration)
        {
            return;
        }

        _pattern = (Pattern)Random.Range(0, 3);
        _teleportCount = Random.Range(teleportCountMin, teleportCountMax + 1);
        _teleportsCompleted = 0;
        ChangeState(State.SmokeEnter);
    }

    private void UpdateSmokeEnter()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= smokeDuration)
        {
            ChangeState(State.Teleport);
        }
    }

    private void UpdateTeleport()
    {
        TeleportAroundPlayer();
        SetSmokeForm(false);

        switch (_pattern)
        {
            case Pattern.TeleportCycle:
                if (Fsm.Player != null && Vector2.Distance(transform.position, Fsm.Player.position) <= scratchRange)
                {
                    ChangeState(State.ScratchWindup);
                }
                else
                {
                    ChangeState(State.Barrage);
                }
                break;
            case Pattern.Clones:
                ChangeState(State.CloneSetup);
                break;
            case Pattern.Dash:
                ChangeState(State.DashWindup);
                break;
        }
    }

    private void UpdateScratchWindup()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= scratchWindupDuration)
        {
            _attackTarget = Fsm.Player != null ? Fsm.Player.position : transform.position;
            ChangeState(State.ScratchDash);
        }
    }

    private void UpdateScratchDash()
    {
        MoveTowards(_attackTarget, scratchDashSpeed);
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= scratchDashDuration)
        {
            Fsm.PerformAttack(scratchRange);
            ContinueTeleportCycle();
        }
    }

    private void UpdateBarrage()
    {
        // Projectile spawning belongs here once the c-1 projectile is implemented.
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= 0.2f)
        {
            ContinueTeleportCycle();
        }
    }

    private void UpdateCloneSetup()
    {
        // Clone spawning and formation logic belong here.
        ChangeState(State.CloneAttack);
    }

    private void UpdateCloneAttack()
    {
        // The slow homing smoke orb belongs here.
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= 0.8f)
        {
            ChangeState(State.Recovery);
        }
    }

    private void UpdateDashWindup()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= dashWindupDuration)
        {
            _attackTarget = Fsm.Player != null ? Fsm.Player.position : transform.position;
            ChangeState(State.Dash);
        }
    }

    private void UpdateDash()
    {
        MoveTowards(_attackTarget, dashSpeed);
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= dashDuration)
        {
            Fsm.PerformAttack(scratchRange);
            ChangeState(State.Recovery);
        }
    }

    private void UpdateRecovery()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= recoveryDuration)
        {
            ChangeState(State.Idle);
        }
    }

    private void UpdateStunned()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _stunDuration)
        {
            _stunDuration = 0f;
            ChangeState(State.Recovery);
        }
    }

    private void ContinueTeleportCycle()
    {
        _teleportsCompleted++;
        ChangeState(_teleportsCompleted >= _teleportCount ? State.Recovery : State.SmokeEnter);
    }

    private void TeleportAroundPlayer()
    {
        if (Fsm.Player == null)
        {
            return;
        }

        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.right;
        }

        transform.position = (Vector2)Fsm.Player.position + direction * Random.Range(teleportDistanceMin, teleportDistanceMax);
    }

    private void MoveTowards(Vector2 target, float speed)
    {
        if (Fsm.Rb == null)
        {
            return;
        }

        Vector2 direction = (target - (Vector2)transform.position).normalized;
        Fsm.Rb.linearVelocity = direction * speed;
    }

    private void SetSmokeForm(bool enabled)
    {
        IsSmokeForm = enabled;
        if (Fsm.Sr == null)
        {
            return;
        }

        Color color = _normalColor;
        color.a = enabled ? 0.35f : 1f;
        Fsm.Sr.color = color;
    }
}
