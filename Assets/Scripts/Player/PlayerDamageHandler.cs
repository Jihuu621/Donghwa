using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDamageHandler : MonoBehaviour, IDamageable
{
    private Health _health;
    private PlayerParry _parry;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _parry = GetComponent<PlayerParry>();
    }

    // IDamageable 구현 1: 공격자가 누군지 모를 때 (낙사, 독 장판 등)
    public void TakeDamage(float damage)
    {
        ProcessDamage(damage);
    }

    // IDamageable 구현 2: 적이 직접 타격했을 때 (넉백 위치 등을 계산 가능)
    public void TakeDamage(float damage, GameObject source)
    {
        // 필요시 여기서 source.transform.position 등을 이용해 넉백을 구현할 수 있습니다.
        ProcessDamage(damage);
    }

    // 공통 데미지 처리 및 패링 로직
    private void ProcessDamage(float amount)
    {
        float finalDamage = amount;

        if (_parry != null)
        {
            finalDamage = _parry.OnHit(amount);

            if (finalDamage <= 0)
            {
                Debug.Log("<color=green>[패링 성공!]</color> 데미지 무효화");
                return;
            }
        }

        if (finalDamage > 0f)
        {
            _health.ReduceHP(finalDamage);
        }
    }
}