using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHP = 100f;

    public float CurrentHP { get; private set; }
    public float MaxHP => maxHP;
    public bool IsInvincible => isInvincible;

    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged;

    private bool isDead = false;
    private bool isPlayer;
    private bool isInvincible;

    private void Awake()
    {
        isPlayer = CompareTag("Player");
        CurrentHP = maxHP;
    }

    private void Update()
    {
        if (!isPlayer || !Input.GetKeyDown(KeyCode.P)) return;

        isInvincible = !isInvincible;
        Debug.Log(isInvincible
            ? "<color=yellow>[플레이어 무적 모드 ON]</color>"
            : "<color=white>[플레이어 무적 모드 OFF]</color>");
    }

    public void Init(float newMaxHP)
    {
        maxHP = newMaxHP;
        CurrentHP = maxHP;
        isDead = false;
        isInvincible = false;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }
    public void TakeDamage(float damage) => TakeDamage(damage, null);
    public void TakeDamage(float damage, GameObject source)
    {
        if (isDead || (isPlayer && isInvincible)) return;

        if (CompareTag("Player"))
        {
            PlayerParry parry = GetComponent<PlayerParry>();
            if (parry != null)
            {
                damage = parry.OnHit(damage);

                if (damage <= 0)
                {
                    Debug.Log("<color=green>[패링 성공!]</color> 공격이 무효화되었습니다.");
                    return;
                }
            }
        }

        ReduceHP(damage);
    }

    public void ReduceHP(float damage)
    {
        if (isDead || (isPlayer && isInvincible)) return;

        CurrentHP -= damage;
        CurrentHP = Mathf.Max(CurrentHP, 0f);

        Debug.Log($"<color=orange>[Health]</color> {gameObject.name}의 남은 체력: {CurrentHP}");

        OnHealthChanged?.Invoke(CurrentHP, maxHP);

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
        if (CompareTag("Player"))
        {
            Debug.Log("유다히 (플레이어 사망)");
        }
        else if (CompareTag("Enemy"))
        {
            Debug.Log($"{gameObject.name} 적 사망, 오브젝트 파괴");
            Destroy(gameObject);
        }
    }
}
