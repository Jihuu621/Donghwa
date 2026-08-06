using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class EnemyFSM : MonoBehaviour
{
    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer Sr { get; private set; }
    public Animator Anim { get; private set; }
    public Transform Player { get; private set; }
    public EnemyData Data { get; private set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Sr = GetComponent<SpriteRenderer>();
        Anim = GetComponent<Animator>();
        
        if (TryGetComponent<EnemyDataManager>(out var dataManager))
        {
            Data = dataManager.EnemyData;
        }
        
        TryAcquirePlayer();
    }

    private void Update()
    {
        if (Player == null)
        {
            TryAcquirePlayer();
        }
    }

    public void StopMovement()
    {
        if (Rb != null)
        {
            Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
        }
    }

    public void StopAllMovement()
    {
        if (Rb != null)
        {
            Rb.linearVelocity = Vector2.zero;
        }
    }

    public bool TryAcquirePlayer()
    {
        Player = GameObject.FindGameObjectWithTag("Player")?.transform;
        return Player != null;
    }

    public void SetPlayerTarget(Transform player)
    {
        Player = player;
    }

    public void PerformAttack(float range)
    {
        if (Player == null && !TryAcquirePlayer()) return;

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
