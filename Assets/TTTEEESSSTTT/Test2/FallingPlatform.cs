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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 빠른 낙하물이 Trigger 콜라이더를 통과하는 것을 줄인다.
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        if (TryDamageTarget(collision.collider)) return;

        if (CanDealImpactDamage() && IsGround(collision.collider))
        {
            DisablePlatformFunction();
        }
    }

    // CheshireCat의 본체 콜라이더는 Trigger이므로 일반 충돌 콜백만으로는 피해를 줄 수 없다.
    private void OnTriggerEnter2D(Collider2D other)
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
