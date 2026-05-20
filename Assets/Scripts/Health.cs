using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHP = 100f;

    public float CurrentHP { get; private set; }
    public float MaxHP => maxHP;

    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged;

    private bool isDead = false;

    private void Awake()
    {
        CurrentHP = maxHP;
    }

    public void Init(float newMaxHP)
    {
        maxHP = newMaxHP;
        CurrentHP = maxHP;
        isDead = false;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    // IDamageable 인터페이스 구현부 (공격자가 누구인지 모를 때)
    public void TakeDamage(float damage) => TakeDamage(damage, null);

    // IDamageable 인터페이스 구현부 (적이 직접 공격했을 때)
    public void TakeDamage(float damage, GameObject source)
    {
        if (isDead) return;

        // 1. 내가 플레이어라면? -> 데미지를 받기 전에 패링 먼저 계산
        if (CompareTag("Player"))
        {
            PlayerParry parry = GetComponent<PlayerParry>();
            if (parry != null)
            {
                damage = parry.OnHit(damage);

                // 패링에 성공해서 데미지가 0 이하가 되면 여기서 방어 종료
                if (damage <= 0)
                {
                    Debug.Log("<color=green>[패링 성공!]</color> 공격이 무효화되었습니다.");
                    return;
                }
            }
        }

        // 패링을 못 했거나 적이라면 그대로 체력 감소 실행
        ReduceHP(damage);
    }

    public void ReduceHP(float damage)
    {
        if (isDead) return;

        CurrentHP -= damage;
        CurrentHP = Mathf.Max(CurrentHP, 0f);

        Debug.Log($"<color=orange>[Health]</color> {gameObject.name}의 남은 체력: {CurrentHP}");

        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        // 체력이 0이 되면 사망 처리
        if (CurrentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        CurrentHP = Mathf.Min(CurrentHP + amount, maxHP);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();

        // 2. 죽었을 때 원래 있던 로직 복구
        if (CompareTag("Player"))
        {
            Debug.Log("유다히 (플레이어 사망)");
        }
        else if (CompareTag("Enemy"))
        {
            Debug.Log($"{gameObject.name} 적 사망, 오브젝트 파괴");
            Destroy(gameObject); // 체력이 0이 된 적을 화면에서 파괴합니다.
        }
    }
}