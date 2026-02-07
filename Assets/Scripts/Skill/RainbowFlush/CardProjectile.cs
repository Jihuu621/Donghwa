using UnityEngine;
using TMPro;
using DG.Tweening;

public class CardProjectile : MonoBehaviour
{
    private bool isFiring = false;
    private bool isPreparing = false;

    public enum Suit { Heart, Diamond, Clover, Spade, Joker }
    public enum Rank { J, Q, K, A, None }

    public Suit mySuit;
    public Rank myRank;

    private SpriteRenderer spriteRenderer;
    private TextMeshPro textMesh;
    private Transform centerTarget;

    [Header("Movement Settings")]
    public float moveSpeed = 25f;
    private Vector3 fireDirection;
    public float currentAngle;
    public float currentRadius;

    [Header("Explosion Data (Joker Only)")]
    private float expRadius;
    private float expDamage;
    private float expDuration;
    private float expAmount;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        textMesh = GetComponentInChildren<TextMeshPro>();
        if (GetComponent<Rigidbody2D>()) GetComponent<Rigidbody2D>().gravityScale = 0;
    }

    public void SetCardInfo(Suit suit, Rank rank)
    {
        mySuit = suit;
        myRank = rank;
        UpdateVisuals();
    }

    // RainbowFlushSkill에서 판정된 족보 수치를 조커에게 주입
    public void SetExplosionData(float r, float d, float dur, float amt)
    {
        expRadius = r;
        expDamage = d;
        expDuration = dur;
        expAmount = amt;
    }

    private void UpdateVisuals()
    {
        if (textMesh == null) return;
        textMesh.text = (myRank == Rank.None) ? "!" : myRank.ToString();
        textMesh.color = Color.white;

        switch (mySuit)
        {
            case Suit.Heart: spriteRenderer.color = new Color(.7f, 0f, 0f); break;
            case Suit.Diamond: spriteRenderer.color = new Color(.7f, .5f, 0f); break;
            case Suit.Clover: spriteRenderer.color = new Color(0f, .7f, 0f); break;
            case Suit.Spade: spriteRenderer.color = new Color(0f, 0f, .7f); break;
            case Suit.Joker: spriteRenderer.color = Color.black; break;
        }
    }

    public void UpdateOrbit(Transform center, float angle, float radius)
    {
        if (isFiring || isPreparing) return;
        centerTarget = center;
        currentAngle = angle;
        currentRadius = radius;
    }

    public void Launch(Vector3 direction)
    {
        isPreparing = true;
        fireDirection = direction;
        transform.SetParent(null);

        Vector3 targetPos = centerTarget.position + (direction * 1.0f);
        Sequence launchSeq = DOTween.Sequence();
        launchSeq.Append(transform.DOMove(targetPos, 0.3f).SetEase(Ease.OutCubic));
        launchSeq.Join(transform.DORotate(new Vector3(0, 0, -90), 0.3f).SetEase(Ease.OutQuad));
        launchSeq.OnComplete(() => {
            isPreparing = false;
            isFiring = true;
        });

        // 빗나갔을 때를 대비한 자동 파괴
        Destroy(gameObject, 3f);
    }

    void FixedUpdate()
    {
        if (!isFiring && !isPreparing && centerTarget != null)
        {
            float radian = currentAngle * Mathf.Deg2Rad;
            float x = centerTarget.position.x + Mathf.Cos(radian) * currentRadius;
            float y = centerTarget.position.y + Mathf.Sin(radian) * currentRadius;
            transform.position = new Vector3(x, y, 0);
            transform.rotation = Quaternion.Euler(0, 0, currentAngle - 90);
        }
        else if (isFiring)
        {
            transform.Translate(fireDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isFiring) return;

        // 적 또는 지형에 닿았을 때
        if (collision.CompareTag("Enemy") || collision.CompareTag("Untagged"))
        {
            if (mySuit == Suit.Joker)
            {
                ExecuteJokerExplosion();
            }
            else
            {
                if (collision.CompareTag("Enemy"))
                    // 일반 카드는 단일 대상에게 데미지 (기본 20)
                    ApplySingleDamage(collision, 20f);
                Destroy(gameObject);
            }
        }
    }

    private void ExecuteJokerExplosion()
    {
        // 싱글톤 매니저 호출
        if (ExplosionManager.Instance != null)
        {
            ExplosionManager.Instance.CreateExplosion(
                transform.position,
                expRadius,
                expDamage,
                expAmount,
                expDuration
            );
        }
        else
        {
            Debug.LogWarning("ExplosionManager 인스턴스를 찾을 수 없습니다!");
        }

        Destroy(gameObject);
    }

    private void ApplySingleDamage(Collider2D other, float baseDamage)
    {
        // ExplosionManager가 없을 때를 대비하거나 일반 카드 처리를 위해 유지
        float ampMult = 1f;
        var amp = other.GetComponent<EnemyDamageAmpData>();
        if (amp != null) ampMult = amp.Multiplier;

        int finalDamage = Mathf.RoundToInt(baseDamage * ampMult);

        // 1. 방패 처리
        var shield = other.GetComponentInChildren<ShieldController>();
        if (shield != null && !shield.IsBroken)
        {
            shield.TakeShieldDamage(finalDamage);
            return;
        }

        // 2. 적 체력 처리
        var sEnemy = other.GetComponentInParent<ShieldEnemyManager>();
        if (sEnemy != null) sEnemy.TakeDamage(finalDamage);

        var health = other.GetComponent<Health>();
        if (health != null) health.TakeDamage(finalDamage);
    }
}