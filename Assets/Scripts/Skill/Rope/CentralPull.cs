using UnityEngine;

public class CentralPull : MonoBehaviour
{
    private const float ArrivalTolerance = 0.001f;

    private Rigidbody2D rb;
    private CentralPull partnerPull;
    private Vector2 targetPosition;
    private float pullSpeed;
    private float horizontalGap;
    private GameObject collisionEffectPrefab;

    private bool isInitialized;
    private bool hasReachedTarget;
    private bool isStopped;
    private bool ownsMerge;
    private bool mergeStarted;

    private RigidbodyType2D previousBodyType;
    private float previousGravityScale;
    private RigidbodyConstraints2D previousConstraints;
    private CollisionDetectionMode2D previousCollisionMode;
    private RigidbodyInterpolation2D previousInterpolation;

    private Collider2D[] ownColliders;
    private Collider2D[] partnerColliders;
    private bool pairCollisionIgnored;

    private LineRenderer pullLine;
    private Material pullLineMaterial;

    /// <summary>
    /// 두 블록(또는 이미 합쳐진 두 그룹)을 수평 일렬 목표점으로 당긴다.
    /// 그룹 내부 자식의 상대 위치는 바꾸지 않고 루트 Rigidbody2D만 이동한다.
    /// </summary>
    public static bool TryStartPair(
        GameObject a,
        GameObject b,
        GameObject effectPrefab,
        float speed = 20f,
        float horizontalGap = 0f)
    {
        if (a == null || b == null || a == b) return false;
        if (a.transform.IsChildOf(b.transform) || b.transform.IsChildOf(a.transform)) return false;
        if (a.GetComponent<CentralPull>() != null || b.GetComponent<CentralPull>() != null) return false;

        Rigidbody2D aRb = a.GetComponent<Rigidbody2D>();
        Rigidbody2D bRb = b.GetComponent<Rigidbody2D>();
        if (aRb == null || bRb == null) return false;

        if (!TryGetObjectBounds(a, out Bounds aBounds) ||
            !TryGetObjectBounds(b, out Bounds bBounds))
        {
            return false;
        }

        CalculateHorizontalTargets(
            a,
            aBounds,
            b,
            bBounds,
            horizontalGap,
            out Vector2 aTarget,
            out Vector2 bTarget);

        CentralPull aPull = a.AddComponent<CentralPull>();
        CentralPull bPull = b.AddComponent<CentralPull>();

        aPull.Initialize(aRb, bPull, aTarget, speed, horizontalGap, effectPrefab, true);
        bPull.Initialize(bRb, aPull, bTarget, speed, horizontalGap, effectPrefab, false);

        Collider2D[] aColliders = GetPhysicalColliders(a);
        Collider2D[] bColliders = GetPhysicalColliders(b);
        SetPairCollisionIgnored(aColliders, bColliders, true);

        aPull.SetIgnoredPair(aColliders, bColliders);
        bPull.SetIgnoredPair(bColliders, aColliders);
        aPull.CreatePullLine();

        return true;
    }

    private void Initialize(
        Rigidbody2D body,
        CentralPull partner,
        Vector2 target,
        float speed,
        float gap,
        GameObject effectPrefab,
        bool mergeOwner)
    {
        rb = body;
        partnerPull = partner;
        targetPosition = target;
        pullSpeed = Mathf.Max(0.1f, speed);
        horizontalGap = gap;
        collisionEffectPrefab = effectPrefab;
        ownsMerge = mergeOwner;

        previousBodyType = rb.bodyType;
        previousGravityScale = rb.gravityScale;
        previousConstraints = rb.constraints;
        previousCollisionMode = rb.collisionDetectionMode;
        previousInterpolation = rb.interpolation;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || isStopped || pullLine == null || partnerPull == null) return;

        pullLine.SetPosition(0, transform.position);
        pullLine.SetPosition(1, partnerPull.transform.position);
    }

    private void FixedUpdate()
    {
        if (!isInitialized || isStopped) return;

        if (partnerPull == null)
        {
            CancelPull();
            return;
        }

        if (!hasReachedTarget)
        {
            Vector2 toTarget = targetPosition - rb.position;
            float maxStep = pullSpeed * Time.fixedDeltaTime;

            if (toTarget.sqrMagnitude <= (maxStep + ArrivalTolerance) * (maxStep + ArrivalTolerance))
            {
                rb.position = targetPosition;
                rb.linearVelocity = Vector2.zero;
                hasReachedTarget = true;
            }
            else
            {
                rb.linearVelocity = toTarget.normalized * pullSpeed;
            }
        }

        if (ownsMerge && hasReachedTarget && partnerPull.hasReachedTarget)
        {
            CompleteMerge();
        }
    }

    private void CompleteMerge()
    {
        if (mergeStarted || partnerPull == null) return;

        mergeStarted = true;
        partnerPull.mergeStarted = true;

        rb.position = targetPosition;
        partnerPull.rb.position = partnerPull.targetPosition;
        rb.linearVelocity = Vector2.zero;
        partnerPull.rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();

        // 당김 과정의 물리 오차와 그룹 루트의 피벗 오차를 제거한다.
        // 이 단계는 선택 순서와 무관하게 현재 콜라이더의 좌우 경계를 다시 맞춘다.
        SnapPairToFinalLayout();

        RestorePairCollisions();
        partnerPull.RestorePairCollisions();

        GameObject a = gameObject;
        GameObject b = partnerPull.gameObject;
        float combinedMass = Mathf.Max(1f, rb.mass + partnerPull.rb.mass);

        // 기존 Rigidbody2D가 실제로 제거되는 다음 프레임까지 owner는 유지한다.
        // 그래야 compound collider가 새 루트에 붙은 뒤에도 마지막 정렬을 보장할 수 있다.
        StopWithoutRestoringBody(false);
        partnerPull.StopWithoutRestoringBody(true);

        GameObject merged = MergeObjects(a, b, combinedMass);
        if (merged != null)
        {
            StartCoroutine(FinalizeMergedLayout(merged, a, b));
        }
        else
        {
            Destroy(this);
        }
    }

    private void SnapPairToFinalLayout()
    {
        if (partnerPull == null ||
            !TryGetObjectBounds(gameObject, out Bounds aBounds) ||
            !TryGetObjectBounds(partnerPull.gameObject, out Bounds bBounds))
        {
            return;
        }

        CalculateHorizontalTargets(
            gameObject,
            aBounds,
            partnerPull.gameObject,
            bBounds,
            horizontalGap,
            out Vector2 aFinalTarget,
            out Vector2 bFinalTarget);

        rb.position = aFinalTarget;
        partnerPull.rb.position = bFinalTarget;
        Physics2D.SyncTransforms();
    }

    private System.Collections.IEnumerator FinalizeMergedLayout(GameObject merged, GameObject a, GameObject b)
    {
        // Destroy(Rigidbody2D)는 프레임 끝에 처리된다. 그 뒤에 자식 Sprite/Collider의 실제 월드 경계를 읽는다.
        yield return null;

        if (merged != null && a != null && b != null &&
            TryGetObjectBounds(a, out Bounds aBounds) &&
            TryGetObjectBounds(b, out Bounds bBounds))
        {
            CalculateHorizontalTargets(
                a,
                aBounds,
                b,
                bBounds,
                horizontalGap,
                out Vector2 aFinalTarget,
                out Vector2 bFinalTarget);

            a.transform.position = new Vector3(aFinalTarget.x, aFinalTarget.y, a.transform.position.z);
            b.transform.position = new Vector3(bFinalTarget.x, bFinalTarget.y, b.transform.position.z);
            Physics2D.SyncTransforms();
        }

        Destroy(this);
    }

    private static void CalculateHorizontalTargets(
        GameObject a,
        Bounds aBounds,
        GameObject b,
        Bounds bBounds,
        float gap,
        out Vector2 aTarget,
        out Vector2 bTarget)
    {
        // x가 같으면 첫 번째로 선택한 그룹(a)을 왼쪽에 두어 결과가 항상 예측 가능하게 한다.
        bool aIsLeft = aBounds.center.x < bBounds.center.x ||
                       Mathf.Approximately(aBounds.center.x, bBounds.center.x);

        Bounds leftBounds = aIsLeft ? aBounds : bBounds;
        Bounds rightBounds = aIsLeft ? bBounds : aBounds;
        Vector2 center = ((Vector2)aBounds.center + (Vector2)bBounds.center) * 0.5f;

        float totalWidth = leftBounds.size.x + rightBounds.size.x + gap;
        Vector2 desiredLeftCenter = new Vector2(
            center.x - totalWidth * 0.5f + leftBounds.extents.x,
            center.y);
        Vector2 desiredRightCenter = new Vector2(
            center.x + totalWidth * 0.5f - rightBounds.extents.x,
            center.y);

        Vector2 leftRootPosition = aIsLeft ? a.transform.position : b.transform.position;
        Vector2 rightRootPosition = aIsLeft ? b.transform.position : a.transform.position;
        Vector2 leftTarget = leftRootPosition + desiredLeftCenter - (Vector2)leftBounds.center;
        Vector2 rightTarget = rightRootPosition + desiredRightCenter - (Vector2)rightBounds.center;

        aTarget = aIsLeft ? leftTarget : rightTarget;
        bTarget = aIsLeft ? rightTarget : leftTarget;
    }

    private static bool TryGetObjectBounds(GameObject obj, out Bounds bounds)
    {
        // 플레이어가 보는 실제 블록 테두리를 우선한다. Collider와 스프라이트 크기가 달라도 틈이 남지 않는다.
        SpriteRenderer[] sprites = obj.GetComponentsInChildren<SpriteRenderer>();
        if (sprites.Length > 0)
        {
            bool hasVisibleSprite = false;
            bounds = default;

            foreach (SpriteRenderer sprite in sprites)
            {
                if (sprite == null || !sprite.enabled) continue;

                if (!hasVisibleSprite)
                {
                    bounds = sprite.bounds;
                    hasVisibleSprite = true;
                }
                else
                {
                    bounds.Encapsulate(sprite.bounds);
                }
            }

            if (hasVisibleSprite) return true;
        }

        Collider2D[] colliders = GetPhysicalColliders(obj);
        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return true;
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        bounds = default;
        return false;
    }

    private static Collider2D[] GetPhysicalColliders(GameObject obj)
    {
        Collider2D[] allColliders = obj.GetComponentsInChildren<Collider2D>();
        int count = 0;

        foreach (Collider2D col in allColliders)
        {
            if (col != null && col.enabled && !col.isTrigger) count++;
        }

        Collider2D[] result = new Collider2D[count];
        int index = 0;
        foreach (Collider2D col in allColliders)
        {
            if (col == null || !col.enabled || col.isTrigger) continue;
            result[index++] = col;
        }

        return result;
    }

    private void SetIgnoredPair(Collider2D[] own, Collider2D[] partner)
    {
        ownColliders = own;
        partnerColliders = partner;
        pairCollisionIgnored = true;
    }

    private static void SetPairCollisionIgnored(Collider2D[] a, Collider2D[] b, bool ignore)
    {
        foreach (Collider2D aCol in a)
        {
            if (aCol == null) continue;

            foreach (Collider2D bCol in b)
            {
                if (bCol != null)
                {
                    Physics2D.IgnoreCollision(aCol, bCol, ignore);
                }
            }
        }
    }

    private void RestorePairCollisions()
    {
        if (!pairCollisionIgnored) return;

        SetPairCollisionIgnored(ownColliders, partnerColliders, false);
        pairCollisionIgnored = false;
    }

    private void CreatePullLine()
    {
        pullLine = gameObject.AddComponent<LineRenderer>();
        pullLine.useWorldSpace = true;
        pullLine.positionCount = 2;
        pullLine.startWidth = 0.109f;
        pullLine.endWidth = 0.109f;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            pullLineMaterial = new Material(shader);
            pullLine.material = pullLineMaterial;
        }
    }

    private void StopWithoutRestoringBody(bool destroyComponent)
    {
        isStopped = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        DestroyPullLine();
        if (destroyComponent) Destroy(this);
    }

    private void CancelPull()
    {
        if (isStopped) return;

        isStopped = true;
        RestorePairCollisions();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = previousBodyType;
            rb.gravityScale = previousGravityScale;
            rb.constraints = previousConstraints;
            rb.collisionDetectionMode = previousCollisionMode;
            rb.interpolation = previousInterpolation;
        }

        DestroyPullLine();
        Destroy(this);
    }

    private static GameObject MergeObjects(GameObject a, GameObject b, float combinedMass)
    {
        if (!TryGetObjectBounds(a, out Bounds aBounds) ||
            !TryGetObjectBounds(b, out Bounds bBounds))
        {
            return null;
        }

        Bounds mergedBounds = aBounds;
        mergedBounds.Encapsulate(bBounds);

        GameObject merged = new GameObject("MergedBlock_Group");
        merged.transform.position = new Vector3(
            mergedBounds.center.x,
            mergedBounds.center.y,
            (a.transform.position.z + b.transform.position.z) * 0.5f);
        merged.layer = a.layer;

        Rigidbody2D aRb = a.GetComponent<Rigidbody2D>();
        Rigidbody2D bRb = b.GetComponent<Rigidbody2D>();

        DisableAndRemoveBody(aRb);
        DisableAndRemoveBody(bRb);

        // 월드 배치를 유지하므로 A+B로 만든 D의 내부 형태도 D+C 합체에서 그대로 보존된다.
        a.transform.SetParent(merged.transform, true);
        b.transform.SetParent(merged.transform, true);

        Rigidbody2D mergedRb = merged.AddComponent<Rigidbody2D>();
        mergedRb.bodyType = RigidbodyType2D.Static;
        mergedRb.mass = combinedMass;
        mergedRb.linearDamping = 3f;
        mergedRb.angularDamping = 5f;
        mergedRb.gravityScale = 0f;
        mergedRb.freezeRotation = true;
        // 합체 후에는 고정 발판으로 유지하고, 다음 Alt 당김을 시작할 때 다시 Dynamic/Continuous로 전환한다.
        mergedRb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        mergedRb.interpolation = RigidbodyInterpolation2D.None;

        Physics2D.SyncTransforms();
        return merged;
    }

    private static void DisableAndRemoveBody(Rigidbody2D body)
    {
        if (body == null) return;

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
        Destroy(body);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInitialized || isStopped || collision.gameObject == null) return;

        GameObject target = collision.gameObject;
        if (!target.CompareTag("Enemy")) return;

        Collider2D enemyCol = target.GetComponent<Collider2D>();
        if (enemyCol != null && !enemyCol.enabled) return;

        CrushEnemy(target);
    }

    private void CrushEnemy(GameObject target)
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
        Vector2 direction = rb.linearVelocity.normalized;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            target.transform.localScale = new Vector3(scale.x * 0.2f, scale.y * 1.5f, scale.z);
        else
            target.transform.localScale = new Vector3(scale.x * 1.5f, scale.y * 0.2f, scale.z);

        if (collisionEffectPrefab != null)
        {
            Instantiate(collisionEffectPrefab, target.transform.position, Quaternion.identity);
        }

        Destroy(target, 0.5f);
    }

    private void DestroyPullLine()
    {
        if (pullLine != null)
        {
            Destroy(pullLine);
            pullLine = null;
        }

        if (pullLineMaterial != null)
        {
            Destroy(pullLineMaterial);
            pullLineMaterial = null;
        }
    }

    private void OnDestroy()
    {
        RestorePairCollisions();
        DestroyPullLine();
    }
}
