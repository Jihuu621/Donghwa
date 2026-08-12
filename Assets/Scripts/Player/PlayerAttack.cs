using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class PlayerAttack : MonoBehaviour
{
#if false
    // Legacy 3-step combo. Keep this disabled until combo sprite sheets are added.
    [Header("공격 설정")]
    public BoxCollider2D hitbox;
    public SpriteRenderer hitboxRenderer;

    [Header("디버그")]
    public bool showHitboxDebug = false;
    public bool logComboDebug = true;

    [Header("데미지 설정")]
    public int damageA = 10;
    public int damageB = 10;
    public int damageC = 15;

    [Header("애니메이터 상태 이름")]
    public string attack1StateName = "Attack1";
    public string attack2StateName = "Attack2";
    public string attack3StateName = "Attack3";

    [Header("안전장치")]
    public float attackSafetyTimeout = 1.2f;

    private int comboStep = 0;
    private bool isAttacking = false;
    private bool canNextCombo = false;
    private bool comboQueued = false;
    private int currentDamage = 0;

    private Animator animator;
    private PlayerParry parry;
    private Coroutine safetyRoutine;

    void Start()
    {
        animator = GetComponent<Animator>();
        parry = GetComponent<PlayerParry>();

        hitbox.enabled = false;
        if (hitboxRenderer != null) hitboxRenderer.enabled = false;

        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), hitbox, true);
    }

    void Update()
    {
        if (parry != null && parry.IsStunned) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!isAttacking)
            {
                Attack();
            }
            else
            {
                comboQueued = true;
                if (logComboDebug) Debug.Log("[PlayerAttack] 콤보 예약됨 (선입력 버퍼)");
            }
        }
    }

    void Attack()
    {
        isAttacking = true;
        canNextCombo = false;
        comboQueued = false;

        comboStep++;
        if (comboStep > 3) comboStep = 1;

        if (comboStep == 1) currentDamage = damageA;
        else if (comboStep == 2) currentDamage = damageB;
        else currentDamage = damageC;

        PlayCurrentAttackAnimation();

        if (logComboDebug) Debug.Log($"[PlayerAttack] Attack 시작 step={comboStep}");

        if (safetyRoutine != null) StopCoroutine(safetyRoutine);
        safetyRoutine = StartCoroutine(SafetyTimeout());
    }

    void PlayCurrentAttackAnimation()
    {
        if (animator == null) return;

        string stateName = attack1StateName;
        if (comboStep == 2) stateName = attack2StateName;
        else if (comboStep == 3) stateName = attack3StateName;

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning($"[PlayerAttack] Animator State 없음: {stateName}");
            return;
        }

        animator.Play(stateHash, 0, 0f);
    }

    private IEnumerator SafetyTimeout()
    {
        yield return new WaitForSeconds(attackSafetyTimeout);
        if (isAttacking)
        {
            Debug.LogWarning("[PlayerAttack] AE_EndAttack 이벤트가 호출되지 않아 강제 리셋합니다. 애니메이션 클립의 이벤트를 확인하세요.");
            if (comboQueued) Attack();
            else ResetCombo();
        }
    }

    public void AE_TriggerHitbox()
    {
        StartCoroutine(HitboxActiveRoutine());
    }

    private IEnumerator HitboxActiveRoutine()
    {
        hitbox.enabled = true;
        if (hitboxRenderer != null && showHitboxDebug)
        {
            DOTween.Kill(hitboxRenderer);
            Color debugColor = (comboStep == 1) ? Color.red : (comboStep == 2) ? Color.green : Color.blue;
            hitboxRenderer.color = debugColor;
            hitboxRenderer.enabled = true;
            hitboxRenderer.DOFade(0f, 0.3f);
        }
        yield return new WaitForSeconds(0.1f);
        hitbox.enabled = false;
    }

    /* 패링 히트 게이지에 따른 공격력 배율은 기획 확정 전까지 비활성화합니다.
    private float GetDamageMultiplier()
    {
        float g = parry.ParryHitGauge;
        if (g >= 500f) return 1.35f;
        if (g >= 300f) return 1.20f;
        if (g >= 200f) return 1.10f;
        if (g >= 100f) return 1.05f;
        return 1f;
    }
    */

    public void AE_OpenComboWindow()
    {
        canNextCombo = true;
        if (logComboDebug) Debug.Log("[PlayerAttack] 콤보 윈도우 열림");
    }

    public void AE_EndAttack()
    {
        if (logComboDebug) Debug.Log($"[PlayerAttack] AE_EndAttack 호출 (queued={comboQueued})");

        if (safetyRoutine != null)
        {
            StopCoroutine(safetyRoutine);
            safetyRoutine = null;
        }

        if (comboQueued)
        {
            Attack();
        }
        else
        {
            ResetCombo();
        }
    }

    public void ResetCombo()
    {
        comboStep = 0;
        isAttacking = false;
        canNextCombo = false;
        comboQueued = false;
        if (animator != null) animator.SetInteger("ComboStep", 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitbox.enabled) return;
        if (other.gameObject == gameObject) return;

        float finalDamage = currentDamage;

        float ampMult = 1f;
        var amp = other.GetComponent<EnemyDamageAmpData>();
        if (amp != null) ampMult = amp.Multiplier;

        finalDamage *= ampMult;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(finalDamage, gameObject);
            Debug.Log($"공격 -> {other.name} : {finalDamage} 데미지 (받피증 x{ampMult:0.00})");
        }
    }
}
#endif

    [Header("임시 단일 공격")]
    [Tooltip("현재 Player_Attack 애니메이션에 맞춘 단일 공격 피해량입니다.")]
    [FormerlySerializedAs("damageA")]
    public int temporaryDamage = 10;
    [SerializeField, Min(0f)] private float hitboxStartDelay = 0.16f;
    [SerializeField, Min(0.01f)] private float hitboxActiveDuration = 0.10f;
    [SerializeField, Min(0.1f)] private float attackDuration = 0.42f;
    [SerializeField] private string attackTriggerName = "AttackTrigger";

    [Header("공격 참조")]
    public BoxCollider2D hitbox;
    public SpriteRenderer hitboxRenderer;

    [Header("디버그")]
    public bool showHitboxDebug = false;

    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

    private Animator animator;
    private PlayerParry parry;
    private Collider2D playerCollider;
    private Coroutine attackRoutine;
    private bool isAttacking;
    private bool isHitboxActive;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        parry = GetComponent<PlayerParry>();
        playerCollider = GetComponent<Collider2D>();

        SetHitboxActive(false);

        if (playerCollider != null && hitbox != null)
        {
            Physics2D.IgnoreCollision(playerCollider, hitbox, true);
        }
    }

    private void Update()
    {
        if (parry != null && !parry.IsReady)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartSingleAttack();
        }
    }

    private void StartSingleAttack()
    {
        isAttacking = true;
        damagedTargets.Clear();

        if (animator != null)
        {
            animator.ResetTrigger(attackTriggerName);
            animator.SetTrigger(attackTriggerName);
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackRoutine = StartCoroutine(SingleAttackRoutine());
    }

    private IEnumerator SingleAttackRoutine()
    {
        yield return new WaitForSeconds(hitboxStartDelay);
        SetHitboxActive(true);

        yield return new WaitForSeconds(hitboxActiveDuration);
        SetHitboxActive(false);

        float recoveryTime = Mathf.Max(0f, attackDuration - hitboxStartDelay - hitboxActiveDuration);
        if (recoveryTime > 0f)
        {
            yield return new WaitForSeconds(recoveryTime);
        }

        isAttacking = false;
        attackRoutine = null;
    }

    private void SetHitboxActive(bool active)
    {
        isHitboxActive = active;

        if (hitbox != null)
        {
            hitbox.enabled = active;
        }

        if (hitboxRenderer != null)
        {
            hitboxRenderer.enabled = active && showHitboxDebug;
            if (showHitboxDebug)
            {
                hitboxRenderer.color = new Color(1f, 0.15f, 0.15f, 0.35f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isHitboxActive || other == null || other.gameObject == gameObject)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || !damagedTargets.Add(damageable))
        {
            return;
        }

        float damage = temporaryDamage;
        EnemyDamageAmpData amp = other.GetComponent<EnemyDamageAmpData>();
        if (amp != null)
        {
            damage *= amp.Multiplier;
        }

        damageable.TakeDamage(damage, gameObject);
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isAttacking = false;
        SetHitboxActive(false);
    }
}
