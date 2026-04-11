using UnityEngine;

[CreateAssetMenu(menuName = "Skill/Checkmate/ChessPieceData")]
public class ChessPieceData : ScriptableObject
{
    public string pieceName;
    public float damage;
    public float stunChance;
    public float stunDuration;
    public float moveDistance;
    public float moveSpeed;
    public float effectRadius;
    public GameObject prefab;
    public int poolSize = 5;
}