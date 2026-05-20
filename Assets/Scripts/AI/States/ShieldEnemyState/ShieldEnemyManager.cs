using UnityEngine;

public class ShieldEnemyManager : MonoBehaviour, IDamageable
{
    public Health EnemyHealth;
    public ShieldController Shield;

    public float MoveSpeed = 2.5f;
    public bool IsShieldBroken => Shield == null || Shield.IsBroken;

    void Awake()
    {
        EnemyHealth = GetComponent<Health>();
        Shield = GetComponentInChildren<ShieldController>();
    }

    // IDamageable 인터페이스 구현부
    public void TakeDamage(float damage) => TakeDamage(damage, null);

    public void TakeDamage(float damage, GameObject source)
    {
        if (!IsShieldBroken)
        {
            Shield.TakeShieldDamage(Mathf.RoundToInt(damage));
            Debug.Log($"[쉴드 에너미] 방패로 피해 차단! 방패 피해: {damage}");
            return;
        }

        EnemyHealth.ReduceHP(damage);
        Debug.Log($"[쉴드 에너미] 본체 체력 피해: {damage}");
    }
}