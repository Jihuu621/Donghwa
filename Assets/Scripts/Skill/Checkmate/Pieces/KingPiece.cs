using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 킹: 플레이어 오른쪽에서 스폰 → 오른쪽으로 이동하며 데미지.
/// 플레이어 방향과 무관하게 항상 오른쪽 기준으로 동작한다.
/// </summary>
public class KingPiece : ChessPiece
{
    private static readonly Collider2D[] hitBuffer = new Collider2D[16];

    public override void Execute(Transform target, Vector3 playerPosition, float facingDir = 1f)
    {
        StartCoroutine(KingRoutine(playerPosition));
    }

    private IEnumerator KingRoutine(Vector3 playerPos)
    {
        // 발동 시점 플레이어 위치 기준 스폰 (이후 플레이어가 움직여도 영향 없음)
        transform.position = playerPos + Vector3.right * skillData.kingSpawnOffset;

        Vector3 moveDir = Vector3.right;
        float distanceTraveled = 0f;
        float totalDistance = skillData.kingMoveDistance;
        float speed = skillData.kingMoveSpeed;

        List<Collider2D> hitTargets = new List<Collider2D>(16);

        while (distanceTraveled < totalDistance)
        {
            float step = speed * Time.deltaTime;
            transform.position += moveDir * step;
            distanceTraveled += step;

            int count = Physics2D.OverlapCircleNonAlloc(transform.position, 1.2f, hitBuffer, enemyLayer);
            for (int i = 0; i < count; i++)
            {
                if (hitBuffer[i] == null) continue;
                if (hitTargets.Contains(hitBuffer[i])) continue;

                hitTargets.Add(hitBuffer[i]);
                GameObject enemy = hitBuffer[i].gameObject;

                // 데미지 적용
                ApplyDamage(enemy, skillData.kingDamage);
                // 킹에게 스턴 효과가 설정되어 있다면 시도
                TryApplyStun(enemy, skillData.kingStunChance, skillData.kingStunDuration);

                Debug.Log($"<color=yellow>[킹]</color> {enemy.name}에게 {skillData.kingDamage} 데미지");
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        ReturnToPool();
    }
}