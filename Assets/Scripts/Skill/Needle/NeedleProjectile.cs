using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NeedleProjectile : MonoBehaviour
{
    public enum NeedleState { Flying, StuckInEnemy, StuckInGround, UsedInTrap }
    public NeedleState currentState { get; private set; }

    [Header("비행 물리 설정")]
    [Tooltip("최종적으로 도달할 최대 중력값")]
    public float maxFlyingGravity = 2.5f;
    [Tooltip("중력이 증가하는 속도 (높을수록 빨리 떨어지기 시작함)")]
    public float gravityIncreaseRate = 4f;

    private float currentGravity;
    private float damage;
    private float stunDuration;
    private float stunValue;
    private float knockbackForce;
    private GameObject playerSource;

    private Rigidbody2D rb;
    private Collider2D col;
    private Coroutine despawnCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void Launch(Vector2 direction, float speed, float dmg, float stunDur, float stunVal, float kb, GameObject source)
    {
        currentState = NeedleState.Flying;
        damage = dmg;
        stunDuration = stunDur;
        stunValue = stunVal;
        knockbackForce = kb;
        playerSource = source;

        col.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;

        // 발사 직후에는 중력을 0으로 두어 완벽한 직선 비행을 유도합니다.
        currentGravity = 0f;
        rb.gravityScale = 0f;

        rb.linearVelocity = direction * speed;
        transform.up = direction;

        if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
        despawnCoroutine = StartCoroutine(DespawnTimer(3f));
    }

    private void Update()
    {
        if (currentState == NeedleState.Flying)
        {
            // 중력을 서서히 증가시켜 자연스러운 궤적 꺾임을 만듭니다.
            if (currentGravity < maxFlyingGravity)
            {
                currentGravity += Time.deltaTime * gravityIncreaseRate;
                rb.gravityScale = Mathf.Min(currentGravity, maxFlyingGravity);
            }

            // 날아가는 방향을 바라보도록 회전
            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                transform.up = rb.linearVelocity.normalized;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (currentState != NeedleState.Flying) return;
        if (collision.gameObject == playerSource) return;

        if (collision.CompareTag("Enemy"))
        {
            HitEnemy(collision.gameObject);
        }
        else if (collision.CompareTag("Ground"))
        {
            HitGround();
        }
    }

    private void HitEnemy(GameObject enemyObj)
    {
        currentState = NeedleState.StuckInEnemy;
        StopMovement();

        IDamageable damageable = enemyObj.GetComponentInParent<IDamageable>();
        if (damageable != null) damageable.TakeDamage(damage, playerSource);

        Rigidbody2D enemyRb = enemyObj.GetComponentInParent<Rigidbody2D>();
        if (enemyRb != null)
        {
            enemyRb.linearVelocity = Vector2.zero;
            enemyRb.AddForce(transform.up * knockbackForce, ForceMode2D.Impulse);
        }

        EffectManager effectManager = enemyObj.GetComponentInParent<EffectManager>();
        if (effectManager != null)
        {
            effectManager.ApplyStatus(StatusKeyword.Stun, stunDuration, stunValue);
        }

        EnemyFSM fsm = enemyObj.GetComponentInParent<EnemyFSM>();
        if (fsm != null && fsm.CurrentState == fsm.Attack)
        {
            Debug.Log("<color=orange>[바늘] 적 공격 취소!</color>");
            fsm.TransitionTo(fsm.Idle);
        }

        transform.SetParent(enemyObj.transform);
        NeedleSkillManager.Instance.RegisterNeedle(this);
        ResetDespawnTimer(5f);
    }

    private void HitGround()
    {
        currentState = NeedleState.StuckInGround;
        StopMovement();

        transform.SetParent(null);
        NeedleSkillManager.Instance.RegisterNeedle(this);
        ResetDespawnTimer(10f);
    }

    private void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        col.enabled = false;
    }

    public void SetAsTrapNode()
    {
        currentState = NeedleState.UsedInTrap;
        if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
    }

    private void ResetDespawnTimer(float time)
    {
        if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
        despawnCoroutine = StartCoroutine(DespawnTimer(time));
    }

    private IEnumerator DespawnTimer(float time)
    {
        yield return new WaitForSeconds(time);
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        NeedleSkillManager.Instance.UnregisterNeedle(this);
        NeedleSkillManager.Instance.needlePool.ReturnToPool(gameObject);
    }

    private void OnDisable()
    {
        NeedleSkillManager.Instance?.UnregisterNeedle(this);
    }
}