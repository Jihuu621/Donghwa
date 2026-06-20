using UnityEngine;

[RequireComponent(typeof(EnemyFSM))]
public class ShieldEnemySetup : MonoBehaviour
{
    [Header("Ranges")]
    public float DetectRange = 10f;
    public float ChaseRange = 15f;
    public float AttackRange = 1.6f;

    [Header("Patrol Timing")]
    public float PatrolMoveTimeMin = 1f;
    public float PatrolMoveTimeMax = 4f;

    [Header("Attack Behavior (General)")]
    public float AttackCooldown = 1.2f;

    [Header("Block Pattern")]
    [Range(0f, 1f)]
    public float BlockChance = 0.4f;
    public float BlockTime = 1.2f;
    [Range(0f, 1f)]
    public float CounterAttackChance = 0.25f;
    public float CounterAttackDelay = 0.3f;

    [Header("Block Stab Pattern")]
    public float BlockStabDelay = 0.2f;
    public float BlockStabRange = 1.8f;
    public float BlockStabExitTime = 0.5f;

    [Header("Stab Pattern")]
    public float StabDelay = 0.4f;
    public float StabRange = 2.0f;
    public float StabExitDelay = 0.3f;

    [Header("Slam Pattern")]
    public float SlamDelay = 0.7f;
    public float SlamRange = 1.5f;
    public float SlamExitDelay = 0.5f;

    [Header("ChargeSwing Pattern")]
    public float ChargeSpeed = 5f;
    public float ChargeAttackRange = 2.2f;
    public float ChargeDuration = 1.0f;
    public float ChargeAttackStart = 0.5f;
    public float ChargeAttackEnd = 0.6f;

    private void Start()
    {
        var fsm = GetComponent<EnemyFSM>();

        fsm.Idle = new IdleState(fsm);
        fsm.Patrol = new ShieldPatrolState(fsm, this);
        fsm.Chase = new ShieldChaseState(fsm, this);
        fsm.Attack = new ShieldAttackState(fsm, this);
        fsm.Stun = new StunState(fsm);

        fsm.Initialize(fsm.Idle);
    }
}
