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

        // 결합도가 낮아진 코드: 플레이어의 IDamageable만 찾아서 데미지를 던집니다.
        IDamageable target = player.GetComponent<IDamageable>();
        if (target != null)
        {
            float damage = 10f;
            if (TryGetComponent<EnemyDataManager>(out var data))
            {
                damage = data.EnemyData.Damage;
            }

            target.TakeDamage(damage, gameObject);
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