using System.Collections;
using UnityEngine;

/// <summary>
/// 폰: 적 머리 위에서 낙하하여 데미지를 준다.
/// 낙하 중 적의 X 위치를 추적하여 빗나가지 않도록 한다.
/// </summary>
public class PawnPiece : ChessPiece
{
    private Transform target;
    private bool hasHit;

    public override void Execute(Transform target, Vector3 playerPosition, float facingDir = 1f)
    {
        this.target = target;
        hasHit = false;
        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        if (target == null)
        {
            ReturnToPool();
            yield break;
        }

        // 적 머리 위에 스폰
        Vector3 spawnPos = target.position + Vector3.up * skillData.pawnSpawnHeight;
        transform.position = spawnPos;

        float speed = skillData.pawnFallSpeed;

        while (!hasHit)
        {
            if (target == null)
            {
                ReturnToPool();
                yield break;
            }

            // 적의 X 위치를 실시간 추적하면서 아래로 낙하
            Vector3 pos = transform.position;
            pos.x = target.position.x;
            pos.y -= speed * Time.deltaTime;
            transform.position = pos;

            // 적과의 Y 거리가 충분히 가까우면 히트 판정
            float yDiff = transform.position.y - target.position.y;
            if (yDiff <= 0.3f)
            {
                hasHit = true;
                ApplyDamage(target.gameObject, skillData.pawnDamage);
                Debug.Log($"<color=white>[폰]</color> {target.name}에게 {skillData.pawnDamage} 데미지");
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.2f);
        ReturnToPool();
    }
}