using UnityEngine;
using System.Collections;
using DG.Tweening;

public class PlayerAttack : MonoBehaviour
{
    [Header("공격 설정")]
    public BoxCollider2D hitbox;
    public SpriteRenderer hitboxRenderer;
    public float comboDelay = 0.6f;

    [Header("디버그")]
    public bool showHitboxDebug = false;

    [Header("데미지 설정")]
    public int damageA = 10;
    public int damageB = 10;
    public int damageC =    15;

    private int comboStep = 0;
    private float comboTimer = 0f;
    private bool isAttacking = false;
    private bool canNextCombo = false;
    private bool comboQueued = false;
    private int currentDamage = 0;

    private Animator animator;
    private PlayerParry parry;

    void Start()
    {
        animator = GetComponent<Animator>();
        parry = GetComponent<PlayerParry>();

        hitbox.enabled = false;
        if (hitboxRenderer != null)
            hitboxRenderer.enabled = false;

        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), hitbox, true);
    }

    void Update()
    {
        if (parry != null && parry.IsStunned) return;

        if (GetComponent<RainbowFlushSkill>() != null &&
            GetComponent<RainbowFlushSkill>().IsSkill() != RainbowFlushSkill.SkillState.Ready)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!isAttacking)
                Attack();
            else if (canNextCombo)
                comboQueued = true;
        }

        if (!isAttacking && comboStep > 0)
        {
            comboTimer += Time.deltaTime;
            if (comboTimer > comboDelay)
                ResetCombo();
        }
    }

    float GetDamageMultiplier()
    {
        float g = parry.parryHitGauge;

        if (g >= 500) return 1.35f;
        if (g >= 300) return 1.20f;
        if (g >= 200) return 1.10f;
        if (g >= 100) return 1.05f;

        return 1f;
    }

    void Attack()
    {
        if (isAttacking) return;
        isAttacking = true;
        comboQueued = false;

        switch (comboStep)
        {
            case 0: StartCoroutine(DoAttack("A", damageA)); break;
            case 1: StartCoroutine(DoAttack("B", damageB)); break;
            case 2: StartCoroutine(DoAttack("C", damageC)); break;
        }
    }

    IEnumerator DoAttack(string attackType, int damage)
    {
        currentDamage = damage;

        if (animator)
            animator.SetTrigger("Attack" + attackType);

        yield return new WaitForSeconds(0.1f);

        if (hitboxRenderer != null)
        {
            if (showHitboxDebug)
            {
                DOTween.Kill(hitboxRenderer);
                if (attackType == "A") hitboxRenderer.color = new Color(1f, 0f, 0f, 1f);
                else if (attackType == "B") hitboxRenderer.color = new Color(0f, 1f, 0f, 1f);
                else hitboxRenderer.color = new Color(0f, 0.5f, 1f, 1f);

                hitboxRenderer.enabled = true;
                hitboxRenderer.DOFade(0f, 0.4f);
            }
            else hitboxRenderer.enabled = false;
        }

        hitbox.enabled = true;

        yield return new WaitForSeconds(0.15f);

        hitbox.enabled = false;

        canNextCombo = true;

        float nextComboWindow = 0.3f;
        float elapsed = 0f;

        while (elapsed < nextComboWindow)
        {
            if (comboQueued)
            {
                comboStep++;
                if (comboStep > 2) comboStep = 0;

                canNextCombo = false;
                comboQueued = false;
                isAttacking = false;

                Attack();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        canNextCombo = false;
        isAttacking = false;

        comboStep++;
        if (comboStep > 2) comboStep = 0;

        comboTimer = 0f;
    }

    void ResetCombo()
    {
        comboStep = 0;
        comboTimer = 0f;
        isAttacking = false;
        canNextCombo = false;
        comboQueued = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitbox.enabled) return;
        if (other.gameObject == gameObject) return;

        // 최종 데미지 계산
        float parryMult = GetDamageMultiplier();
        int finalDamage = Mathf.RoundToInt(currentDamage * parryMult);

        float ampMult = 1f;
        var amp = other.GetComponent<EnemyDamageAmpData>();
        if (amp != null) ampMult = amp.Multiplier;

        finalDamage = Mathf.RoundToInt(finalDamage * ampMult);

        // 1️방패 우선 처리
        ShieldController shield = other.GetComponentInChildren<ShieldController>();
        if (shield != null && !shield.IsBroken)
        {
            shield.TakeShieldDamage(finalDamage);
            Debug.Log($"[플레이어 공격] 방패 피해 {finalDamage}");
            return;
        }

        // 2️방패 없으면 적 체력
        ShieldEnemyManager enemy = other.GetComponentInParent<ShieldEnemyManager>();
        if (enemy != null)
        {
            enemy.TakeDamage(finalDamage);
            Debug.Log($"[플레이어 공격] 적 피해 {finalDamage}");
        }
        Debug.Log($"공격 → {other.name} : {finalDamage} 데미지 " + $"(패링히트 x{parryMult:0.00}, 받피증 x{ampMult:0.00})");

        // 3아예 처음부터 없는 적 체력
        Health noShieldEnemy = other.GetComponent<Health>();
        if (noShieldEnemy != null)
        {
            noShieldEnemy.TakeDamage(finalDamage);
        }

    }

}
