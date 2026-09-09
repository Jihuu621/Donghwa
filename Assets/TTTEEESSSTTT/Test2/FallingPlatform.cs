using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [Header("데미지 설정")]
    [SerializeField] private float damageAmount = 50f;
    [SerializeField, Range(0.01f, 1f)] private float bossMaxHealthDamageRatio = 0.2f;
    [SerializeField] private float minFallSpeed = 0.8f;
    [SerializeField] private float minRotationSpeed = 100f;

    private Rigidbody2D rb;
    private bool isFunctional = true;
    private readonly RaycastHit2D[] sweepHits = new RaycastHit2D[8];
    private ContactFilter2D sweepFilter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 빠른 낙하물이 Trigger 콜라이더를 통과하는 것을 줄인다.
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        sweepFilter = ContactFilter2D.noFilter;
        sweepFilter.useTriggers = true;
    }

    private void FixedUpdate()
    {
        if (!CanDealImpactDamage()) return;

        Vector2 velocity = rb.linearVelocity;
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        if (distance <= 0f) return;

        int hitCount = rb.Cast(velocity / velocity.magnitude, sweepFilter, sweepHits, distance);
        for (int i = 0; i < hitCount; i++)
        {
            if (TryDamageTarget(sweepHits[i].collider)) return;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Collision2D.collider는 이 물체의 콜라이더다. 대상은 otherCollider로 읽어야 한다.
        if (collision == null || collision.otherCollider == null) return;
        if (TryDamageTarget(collision.otherCollider)) return;

        if (CanDealImpactDamage() && IsGround(collision.otherCollider))
        {
            DisablePlatformFunction();
        }
    }

    // CheshireCat의 본체 콜라이더는 Trigger이므로 일반 충돌 콜백만으로는 피해를 줄 수 없다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamageTarget(other);
    }

    // 잘린 직후 보스와 이미 겹친 경우에는 Enter 이벤트가 새로 오지 않을 수 있다.
    // 낙하 속도가 생긴 다음 물리 스텝에서도 한 번만 피해를 판정한다.
    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamageTarget(other);
    }

    private bool TryDamageTarget(Collider2D targetCollider)
    {
        if (targetCollider == null || !CanDealImpactDamage()) return false;

        CheshireCatHealth cheshireCat = targetCollider.GetComponentInParent<CheshireCatHealth>();
        if (cheshireCat != null)
        {
            float bossDamage = cheshireCat.MaxHP * bossMaxHealthDamageRatio;
            cheshireCat.TakeDamage(bossDamage, gameObject);
            Debug.Log($"{cheshireCat.gameObject.name}에게 낙하물 피해 {bossDamage:0.#}!");
            DisablePlatformFunction();
            return true;
        }

        Transform targetRoot = targetCollider.transform.root;
        if (targetRoot == null || !targetRoot.CompareTag("Enemy")) return false;

        IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();
        if (damageable == null) return false;

        damageable.TakeDamage(damageAmount, gameObject);
        Debug.Log($"{targetRoot.name}에게 낙하물 피해 {damageAmount:0.#}!");
        DisablePlatformFunction();
        return true;
    }

    private bool CanDealImpactDamage()
    {
        if (!isFunctional || rb == null) return false;

        return rb.linearVelocity.magnitude > minFallSpeed ||
               Mathf.Abs(rb.angularVelocity) > minRotationSpeed;
    }

    private static bool IsGround(Collider2D targetCollider)
    {
        return targetCollider.CompareTag("Ground") ||
               targetCollider.transform.root.CompareTag("Ground");
    }

    private void DisablePlatformFunction()
    {
        isFunctional = false;
        this.enabled = false;
        Debug.Log("플랫폼 공격 기능이 종료되었습니다.");
    }

    private void OnValidate()
    {
        damageAmount = Mathf.Max(0f, damageAmount);
        bossMaxHealthDamageRatio = Mathf.Clamp(bossMaxHealthDamageRatio, 0.01f, 1f);
        minFallSpeed = Mathf.Max(0f, minFallSpeed);
        minRotationSpeed = Mathf.Max(0f, minRotationSpeed);
    }
}
