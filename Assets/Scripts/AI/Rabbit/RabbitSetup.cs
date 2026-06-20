using UnityEngine;

[RequireComponent(typeof(EnemyFSM))]
public class RabbitSetup : MonoBehaviour
{
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

    [Header("Patrol Timing")]
    public float PatrolMoveTimeMin = 1f;
    public float PatrolMoveTimeMax = 2.5f;

    [Header("Animation")]
    public string IdleAnimation = "Rabbit_Idle";
    public string AttackAnimation = "Rabbit_Attack";
    public string AttackTrigger = "Attack";

    [Header("Facing")]
    public bool DefaultFacingRight = false;

    private void Start()
    {
        var fsm = GetComponent<EnemyFSM>();

        fsm.Idle = new IdleState(fsm);
        fsm.Patrol = new RabbitPatrolState(fsm, this);
        fsm.Chase = new RabbitChaseState(fsm, this);
        fsm.Attack = new RabbitAttackState(fsm, this);
        fsm.Stun = new StunState(fsm);

        fsm.Initialize(fsm.Idle);
    }
}
