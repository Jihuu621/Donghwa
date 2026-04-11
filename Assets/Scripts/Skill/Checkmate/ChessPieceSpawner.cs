using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무브 카운트에 따라 적절한 체스 기물을 오브젝트 풀에서 스폰한다.
/// </summary>
public class ChessPieceSpawner
{
    private readonly ObjectPoolManager poolManager;
    private readonly CheckmateSkillData skillData;
    private readonly LayerMask enemyLayer;

    // 기물 프리팹 참조 (체크메이트 스킬에서 주입)
    private readonly ChessPieceData pawnData;
    private readonly ChessPieceData bishopData;
    private readonly ChessPieceData knightData;
    private readonly ChessPieceData rookData;
    private readonly ChessPieceData queenData;
    private readonly ChessPieceData kingData;

    // NonAlloc 버퍼
    private static Collider2D[] enemyBuffer = new Collider2D[32];

    public ChessPieceSpawner(
        ObjectPoolManager poolManager,
        CheckmateSkillData skillData,
        LayerMask enemyLayer,
        ChessPieceData pawn,
        ChessPieceData bishop,
        ChessPieceData knight,
        ChessPieceData rook,
        ChessPieceData queen,
        ChessPieceData king)
    {
        this.poolManager = poolManager;
        this.skillData = skillData;
        this.enemyLayer = enemyLayer;
        this.pawnData = pawn;
        this.bishopData = bishop;
        this.knightData = knight;
        this.rookData = rook;
        this.queenData = queen;
        this.kingData = king;
    }

    /// <summary>
    /// 풀 초기화. 게임 시작 시 호출한다.
    /// </summary>
    public void InitializePools()
    {
        if (pawnData != null && pawnData.prefab != null)
            poolManager.InitializePool(pawnData.prefab, pawnData.poolSize);
        if (bishopData != null && bishopData.prefab != null)
            poolManager.InitializePool(bishopData.prefab, bishopData.poolSize);
        if (knightData != null && knightData.prefab != null)
            poolManager.InitializePool(knightData.prefab, knightData.poolSize);
        if (rookData != null && rookData.prefab != null)
            poolManager.InitializePool(rookData.prefab, rookData.poolSize);
        if (queenData != null && queenData.prefab != null)
            poolManager.InitializePool(queenData.prefab, queenData.poolSize);
        if (kingData != null && kingData.prefab != null)
            poolManager.InitializePool(kingData.prefab, kingData.poolSize);
    }

    /// <summary>
    /// 무브 수와 타겟 목록에 따라 기물을 스폰한다.
    /// facingDir: 플레이어가 바라보는 방향 (1 = 오른쪽, -1 = 왼쪽)
    /// </summary>
    public void SpawnPieces(int moveCount, List<Transform> targets, Vector3 playerPosition, float facingDir)
    {
        if (targets.Count == 0) return;

        if (moveCount >= skillData.queenKingMinMoves)
        {
            SpawnQueenAndKing(playerPosition, facingDir);
        }
        else if (moveCount >= skillData.rookMinMoves)
        {
            SpawnRook(targets, playerPosition, facingDir);
        }
        else if (moveCount >= skillData.bishopKnightMinMoves)
        {
            SpawnBishopOrKnight(targets, playerPosition, facingDir);
        }
        else if (moveCount >= skillData.pawnMinMoves)
        {
            SpawnPawns(targets, playerPosition, facingDir);
        }
        else
        {
            SpawnPawns(targets, playerPosition, facingDir);
        }

        Debug.Log($"<color=cyan>[체크메이트]</color> 무브 {moveCount} → 기물 스폰 완료");
    }

    private void SpawnPawns(List<Transform> targets, Vector3 playerPos, float facingDir)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            SpawnPiece(pawnData, targets[i], playerPos, facingDir);
        }
    }

    private void SpawnBishopOrKnight(List<Transform> targets, Vector3 playerPos, float facingDir)
    {
        Transform closest = FindClosestTarget(targets, playerPos);

        // 비숍 또는 나이트 중 하나만 랜덤 스폰
        bool spawnBishop = Random.value < 0.5f;

        if (spawnBishop)
        {
            SpawnPiece(bishopData, closest, playerPos, facingDir);
            Debug.Log("<color=purple>[체크메이트]</color> 비숍 선택됨");
        }
        else
        {
            SpawnPiece(knightData, closest, playerPos, facingDir);
            Debug.Log("<color=green>[체크메이트]</color> 나이트 선택됨");
        }
    }

    private void SpawnRook(List<Transform> targets, Vector3 playerPos, float facingDir)
    {
        Transform closest = FindClosestTarget(targets, playerPos);
        SpawnPiece(rookData, closest, playerPos, facingDir);
    }

    private void SpawnQueenAndKing(Vector3 playerPos, float facingDir)
    {
        SpawnPiece(queenData, null, playerPos, facingDir);
        SpawnPiece(kingData, null, playerPos, facingDir);
    }

    private void SpawnPiece(ChessPieceData pieceData, Transform target, Vector3 playerPos, float facingDir)
    {
        if (pieceData == null || pieceData.prefab == null) return;

        Vector3 spawnPos = target != null ? target.position + Vector3.up * 3f : playerPos;
        GameObject obj = poolManager.Get(pieceData.prefab, spawnPos, Quaternion.identity);

        // 풀에서 꺼낸 오브젝트의 부모를 해제하여 플레이어 이동에 종속되지 않게 함
        obj.transform.SetParent(null);

        ChessPiece piece = obj.GetComponent<ChessPiece>();
        if (piece != null)
        {
            piece.Initialize(poolManager, skillData, enemyLayer);
            piece.Execute(target, playerPos, facingDir);
        }
    }

    /// <summary>
    /// 플레이어에게 가장 가까운 적을 찾는다. sqrMagnitude로 최적화.
    /// </summary>
    private Transform FindClosestTarget(List<Transform> targets, Vector3 origin)
    {
        Transform closest = null;
        float closestSqr = float.MaxValue;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            float dx = targets[i].position.x - origin.x;
            float dy = targets[i].position.y - origin.y;
            float sqr = dx * dx + dy * dy;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = targets[i];
            }
        }

        return closest;
    }
}