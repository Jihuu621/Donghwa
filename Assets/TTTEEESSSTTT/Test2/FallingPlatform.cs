using UnityEngine;
public class FallingPlatform : MonoBehaviour
{
    [Header("데미지 설정")]
    [SerializeField] private float damageAmount = 50f;
    [SerializeField] private float minFallSpeed = 0.8f;       // 최소 이동 속력 기준
    [SerializeField] private float minRotationSpeed = 100f;  // 최소 회전 속도 기준

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
                Health health = collision.gameObject.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damageAmount);
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