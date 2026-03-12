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
    /// </summary>
    public void SpawnPieces(int moveCount, List<Transform> targets, Vector3 playerPosition)
    {
        if (targets.Count == 0) return;

        if (moveCount >= skillData.queenKingMinMoves)
        {
            SpawnQueenAndKing(playerPosition);
        }
        else if (moveCount >= skillData.rookMinMoves)
        {
            SpawnRook(targets, playerPosition);
        }
        else if (moveCount >= skillData.bishopKnightMinMoves)
        {
            SpawnBishopAndKnight(targets, playerPosition);
        }
        else if (moveCount >= skillData.pawnMinMoves)
        {
            SpawnPawns(targets, playerPosition);
        }
        else
        {
            // 0 무브: 폰 1개라도 스폰
            SpawnPawns(targets, playerPosition);
        }

        Debug.Log($"<color=cyan>[체크메이트]</color> 무브 {moveCount} → 기물 스폰 완료");
    }

    private void SpawnPawns(List<Transform> targets, Vector3 playerPos)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            SpawnPiece(pawnData, targets[i], playerPos);
        }
    }

    private void SpawnBishopAndKnight(List<Transform> targets, Vector3 playerPos)
    {
        Transform closest = FindClosestTarget(targets, playerPos);

        SpawnPiece(bishopData, closest, playerPos);
        SpawnPiece(knightData, closest, playerPos);
    }

    private void SpawnRook(List<Transform> targets, Vector3 playerPos)
    {
        Transform closest = FindClosestTarget(targets, playerPos);
        SpawnPiece(rookData, closest, playerPos);
    }

    private void SpawnQueenAndKing(Vector3 playerPos)
    {
        // 퀸/킹은 플레이어 기준으로 스폰하므로 타겟 없이 실행
        SpawnPiece(queenData, null, playerPos);
        SpawnPiece(kingData, null, playerPos);
    }

    private void SpawnPiece(ChessPieceData pieceData, Transform target, Vector3 playerPos)
    {
        if (pieceData == null || pieceData.prefab == null) return;

        Vector3 spawnPos = target != null ? target.position + Vector3.up * 3f : playerPos;
        GameObject obj = poolManager.Get(pieceData.prefab, spawnPos, Quaternion.identity);

        ChessPiece piece = obj.GetComponent<ChessPiece>();
        if (piece != null)
        {
            piece.Initialize(poolManager, skillData, enemyLayer);
            piece.Execute(target, playerPos);
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