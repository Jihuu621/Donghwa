using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룩: 가장 가까운 적에게 돌진 → 데미지 + 스턴 → 적이 가장 많은 방향으로 직선 대시.
/// </summary>
public class RookPiece : ChessPiece
{
    private static Collider2D[] scanBuffer = new Collider2D[32];
    private static Collider2D[] dashHitBuffer = new Collider2D[16];

    public override void Execute(Transform target, Vector3 playerPosition)
    {
        StartCoroutine(RookRoutine(target));
    }

    private IEnumerator RookRoutine(Transform primaryTarget)
    {
        if (primaryTarget == null)
        {
            ReturnToPool();
            yield break;
        }

        // 1차 타겟으로 이동
        yield return MoveToTarget(primaryTarget.position);

        // 1차 데미지 + 스턴
        ApplyDamage(primaryTarget.gameObject, skillData.rookDamage);
        TryApplyStun(primaryTarget.gameObject, skillData.rookStunChance, skillData.rookStunDuration);
        Debug.Log($"<color=blue>[룩]</color> {primaryTarget.name}에게 {skillData.rookDamage} 데미지");

        // 적이 가장 많은 방향 판별 (좌/우)
        Vector2 dashDir = DetermineDirectionWithMostEnemies(transform.position);

        // 직선 대시
        yield return DashRoutine(dashDir);

        yield return new WaitForSeconds(0.3f);
        ReturnToPool();
    }

    /// <summary>
    /// 좌/우 중 적이 더 많은 방향을 반환한다.
    /// </summary>
    private Vector2 DetermineDirectionWithMostEnemies(Vector3 pos)
    {
        int count = Physics2D.OverlapCircleNonAlloc(pos, skillData.rookDashDistance, scanBuffer, enemyLayer);

        int leftCount = 0;
        int rightCount = 0;

        for (int i = 0; i < count; i++)
        {
            if (scanBuffer[i] == null) continue;
            if (scanBuffer[i].transform.position.x < pos.x)
                leftCount++;
            else
                rightCount++;
        }

        return rightCount >= leftCount ? Vector2.right : Vector2.left;
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        float distanceTraveled = 0f;
        float totalDistance = skillData.rookDashDistance;
        float speed = skillData.rookDashSpeed;

        // 대시 중 이미 히트한 적 추적 (중복 피해 방지)
        List<Collider2D> hitTargets = new List<Collider2D>(16);

        while (distanceTraveled < totalDistance)
        {
            float step = speed * Time.deltaTime;
            transform.position += (Vector3)(direction * step);
            distanceTraveled += step;

            // 대시 경로상 적 히트 판정
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, 1f, dashHitBuffer, enemyLayer);
            for (int i = 0; i < count; i++)
            {
                if (dashHitBuffer[i] == null) continue;
                if (hitTargets.Contains(dashHitBuffer[i])) continue;

                hitTargets.Add(dashHitBuffer[i]);
                GameObject enemy = dashHitBuffer[i].gameObject;

                ApplyDamage(enemy, skillData.rookDashDamage);
                TryApplyStun(enemy, skillData.rookDashStunChance, skillData.rookDashStunDuration);
                ApplyKnockback(enemy, direction, skillData.rookKnockback);
                Debug.Log($"<color=blue>[룩 대시]</color> {enemy.name} 히트");
            }

            yield return null;
        }
    }

    private IEnumerator MoveToTarget(Vector3 destination)
    {
        float speed = skillData.rookDashSpeed;
        while (true)
        {
            float dx = destination.x - transform.position.x;
            float dy = destination.y - transform.position.y;
            if (dx * dx + dy * dy < 0.1f) break;

            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = destination;
    }
}