using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CentralPull : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 targetPoint;
    private LineRenderer line;
    private bool isInitialized = false;
    private bool isFixed = false;

    public void Setup(Vector2 target, GameObject effect)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.mass = 1f;

        targetPoint = target;

        line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();

        line.startWidth = 0.109f;
        line.endWidth = 0.109f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.positionCount = 2;

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || isFixed) return;

        line.SetPosition(0, transform.position);
        line.SetPosition(1, targetPoint);
    }

    void FixedUpdate()
    {
        if (!isInitialized || isFixed) return;

        Vector2 dir = (targetPoint - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * 20f; // 속도를 살짝 높여서 타격감을 더 줬습니다 (기존 15f)
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isFixed) return;

        // 적과 충돌했을 때
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 이미 찌그러져서 충돌체가 꺼진 적이면 무시하고 통과
            if (collision.gameObject.GetComponent<Collider2D>().enabled == false) return;

            CrushEnemy(collision.gameObject);
            return; // 적을 뚫고 계속 중앙으로 날아가도록 return
        }

        // 중앙에서 반대편 물체와 만났을 때 고정
        if (collision.gameObject.GetComponent<CentralPull>() != null)
        {
            FixPosition();
        }
    }

    // [핵심 기능] 적을 압사시키는 연출
    void CrushEnemy(GameObject target)
    {
        // 1. 물체들이 멈추지 않고 통과할 수 있게 적의 충돌체 무력화
        Collider2D col = target.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 2. 적이 밀려나지 않게 고정
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            targetRb.linearVelocity = Vector2.zero;
            targetRb.bodyType = RigidbodyType2D.Static;
        }

        // 3. 날아가는 방향을 기반으로 적을 납작하게 찌그러뜨림
        Vector3 scale = target.transform.localScale;
        Vector2 dir = rb.linearVelocity.normalized;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            // 좌우로 날아와서 부딪힌 경우 (X축 축소, Y축 팽창)
            target.transform.localScale = new Vector3(scale.x * 0.2f, scale.y * 1.5f, scale.z);
        }
        else
        {
            // 위아래로 날아와서 부딪힌 경우 (Y축 축소, X축 팽창)
            target.transform.localScale = new Vector3(scale.x * 1.5f, scale.y * 0.2f, scale.z);
        }

        // 4. 찌그러진 잔해를 0.5초 동안 보여준 뒤 완전 삭제
        Destroy(target, 0.5f);
    }

    void FixPosition()
    {
        isFixed = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        if (line != null) Destroy(line);
        Destroy(this, 0.1f);
    }
}