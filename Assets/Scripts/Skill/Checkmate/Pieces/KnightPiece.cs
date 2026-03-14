using System.Collections;
using UnityEngine;

/// <summary>
/// 나이트: 가장 가까운 적에게 이동 → 데미지 + 스턴 → 주변 적에게 점프 연쇄 공격.
/// </summary>
public class KnightPiece : ChessPiece
{
    private static Collider2D[] jumpBuffer = new Collider2D[16];

    public override void Execute(Transform target, Vector3 playerPosition, float facingDir = 1f)
    {
        StartCoroutine(KnightRoutine(target));
    }

    private IEnumerator KnightRoutine(Transform primaryTarget)
    {
        if (primaryTarget == null)
        {
            ReturnToPool();
            yield break;
        }

        // 1차 타겟으로 이동
        yield return MoveToTarget(primaryTarget.position);

        // 1차 데미지 + 스턴
        ApplyDamage(primaryTarget.gameObject, skillData.knightDamage);
        TryApplyStun(primaryTarget.gameObject, skillData.knightStunChance, skillData.knightStunDuration);
        Debug.Log($"<color=green>[나이트]</color> {primaryTarget.name}에게 {skillData.knightDamage} 데미지");

        // 연쇄 점프 공격
        Transform lastTarget = primaryTarget;
        int jumpsRemaining = skillData.knightMaxJumps;

        while (jumpsRemaining > 0)
        {
            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, skillData.knightJumpRadius, jumpBuffer, enemyLayer);

            Transform nextTarget = null;
            float closestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (jumpBuffer[i] == null) continue;
                Transform t = jumpBuffer[i].transform;
                if (t == lastTarget) continue;

                float dx = t.position.x - transform.position.x;
                float dy = t.position.y - transform.position.y;
                float sqr = dx * dx + dy * dy;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    nextTarget = t;
                }
            }

            if (nextTarget == null) break;

            yield return MoveToTarget(nextTarget.position);
            ApplyDamage(nextTarget.gameObject, skillData.knightDamage * 0.7f);
            TryApplyStun(nextTarget.gameObject, skillData.knightStunChance, skillData.knightStunDuration);
            Debug.Log($"<color=green>[나이트 점프]</color> {nextTarget.name}에게 연쇄 공격");

            lastTarget = nextTarget;
            jumpsRemaining--;
        }

        yield return new WaitForSeconds(0.2f);
        ReturnToPool();
    }

    private IEnumerator MoveToTarget(Vector3 destination)
    {
        float speed = skillData.knightMoveSpeed;
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