using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [Header("데미지 설정")]
    [SerializeField] private float damageAmount = 50f;
    [SerializeField] private float minFallSpeed = 0.8f;
    [SerializeField] private float minRotationSpeed = 100f;

    private Rigidbody2D rb;
    private bool isFunctional = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFunctional) return;

        float currentSpeed = rb.linearVelocity.magnitude;
        float currentRotationSpeed = Mathf.Abs(rb.angularVelocity);

        if (currentSpeed > minFallSpeed || currentRotationSpeed > minRotationSpeed)
        {
            if (collision.gameObject.CompareTag("Enemy"))
            {
                IDamageable damageable = collision.gameObject.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damageAmount, gameObject);
                    Debug.Log($"{collision.gameObject.name} 처치!");
                }
                DisablePlatformFunction();
            }
            else if (collision.gameObject.CompareTag("Ground"))
            {
                DisablePlatformFunction();
            }
        }
    }

    private void DisablePlatformFunction()
    {
        isFunctional = false;
        this.enabled = false;
        Debug.Log("플랫폼 공격 기능이 종료되었습니다.");
    }
}