using System;
using UnityEngine;

[RequireComponent(typeof(CheshireCatAI), typeof(EnemyDataManager))]
public class CheshireCatHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1f)] private float maxHP = 400f;

    public float CurrentHP { get; private set; }
    public float MaxHP => maxHP;
    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged;

    private CheshireCatAI _ai;
    private bool _isDead;

    private void Awake()
    {
        _ai = GetComponent<CheshireCatAI>();

        if (TryGetComponent(out EnemyDataManager dataManager) &&
            dataManager.EnemyData != null &&
            dataManager.EnemyData.MaxHP > 0f)
        {
            maxHP = dataManager.EnemyData.MaxHP;
        }

        CurrentHP = maxHP;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, GameObject source)
    {
        if (_isDead || damage <= 0f || (_ai != null && _ai.IsSmokeForm)) return;

        CurrentHP = Mathf.Max(0f, CurrentHP - damage);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
        if (CurrentHP > 0f) return;

        _isDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        maxHP = Mathf.Max(1f, maxHP);
    }
}
