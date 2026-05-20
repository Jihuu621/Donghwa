using UnityEngine;

/// <summary>
/// 모든 체스 기물의 베이스 클래스.
/// 풀에서 활성화되면 Execute()가 호출되며, 작업 완료 시 스스로 풀에 반환한다.
/// </summary>
public abstract class ChessPiece : MonoBehaviour
{
    protected ObjectPoolManager poolManager;
    protected CheckmateSkillData skillData;
    protected LayerMask enemyLayer;

    /// <summary>
    /// 풀에서 꺼낸 뒤 초기 참조를 주입한다.
    /// </summary>
    public void Initialize(ObjectPoolManager pool, CheckmateSkillData data, LayerMask layer)
    {
        poolManager = pool;
        skillData = data;
        enemyLayer = layer;
    }

    /// <summary>
    /// 기물의 공격 동작을 시작한다.
    /// facingDir: 발동 시점 플레이어 방향 (1 = 오른쪽, -1 = 왼쪽)
    /// </summary>
    public abstract void Execute(Transform target, Vector3 playerPosition, float facingDir = 1f);

    /// <summary>
    /// 대상에게 데미지를 적용한다. (IDamageable 연동)
    /// </summary>
    protected void ApplyDamage(GameObject target, float damage)
    {
        if (target == null) return;

        float ampMult = 1f;
        var amp = target.GetComponent<EnemyDamageAmpData>();
        if (amp != null) ampMult = amp.Multiplier;

        float finalDamage = damage * ampMult;

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(finalDamage, gameObject);
        }
    }

    /// <summary>
    /// 확률적으로 스턴을 적용한다.
    /// </summary>
    protected void TryApplyStun(GameObject target, float chance, float duration)
    {
        if (target == null || chance <= 0f) return;
        if (Random.value > chance) return;

        var runner = target.GetComponent<EnemyStatusEffectRunner>();
        if (runner == null) runner = target.AddComponent<EnemyStatusEffectRunner>();
        runner.AddEffect(new StunEffect(duration));
    }

    /// <summary>
    /// 넉백을 적용한다.
    /// </summary>
    protected void ApplyKnockback(GameObject target, Vector2 direction, float force)
    {
        var rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// 작업 완료 후 풀에 자신을 반환한다.
    /// </summary>
    protected void ReturnToPool()
    {
        if (poolManager != null)
        {
            poolManager.Return(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}