using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string EnemyName;
    [TextArea]
    public string EnemyDescription;

    public float MaxHP;
    public float Damage;
    public float MoveSpeed;
    public float PatrolSpeed;
}