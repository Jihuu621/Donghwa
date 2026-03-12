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
    /// </summary>
    public abstract void Execute(Transform target, Vector3 playerPosition);

    /// <summary>
    /// 대상에게 데미지를 적용한다. 기존 Health/ShieldEnemyManager 시스템과 호환.
    /// </summary>
    protected void ApplyDamage(GameObject target, float damage)
    {
        if (target == null) return;

        // 데미지 증폭 처리
        float ampMult = 1f;
        var amp = target.GetComponent<EnemyDamageAmpData>();
        if (amp != null) ampMult = amp.Multiplier;

        int finalDamage = Mathf.RoundToInt(damage * ampMult);

        // 방패 적 처리
        var shield = target.GetComponentInChildren<ShieldController>();
        if (shield != null && !shield.IsBroken)
        {
            shield.TakeShieldDamage(finalDamage);
            return;
        }

        var shieldEnemy = target.GetComponentInParent<ShieldEnemyManager>();
        if (shieldEnemy != null)
        {
            shieldEnemy.TakeDamage(finalDamage);
        }

        var health = target.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(finalDamage);
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