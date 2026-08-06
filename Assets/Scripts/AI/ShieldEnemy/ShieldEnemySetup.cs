using UnityEngine;

public class ShieldEnemySetup : EnemyAIBase
{
    private enum State { None, Idle, Patrol, Chase, Attack, Stunned }
    private enum AttackMode { Select, Block, BlockStab, Stab, Slam, ChargeSwing }
    [Header("Ranges")] public float DetectRange = 10f; public float ChaseRange = 15f; public float AttackRange = 1.6f;
    [Header("Patrol Timing")] public float PatrolMoveTimeMin = 1f; public float PatrolMoveTimeMax = 4f;
    [Header("Attack Behavior (General)")] public float AttackCooldown = 1.2f;
    [Header("Block Pattern")] [Range(0f, 1f)] public float BlockChance = .4f; public float BlockTime = 1.2f; [Range(0f, 1f)] public float CounterAttackChance = .25f; public float CounterAttackDelay = .3f;
    [Header("Block Stab Pattern")] public float BlockStabDelay = .2f; public float BlockStabRange = 1.8f; public float BlockStabExitTime = .5f;
    [Header("Stab Pattern")] public float StabDelay = .4f; public float StabRange = 2f; public float StabExitDelay = .3f;
    [Header("Slam Pattern")] public float SlamDelay = .7f; public float SlamRange = 1.5f; public float SlamExitDelay = .5f;
    [Header("ChargeSwing Pattern")] public float ChargeSpeed = 5f; public float ChargeAttackRange = 2.2f; public float ChargeDuration = 1f; public float ChargeAttackStart = .5f; public float ChargeAttackEnd = .6f;
    private State _state; private AttackMode _attackMode; private float _stateTimer, _idleDuration, _patrolDuration, _stunDuration; private int _direction; private bool _hasAttacked, _didCounter; private Vector3 _originScale; private ShieldEnemyManager _shield;
    protected override void Awake() { base.Awake(); _originScale = transform.localScale; _shield = GetComponent<ShieldEnemyManager>(); }
    private void Start() => ChangeState(State.Idle);
    private void Update() { switch (_state) { case State.Idle: UpdateIdle(); break; case State.Patrol: UpdatePatrol(); break; case State.Chase: UpdateChase(); break; case State.Attack: UpdateAttack(); break; case State.Stunned: UpdateStunned(); break; } }
    public override bool TryStun(float duration) { _stunDuration = Mathf.Max(_stunDuration, duration); ChangeState(State.Stunned); return true; }
    private void ChangeState(State next)
    {
        if (_state == next) return; _state = next; _stateTimer = 0f;
        switch (next) { case State.Idle: Fsm.StopMovement(); if (Fsm.Sr != null) Fsm.Sr.color = Color.white; _idleDuration = Random.Range(1f, 4f); break; case State.Patrol: if (Fsm.Sr != null) Fsm.Sr.color = Color.yellow; _patrolDuration = Random.Range(PatrolMoveTimeMin, PatrolMoveTimeMax); _direction = Random.value < .5f ? -1 : 1; Face(_direction); break; case State.Chase: if (Fsm.Sr != null) Fsm.Sr.color = Color.red; break; case State.Attack: if (Fsm.Sr != null) Fsm.Sr.color = Color.magenta; Fsm.StopMovement(); SetAttackMode(AttackMode.Select); break; case State.Stunned: Fsm.StopMovement(); if (Fsm.Sr != null) Fsm.Sr.color = Color.blue; break; }
    }
    private void UpdateIdle() { _stateTimer += Time.deltaTime; if (_stateTimer >= _idleDuration) ChangeState(State.Patrol); }
    private void UpdatePatrol() { if (Fsm.Player != null && Vector2.Distance(transform.position, Fsm.Player.position) <= DetectRange) { ChangeState(State.Chase); return; } Move(_direction, Fsm.Data != null ? Fsm.Data.PatrolSpeed : 1f); _stateTimer += Time.deltaTime; if (_stateTimer >= _patrolDuration) ChangeState(State.Idle); }
    private void UpdateChase() { if (Fsm.Player == null) { ChangeState(State.Idle); return; } float distance = Vector2.Distance(transform.position, Fsm.Player.position); if (distance > ChaseRange) { ChangeState(State.Patrol); return; } if (distance < AttackRange) { ChangeState(State.Attack); return; } Move(Fsm.Player.position.x > transform.position.x ? 1 : -1, Fsm.Data != null ? Fsm.Data.MoveSpeed : 2.5f); }
    private void UpdateAttack()
    {
        if (Fsm.Player == null) { ChangeState(State.Idle); return; } _stateTimer += Time.deltaTime;
        switch (_attackMode) { case AttackMode.Select: if (_stateTimer >= AttackCooldown) SetAttackMode(_shield != null && !_shield.IsShieldBroken && Random.value < BlockChance ? AttackMode.Block : (AttackMode)Random.Range(3, 6)); break; case AttackMode.Block: if (_shield != null && _shield.IsShieldBroken) SetAttackMode(AttackMode.Select); else if (!_didCounter && _stateTimer > CounterAttackDelay && Random.value < CounterAttackChance) { _didCounter = true; SetAttackMode(AttackMode.BlockStab); } else if (_stateTimer >= BlockTime) ChangeState(State.Chase); break; case AttackMode.BlockStab: AttackOnce(BlockStabDelay, BlockStabRange); if (_stateTimer >= BlockStabExitTime) ChangeState(State.Chase); break; case AttackMode.Stab: AttackOnce(StabDelay, StabRange); if (_stateTimer >= StabDelay + StabExitDelay) ChangeState(State.Chase); break; case AttackMode.Slam: AttackOnce(SlamDelay, SlamRange); if (_stateTimer >= SlamDelay + SlamExitDelay) ChangeState(State.Chase); break; case AttackMode.ChargeSwing: UpdateCharge(); break; }
    }
    private void UpdateCharge() { if (_stateTimer < ChargeAttackStart) Move(transform.localScale.x > 0f ? 1 : -1, ChargeSpeed); else if (_stateTimer < ChargeAttackEnd) { Fsm.StopMovement(); AttackOnce(0f, ChargeAttackRange); } else if (_stateTimer >= ChargeDuration) ChangeState(State.Chase); }
    private void AttackOnce(float delay, float range) { if (_hasAttacked || _stateTimer < delay) return; Fsm.PerformAttack(range); _hasAttacked = true; }
    private void UpdateStunned() { _stateTimer += Time.deltaTime; if (_stateTimer >= _stunDuration) { _stunDuration = 0f; ChangeState(State.Chase); } }
    private void SetAttackMode(AttackMode mode) { _attackMode = mode; _stateTimer = 0f; _hasAttacked = false; _didCounter = false; }
    private void Move(int direction, float speed) { if (Fsm.Rb != null) Fsm.Rb.linearVelocity = new Vector2(direction * speed, Fsm.Rb.linearVelocity.y); Face(direction); }
    private void Face(float direction) { transform.localScale = new Vector3(Mathf.Abs(_originScale.x) * Mathf.Sign(direction), _originScale.y, _originScale.z); }
}
