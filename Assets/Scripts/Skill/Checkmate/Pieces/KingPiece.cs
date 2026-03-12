using System.Collections;
using UnityEngine;

/// <summary>
/// 킹: 플레이어 오른쪽에서 스폰 → 전방으로 이동하며 데미지.
/// </summary>
public class KingPiece : ChessPiece
{
    private static Collider2D[] hitBuffer = new Collider2D[16];

    public override void Execute(Transform target, Vector3 playerPosition)
    {
        StartCoroutine(KingRoutine(playerPosition));
    }

    private IEnumerator KingRoutine(Vector3 playerPos)
    {
        // 플레이어 오른쪽에 스폰
        Vector3 spawnPos = playerPos + Vector3.right * skillData.kingSpawnOffset;
        transform.position = spawnPos;

        // 전방(오른쪽)으로 이동
        Vector3 moveDir = Vector3.right;
        float distanceTraveled = 0f;
        float totalDistance = skillData.kingMoveDistance;
        float speed = skillData.kingMoveSpeed;

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
                ApplyDamage(enemy, skillData.kingDamage);
                Debug.Log($"<color=gold>[킹]</color> {enemy.name}에게 {skillData.kingDamage} 데미지");
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        ReturnToPool();
    }
}