using UnityEngine;

[RequireComponent(typeof(EnemyFSM))]
public class BirdSetup : MonoBehaviour
{
    [Header("Bird Settings")]
    public float PatrolRadius = 3f;
    public float AttackPrepTime = 0.5f;
    public float LungeDuration = 0.4f;

    [Header("Lunge Tuning")]
    public float LungeDistanceMultiplierFar = 3.0f;
    public float LungeTargetYOffset = -0.5f;
    public float LungeAccel = 1.2f;
    public float LungeArcLowFactor = 0.5f;
    [Tooltip("플레이어 넘어서 얼만큼 지나갈지(가로 오버슈트 거리)")]
    public float LungeOvershoot = 4f;
    [Tooltip("다이브 시작 시 추가로 주는 위로 솟구치는 초기 속도 (포물선 크기)")]
    public float LungeArcHeight = 4f;

    [Header("Ranges")]
    public float DetectRange = 10f;
    public float ChaseRange = 15f;
    public float AttackRange = 10f;

    [Header("Patrol Timing")]
    public float PatrolMoveTimeMin = 1f;
    public float PatrolMoveTimeMax = 2.5f;

    [Header("Hover Tuning")]
    public float HoverAmplitude = 0.6f;
    public float HoverFrequency = 1.2f;
    public float HoverDriftSpeed = 0.6f;
    public float HoverReturnSpeed = 4f;
    public float HoverMaxVy = 3f;
    [Tooltip("Chase / Attack 준비 시 플레이어 머리 위 어느 높이에서 대기할지")]
    public float HoverHeightAboveTarget = 3f;
    [Tooltip("공격 시 플레이어와 유지할 가로 거리. 너무 가깝게 붙으면 다이브가 안 그려짐")]
    public float AttackStandoffDistance = 5f;
    [Tooltip("강하(돌진) 중 종속도 상한. 너무 빠르게 직선으로 박는 현상 방지")]
    public float MaxDiveSpeed = 9f;

    private void Start()
    {
        var fsm = GetComponent<EnemyFSM>();

        fsm.Idle = new IdleState(fsm);
        fsm.Patrol = new BirdPatrolState(fsm, this);
        fsm.Chase = new BirdChaseState(fsm, this);
        fsm.Attack = new BirdAttackState(fsm, this);
        fsm.Stun = new StunState(fsm);

        fsm.Initialize(fsm.Idle);
    }
}
