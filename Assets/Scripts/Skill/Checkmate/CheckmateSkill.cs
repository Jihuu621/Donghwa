using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 체크메이트 스킬 메인 컨트롤러.
/// E키로 활성화 → 적 감지 → 무브 누적 → 해결(기물 스폰).
/// </summary>
public class CheckmateSkill : MonoBehaviour
{
    public enum SkillState { Ready, Tracking, Resolved }

    [Header("Data")]
    public CheckmateSkillData skillData;

    [Header("Chess Piece Data")]
    public ChessPieceData pawnData;
    public ChessPieceData bishopData;
    public ChessPieceData knightData;
    public ChessPieceData rookData;
    public ChessPieceData queenData;
    public ChessPieceData kingData;

    [Header("Settings")]
    public LayerMask enemyLayer;

    [Header("Runtime")]
    public SkillState currentState = SkillState.Ready;

    // 캐시된 참조
    private ObjectPoolManager poolManager;
    private CheckmateTargetTracker targetTracker;
    private MoveCounter moveCounter;
    private ChessPieceSpawner spawner;
    private PlayerController playerController;

    private float trackingTimer;

    public SkillState CurrentSkillState => currentState;

    void Start()
    {
        // ObjectPoolManager를 같은 오브젝트 또는 자식에서 찾거나 생성
        poolManager = GetComponent<ObjectPoolManager>();
        if (poolManager == null)
        {
            poolManager = gameObject.AddComponent<ObjectPoolManager>();
        }

        // 플레이어 방향 참조 캐시
        playerController = GetComponent<PlayerController>();

        // 모듈 초기화
        moveCounter = new MoveCounter(skillData.movementThreshold);
        targetTracker = new CheckmateTargetTracker(
            skillData.detectionRadius, enemyLayer, OnEnemyAttack);

        spawner = new ChessPieceSpawner(
            poolManager, skillData, enemyLayer,
            pawnData, bishopData, knightData,
            rookData, queenData, kingData);

        spawner.InitializePools();
    }

    void Update()
    {
        // 다른 스킬 활성 상태면 무시
        var rainbowFlush = GetComponent<RainbowFlushSkill>();
        if (rainbowFlush != null && rainbowFlush.IsSkill() != RainbowFlushSkill.SkillState.Ready)
            return;

        switch (currentState)
        {
            case SkillState.Ready:
                if (Input.GetKeyDown(KeyCode.E))
                {
                    ActivateSkill();
                }
                break;

            case SkillState.Tracking:
                UpdateTracking();
                // E 재입력으로 즉시 해결
                if (Input.GetKeyDown(KeyCode.E))
                {
                    ResolveSkill();
                }
                break;
        }
    }

    /// <summary>
    /// 스킬 활성화: 적 감지 시작.
    /// </summary>
    private void ActivateSkill()
    {
        currentState = SkillState.Tracking;
        trackingTimer = 0f;

        // 무브 카운터 초기화
        moveCounter.Reset();

        // 반경 내 적 감지
        targetTracker.DetectTargets(transform.position);

        // 감지된 적을 무브 카운터에 등록
        List<Transform> targets = targetTracker.TrackedTargets;
        for (int i = 0; i < targets.Count; i++)
        {
            moveCounter.RegisterTarget(targets[i]);
        }

        Debug.Log($"<color=cyan>[체크메이트]</color> 스킬 활성화! 추적 시작 ({targets.Count}명)");
    }

    /// <summary>
    /// 추적 중 로직: 무브 누적 + 타이머.
    /// </summary>
    private void UpdateTracking()
    {
        trackingTimer += Time.deltaTime;

        // 파괴된 적 정리
        targetTracker.CleanupDestroyedTargets();

        // 이동 거리 누적
        moveCounter.UpdateMovement(targetTracker.TrackedTargets);

        // 3초 경과 시 자동 해결
        if (trackingTimer >= skillData.resolutionTime)
        {
            ResolveSkill();
        }
    }

    /// <summary>
    /// 스킬 해결: 무브 수에 따라 체스 기물 스폰.
    /// 발동 시점의 플레이어 위치와 방향을 스냅샷으로 넘긴다.
    /// </summary>
    private void ResolveSkill()
    {
        int totalMoves = moveCounter.MoveCount;
        List<Transform> targets = targetTracker.TrackedTargets;

        // 발동 시점 위치/방향 스냅샷 (이후 플레이어가 움직여도 영향 없음)
        Vector3 snapshotPos = transform.position;
        float facingDir = 1f;
        if (playerController != null)
        {
            facingDir = playerController.FacingRight ? 1f : -1f;
        }
        else
        {   
            facingDir = transform.localScale.x > 0f ? 1f : -1f;
        }

        Debug.Log($"<color=cyan>[체크메이트]</color> 해결! 총 무브: {totalMoves}");

        // 기물 스폰 (스냅샷 위치/방향 전달)
        spawner.SpawnPieces(totalMoves, targets, snapshotPos, facingDir);

        // 정리
        targetTracker.Clear();
        currentState = SkillState.Ready;
    }

    /// <summary>
    /// IAttackNotifier 이벤트 콜백. 적이 공격할 때마다 호출.
    /// </summary>
    private void OnEnemyAttack()
    {
        if (currentState == SkillState.Tracking)
        {
            moveCounter.OnEnemyAttack();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (skillData == null) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, skillData.detectionRadius);
    }
}
