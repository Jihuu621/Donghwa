using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] // 리지드바디가 없으면 자동으로 추가
public class CentralPull : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 targetPoint;
    private LineRenderer line;
    private bool isInitialized = false;
    private bool isFixed = false;

    public void Setup(Vector2 target, GameObject effect)
    {
        // 1. Rigidbody 설정
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.mass = 1f; // 질량 통일

        targetPoint = target;

        // 2. LineRenderer 에러 방지 (없으면 생성)
        line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();

        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.positionCount = 2;

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || isFixed) return;

        // 실 위치 갱신
        line.SetPosition(0, transform.position);
        line.SetPosition(1, targetPoint);
    }

    void FixedUpdate()
    {
        if (!isInitialized || isFixed) return;

        // 중앙으로 당기는 속도 조절
        Vector2 dir = (targetPoint - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * 15f;
    }

    // [중요] 실제 충돌 시 호출되는 함수
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isFixed) return;

        // 상대방도 끌려오는 중인 물체라면 (또는 특정 태그를 확인해도 됨)
        if (collision.gameObject.GetComponent<CentralPull>() != null)
        {
            FixPosition();
        }
    }

    void FixPosition()
    {
        isFixed = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; // 완전히 고정시켜서 발판으로 만듦

        // 시각적 실 제거
        if (line != null) Destroy(line);

        // 스크립트 기능 종료
        Destroy(this, 0.1f);
    }
}