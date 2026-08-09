using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CentralPull : MonoBehaviour
{
    private Rigidbody2D rb;
    private Transform partnerTransform;
    private LineRenderer line;
    private bool isInitialized = false;
    private bool isFixed = false;

    public void Setup(Transform partner, GameObject effect)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.mass = 1f;
        rb.freezeRotation = true;

        partnerTransform = partner;

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
        if (!isInitialized || isFixed || partnerTransform == null) return;

        line.SetPosition(0, transform.position);
        line.SetPosition(1, partnerTransform.position);
    }

    void FixedUpdate()
    {
        if (!isInitialized || isFixed || partnerTransform == null) return;

        Vector2 dir = ((Vector2)partnerTransform.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * 20f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isFixed || partnerTransform == null) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (collision.gameObject.GetComponent<Collider2D>()?.enabled == false) return;
            CrushEnemy(collision.gameObject);
            return;
        }

        // 상대방 물체와 부딪혔을 때 합체 실행
        if (collision.gameObject == partnerTransform.gameObject)
        {
            if (gameObject.GetInstanceID() < partnerTransform.gameObject.GetInstanceID())
            {
                MergeObjects(gameObject, partnerTransform.gameObject);
            }
        }
    }

    void MergeObjects(GameObject a, GameObject b)
    {
        // 1. C 오브젝트 생성
        GameObject c = new GameObject("MergedBlock_C");
        c.transform.position = (a.transform.position + b.transform.position) * 0.5f;
        c.layer = a.layer;

        // 2. 잔여 컴포넌트 정리
        CleanUpResidualComponents(a);
        CleanUpResidualComponents(b);

        // 3. 자식 Rigidbody 물리 연산을 즉시 중단시킨 후 삭제 (자식 끼리 튕겨나가는 현상 방지)
        Rigidbody2D aRb = a.GetComponent<Rigidbody2D>();
        Rigidbody2D bRb = b.GetComponent<Rigidbody2D>();

        if (aRb != null) { aRb.simulated = false; Destroy(aRb); }
        if (bRb != null) { bRb.simulated = false; Destroy(bRb); }

        // 4. C의 자식으로 배치 (월드 트랜스폼 유지)
        a.transform.SetParent(c.transform, true);
        b.transform.SetParent(c.transform, true);

        // 5. C 오브젝트 물리 설정 (공중 유지 및 안정화)
        Rigidbody2D cRb = c.AddComponent<Rigidbody2D>();
        cRb.bodyType = RigidbodyType2D.Dynamic;
        cRb.mass = 5f;
        cRb.linearDamping = 3f;
        cRb.angularDamping = 5f;
        cRb.gravityScale = 0f;               // 공중에 결합 상태로 정지 (필요 시 조정)
        cRb.linearVelocity = Vector2.zero;
        cRb.angularVelocity = 0f;
        cRb.freezeRotation = true;
        cRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void CleanUpResidualComponents(GameObject obj)
    {
        HingeJoint2D[] joints = obj.GetComponents<HingeJoint2D>();
        foreach (var j in joints) Destroy(j);

        CentralPull pull = obj.GetComponent<CentralPull>();
        if (pull != null) pull.CleanUp();
    }

    void CrushEnemy(GameObject target)
    {
        Collider2D enemyCol = target.GetComponent<Collider2D>();
        if (enemyCol != null) enemyCol.enabled = false;

        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            targetRb.linearVelocity = Vector2.zero;
            targetRb.bodyType = RigidbodyType2D.Static;
        }

        Vector3 scale = target.transform.localScale;
        Vector2 dir = rb.linearVelocity.normalized;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            target.transform.localScale = new Vector3(scale.x * 0.2f, scale.y * 1.5f, scale.z);
        else
            target.transform.localScale = new Vector3(scale.x * 1.5f, scale.y * 0.2f, scale.z);

        Destroy(target, 0.5f);
    }

    public void CleanUp()
    {
        isFixed = true;
        if (line != null) Destroy(line);
        Destroy(this);
    }
}