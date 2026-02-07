using UnityEngine;

public class PlayerDebugger : MonoBehaviour
{
    public PlayerParry parry;
    public float debugRadius = 1.5f;

    private void OnDrawGizmos()
    {
        if (parry == null) return;

        // 상태별 색상
        if (parry.IsFailTime)
            Gizmos.color = Color.red;      // 패링 실패 구간
        else if (parry.IsParryTime)
            Gizmos.color = Color.green;    // 패링 성공 구간
        else if (parry.IsGuardTime)
            Gizmos.color = Color.blue;     // 가드 구간
        else
            Gizmos.color = Color.white;    // 아무 상태 아님

        Gizmos.DrawWireSphere(transform.position, debugRadius);
    }
}
