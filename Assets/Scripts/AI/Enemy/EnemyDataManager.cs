using UnityEngine;

public class EnemyDataManager : MonoBehaviour
{
    [SerializeField] private EnemyData _enemyData;  // 인스펙터에서 할당

    // 외부에서 읽을 수 있는 프로퍼티
    public EnemyData EnemyData => _enemyData;
}
