using UnityEngine;

[CreateAssetMenu(menuName = "Skill/Checkmate/SkillData")]
public class CheckmateSkillData : ScriptableObject
{
    [Header("감지")]
    public float detectionRadius = 8f;
    public float resolutionTime = 3f;

    [Header("무브 임계값")]
    public float movementThreshold = 2f; // Y미터마다 +1 무브

    [Header("티어 구간")]
    public int pawnMinMoves = 1;
    public int pawnMaxMoves = 2;
    public int bishopKnightMinMoves = 3;
    public int bishopKnightMaxMoves = 4;
    public int rookMinMoves = 5;
    public int rookMaxMoves = 8;
    public int queenKingMinMoves = 9;

    [Header("Pawn")]
    public float pawnDamage = 30f;
    public float pawnFallSpeed = 12f;
    public float pawnSpawnHeight = 5f;

    [Header("Bishop")]
    public float bishopDamage = 40f;
    public float bishopChainRadius = 6f;
    public float bishopChainYThreshold = 1.5f;
    public float bishopChainDamage = 25f;
    public float bishopMoveSpeed = 15f;

    [Header("Knight")]
    public float knightDamage = 35f;
    public float knightStunChance = 0.4f;
    public float knightStunDuration = 1.5f;
    public float knightJumpRadius = 5f;
    public int knightMaxJumps = 2;
    public float knightMoveSpeed = 18f;

    [Header("Rook")]
    public float rookDamage = 50f;
    public float rookStunChance = 0.5f;
    public float rookStunDuration = 2f;
    public float rookDashDistance = 10f;
    public float rookDashDamage = 35f;
    public float rookDashStunChance = 0.3f;
    public float rookDashStunDuration = 1f;
    public float rookKnockback = 8f;
    public float rookDashSpeed = 20f;

    [Header("Queen")]
    public float queenDamage = 60f;
    public float queenMoveDistance = 8f;
    public float queenStunChance = 0.6f;
    public float queenStunDuration = 2f;
    public float queenMoveSpeed = 14f;
    public float queenSpawnOffset = 2f;

    [Header("King")]
    public float kingDamage = 80f;
    public float kingMoveDistance = 8f;
    public float kingMoveSpeed = 14f;
    public float kingSpawnOffset = 2f;
}