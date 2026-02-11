using UnityEngine;

public class EnemyStateManager : MonoBehaviour
{
    public IEnemyState CurrentState;

    [Header("State Behaviors")]
    [SerializeField] MonoBehaviour _idleBehavior;
    [SerializeField] MonoBehaviour _patrolBehavior;
    [SerializeField] MonoBehaviour _chaseBehavior;
    [SerializeField] MonoBehaviour _attackBehavior;

    public IEnemyIdleBehavior IdleBehavior => _idleBehavior as IEnemyIdleBehavior;
    public IEnemyPatrolBehavior PatrolBehavior => _patrolBehavior as IEnemyPatrolBehavior;
    public IEnemyChaseBehavior ChaseBehavior => _chaseBehavior as IEnemyChaseBehavior;
    public IEnemyAttackBehavior AttackBehavior => _attackBehavior as IEnemyAttackBehavior;

    private void Awake()
    {
        CacheBehaviors();
    }

    private void Start()
    {
        TransitionToState(new IdleState());
    }

    private void Update()
    {
        CurrentState?.UpdateState(this);
    }

    public void TransitionToState(IEnemyState newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);
        // Debug.Log($"[TransitionToState] {newState}로 스테이츠 변경");
    }

    public void PerformAttack(float range)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        if (dist > range) return;

        Health playerHealth = player.GetComponent<Health>();
        PlayerParry parry = player.GetComponent<PlayerParry>();

        float damage = 10f;
        if (TryGetComponent<EnemyDataManager>(out var data))
        {
            damage = data.EnemyData.Damage;
        }

        if (parry != null)
        {
            float finalDamage = parry.OnHit(damage);

            if (finalDamage <= 0)
            {
                Debug.Log("<color=green>[패링 성공]</color> 공격이 무효화되었습니다.");
            }
            else
            {
                playerHealth?.TakeDamage(finalDamage);
            }
        }
        else
        {
            playerHealth?.TakeDamage(damage);
        }
    }

    private void CacheBehaviors()
    {
        if (_idleBehavior != null && _patrolBehavior != null && _chaseBehavior != null && _attackBehavior != null)
        {
            return;
        }

        var components = GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (_idleBehavior == null && component is IEnemyIdleBehavior)
            {
                _idleBehavior = component;
            }

            if (_patrolBehavior == null && component is IEnemyPatrolBehavior)
            {
                _patrolBehavior = component;
            }

            if (_chaseBehavior == null && component is IEnemyChaseBehavior)
            {
                _chaseBehavior = component;
            }

            if (_attackBehavior == null && component is IEnemyAttackBehavior)
            {
                _attackBehavior = component;
            }
        }
    }
}