using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class EnemyFSM : MonoBehaviour
{
    public EnemyState CurrentState { get; private set; }

    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer Sr { get; private set; }
    public Animator Anim { get; private set; }
    public Transform Player { get; private set; }
    public EnemyData Data { get; private set; }

    public EnemyState Idle { get; set; }
    public EnemyState Patrol { get; set; }
    public EnemyState Chase { get; set; }
    public EnemyState Attack { get; set; }
    public EnemyState Stun { get; set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Sr = GetComponent<SpriteRenderer>();
        Anim = GetComponent<Animator>();
        
        if (TryGetComponent<EnemyDataManager>(out var dataManager))
        {
            Data = dataManager.EnemyData;
        }
        
        Player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        CurrentState?.Execute();
    }

    public void Initialize(EnemyState startState)
    {
        TransitionTo(startState);
    }

    public void TransitionTo(EnemyState newState)
    {
        if (newState == null) return;
        
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void StopMovement()
    {
        if (Rb != null)
        {
            Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
        }
    }

    public void PerformAttack(float range)
    {
        if (Player == null) return;

        float dist = Vector2.Distance(transform.position, Player.position);
        if (dist > range) return;

        IDamageable target = Player.GetComponent<IDamageable>();
        if (target != null)
        {
            float damage = Data != null ? Data.Damage : 10f;
            target.TakeDamage(damage, gameObject);
        }
    }
}