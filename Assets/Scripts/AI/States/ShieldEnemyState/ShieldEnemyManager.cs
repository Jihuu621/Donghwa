using UnityEngine;

public class ShieldEnemyManager : MonoBehaviour
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

    public void TakeDamage(int damage)
    {
        if (!IsShieldBroken)
        {
            Debug.Log("[쉴드 에너미] 방패로 피해 차단");
            return;
        }

        EnemyHealth.TakeDamage(damage);
        Debug.Log($"[쉴드 에너미] 체력 피해 {damage}");
    }
}
