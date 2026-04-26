using UnityEngine;
using System.Collections;
using DG.Tweening;

public class PlayerAttack : MonoBehaviour
{
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
                // 윈도우가 아직 안 열렸어도 미리 예약해두기 (인풋 버퍼)
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

        // 안전장치: 일정 시간 내에 AE_EndAttack이 호출되지 않으면 강제 종료
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

    // =========================================================
    //  애니메이션 이벤트 (각 클립의 특정 프레임에서 호출)
    // =========================================================

    // 타격 발생 (2컷, 6컷, 14컷 위치)
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

    float GetDamageMultiplier()
    {
        if (parry == null) return 1f; // 안전장치: parry 컴포넌트가 없을 경우 기본 배율 1배

        float g = parry.parryHitGauge;

        if (g >= 500) return 1.35f;
        if (g >= 300) return 1.20f;
        if (g >= 200) return 1.10f;
        if (g >= 100) return 1.05f;

        return 1f;
    }

    // 다음 콤보 입력을 허용하는 타이밍 (휘두른 직후)
    public void AE_OpenComboWindow()
    {
        canNextCombo = true;
        if (logComboDebug) Debug.Log("[PlayerAttack] 콤보 윈도우 열림");
    }

    // 애니메이션 종료 시점 (클립 끝)
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
