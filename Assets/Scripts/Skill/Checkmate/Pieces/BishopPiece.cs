using System.Collections;
using UnityEngine;

/// <summary>
/// 비숍: 가장 가까운 적에게 이동 → 데미지 → 반경 내 Y축 차이가 큰 적에게 연쇄 공격.
/// </summary>
public class BishopPiece : ChessPiece
{
    // NonAlloc 재사용 버퍼
    private static Collider2D[] chainBuffer = new Collider2D[16];

    public override void Execute(Transform target, Vector3 playerPosition, float facingDir = 1f)
    {
        StartCoroutine(BishopRoutine(target));
    }

    private IEnumerator BishopRoutine(Transform primaryTarget)
    {
        if (primaryTarget == null)
        {
            ReturnToPool();
            yield break;
        }

        // 1차 타겟으로 이동
        yield return MoveToTarget(primaryTarget.position);

        // 1차 데미지
        ApplyDamage(primaryTarget.gameObject, skillData.bishopDamage);
        Debug.Log($"<color=purple>[비숍]</color> {primaryTarget.name}에게 {skillData.bishopDamage} 데미지");

        // 연쇄 공격: 반경 내 Y축 차이가 임계값 이상인 적 탐색
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, skillData.bishopChainRadius, chainBuffer, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            if (chainBuffer[i] == null) continue;
            Transform chainTarget = chainBuffer[i].transform;

            // 자기 자신(이미 때린 적) 제외
            if (chainTarget == primaryTarget) continue;

            // Y축 차이 조건 확인
            float yDiff = Mathf.Abs(transform.position.y - chainTarget.position.y);
            if (yDiff <= skillData.bishopChainYThreshold) continue;

            // 연쇄 대상으로 이동 후 공격
            yield return MoveToTarget(chainTarget.position);
            ApplyDamage(chainTarget.gameObject, skillData.bishopChainDamage);
            Debug.Log($"<color=purple>[비숍 연쇄]</color> {chainTarget.name}에게 {skillData.bishopChainDamage} 데미지");
        }

        yield return new WaitForSeconds(0.2f);
        ReturnToPool();
    }

    private IEnumerator MoveToTarget(Vector3 destination)
    {
        float speed = skillData.bishopMoveSpeed;
        while (true)
        {
            float dx = destination.x - transform.position.x;
            float dy = destination.y - transform.position.y;
            float sqrDist = dx * dx + dy * dy;

            if (sqrDist < 0.1f) break;

            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = destination;
    }
}