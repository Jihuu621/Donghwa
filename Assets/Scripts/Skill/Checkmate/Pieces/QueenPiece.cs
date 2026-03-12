using System.Collections;
using UnityEngine;

/// <summary>
/// 퀸: 플레이어 왼쪽에서 스폰 → 전방으로 이동하며 데미지 + 스턴.
/// </summary>
public class QueenPiece : ChessPiece
{
    private static Collider2D[] hitBuffer = new Collider2D[16];

    public override void Execute(Transform target, Vector3 playerPosition)
    {
        StartCoroutine(QueenRoutine(playerPosition));
    }

    private IEnumerator QueenRoutine(Vector3 playerPos)
    {
        // 플레이어 왼쪽에 스폰
        Vector3 spawnPos = playerPos + Vector3.left * skillData.queenSpawnOffset;
        transform.position = spawnPos;

        // 전방(왼쪽)으로 이동
        Vector3 moveDir = Vector3.left;
        float distanceTraveled = 0f;
        float totalDistance = skillData.queenMoveDistance;
        float speed = skillData.queenMoveSpeed;

        // 히트한 적 중복 방지
        System.Collections.Generic.List<Collider2D> hitTargets =
            new System.Collections.Generic.List<Collider2D>(16);

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
                ApplyDamage(enemy, skillData.queenDamage);
                TryApplyStun(enemy, skillData.queenStunChance, skillData.queenStunDuration);
                Debug.Log($"<color=magenta>[퀸]</color> {enemy.name}에게 {skillData.queenDamage} 데미지");
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        ReturnToPool();
    }
}