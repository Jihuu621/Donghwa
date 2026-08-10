using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerParry : MonoBehaviour
{
    private enum GuardState
    {
        Ready,
        PerfectGuard,
        Guard,
        Recovery
    }

    [Header("가드 입력")]
    [SerializeField] private KeyCode guardKey = KeyCode.LeftControl;
    [FormerlySerializedAs("parryWindow")]
    [SerializeField, Min(0.01f)] private float perfectGuardWindow = 0.2f;
    [SerializeField, Min(0.01f)] private float guardWindow = 0.6f;
    [FormerlySerializedAs("parryCooldown")]
    [SerializeField, Min(0f)] private float recoveryDuration = 0.5f;
    [SerializeField, Min(0f)] private float guardActivationCost = 5f;

    [Header("가드 효과")]
    [SerializeField, Range(0f, 1f)] private float guardDamageReduce = 0.6f;
    [SerializeField, Min(0f)] private float guardPenalty = 20f;
    [SerializeField, Min(0f)] private float guardBreakStunDuration = 1f;

    [Header("패링 게이지")]
    [SerializeField, Min(1f)] private float maxGauge = 100f;
    [SerializeField] private float currentGauge;
    [SerializeField, Min(0f)] private float gaugeRegenPerSec = 1f;
    [SerializeField, Min(0f)] private float parryReward = 5f;

    /* 패링 히트 시스템은 기획 확정 전까지 비활성화합니다.
    [Header("패링 히트 시스템")]
    [SerializeField] private float parryHitGauge;
    [SerializeField, Min(1f)] private float maxParryHitGauge = 600f;
    [SerializeField, Min(0f)] private float parryHitGain = 30f;
    [SerializeField, Min(0f)] private float parryHitDecay = 10f;
    [SerializeField, Min(0f)] private float parryHitDecayDelay = 2f;
    */

    [Header("가드 판정 표시")]
    [SerializeField] private GameObject great;
    [SerializeField] private GameObject good;
    [SerializeField] private GameObject bad;

    private GuardState _guardState = GuardState.Ready;
    private float _stateTimer;
    private float _stunTimer;

    // 기존 디버거와 체셔캣 연동에서 사용하는 공개 판정입니다.
    // 실패 선딜은 제거됐지만 기존 디버거 호환을 위해 false로 유지합니다.
    public bool IsFailTime => false;
    public bool IsParryTime => _guardState == GuardState.PerfectGuard;
    public bool IsGuardTime => _guardState == GuardState.Guard;
    public bool IsStunned => _stunTimer > 0f;
    public bool IsReady => _guardState == GuardState.Ready && !IsStunned;
    public float CurrentGauge => currentGauge;

    private void Start()
    {
        currentGauge = maxGauge;
        SetFeedbackObjectsInactive();
    }

    private void Update()
    {
        TickStun();
        TickGuardState();
        TickGauge();

        if (Input.GetKeyDown(guardKey)) TryActivateGuard();
    }

    private void TryActivateGuard()
    {
        if (!IsReady || currentGauge < guardActivationCost) return;

        currentGauge -= guardActivationCost;
        SetGuardState(GuardState.PerfectGuard, perfectGuardWindow);
    }

    private void TickGuardState()
    {
        if (_guardState == GuardState.Ready) return;

        _stateTimer -= Time.deltaTime;
        if (_stateTimer > 0f) return;

        switch (_guardState)
        {
            case GuardState.PerfectGuard:
                SetGuardState(GuardState.Guard, guardWindow);
                break;
            case GuardState.Guard:
                SetGuardState(GuardState.Recovery, recoveryDuration);
                break;
            case GuardState.Recovery:
                SetGuardState(GuardState.Ready, 0f);
                break;
        }
    }

    private void TickStun()
    {
        if (_stunTimer <= 0f) return;
        _stunTimer = Mathf.Max(0f, _stunTimer - Time.deltaTime);
    }

    private void TickGauge()
    {
        if (!IsStunned)
        {
            currentGauge = Mathf.Min(maxGauge, currentGauge + gaugeRegenPerSec * Time.deltaTime);
        }

        /* 패링 히트 게이지 감소 처리 비활성화
        if (Time.time - _lastParryHitTime > parryHitDecayDelay)
            parryHitGauge = Mathf.Max(0f, parryHitGauge - parryHitDecay * Time.deltaTime);
        */
    }

    private void SetGuardState(GuardState state, float duration)
    {
        _guardState = state;
        _stateTimer = Mathf.Max(0f, duration);

        if (state == GuardState.Recovery && _stateTimer <= 0f)
        {
            _guardState = GuardState.Ready;
        }
    }

    public float OnHit(float damage)
    {
        RestZoneSkill rest = GetComponent<RestZoneSkill>();
        if (rest != null && rest.IsZoneActive && rest.IsPlayerInsideZone())
        {
            Debug.Log("<color=cyan>[스킬이름이 어떻게 휴식시간]</color> 구역 안 무적! (피해 0)");
            return 0f;
        }

        if (IsParryTime)
        {
            ShowFeedback(great);
            if (CamaraShake.Instance != null) CamaraShake.Instance.Shake();

            currentGauge = Mathf.Min(maxGauge, currentGauge + parryReward);
            /* 패링 히트 게이지 획득 처리 비활성화
            parryHitGauge = Mathf.Min(maxParryHitGauge, parryHitGauge + parryHitGain);
            _lastParryHitTime = Time.time;
            */

            Debug.Log("<color=lime>[플레이어] 완벽 가드 성공! 피해 0</color>");
            return 0f;
        }

        if (IsGuardTime)
        {
            float reducedDamage = damage * (1f - guardDamageReduce);
            currentGauge = Mathf.Max(0f, currentGauge - guardPenalty);
            ShowFeedback(good);

            if (currentGauge <= 0f)
            {
                BreakGuard();
            }

            Debug.Log($"<color=yellow>[플레이어] 가드 성공! 피해 {reducedDamage:0.##}</color>");
            return reducedDamage;
        }

        Debug.Log("<color=white>[플레이어] 일반 피격</color>");
        return damage;
    }

    private void BreakGuard()
    {
        _stunTimer = guardBreakStunDuration;
        SetGuardState(GuardState.Recovery, Mathf.Max(recoveryDuration, guardBreakStunDuration));
        ShowFeedback(bad);
        Debug.Log("<color=purple>[플레이어] 가드 게이지 소진, 기절!</color>");
    }

    private void ShowFeedback(GameObject target)
    {
        if (target != null) StartCoroutine(ShowFeedbackRoutine(target));
    }

    private static IEnumerator ShowFeedbackRoutine(GameObject target)
    {
        target.SetActive(true);
        yield return new WaitForSeconds(0.6f);
        if (target != null) target.SetActive(false);
    }

    private void SetFeedbackObjectsInactive()
    {
        if (great != null) great.SetActive(false);
        if (good != null) good.SetActive(false);
        if (bad != null) bad.SetActive(false);
    }

    private void OnValidate()
    {
        perfectGuardWindow = Mathf.Max(0.01f, perfectGuardWindow);
        guardWindow = Mathf.Max(0.01f, guardWindow);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        guardActivationCost = Mathf.Max(0f, guardActivationCost);
        guardPenalty = Mathf.Max(0f, guardPenalty);
        guardBreakStunDuration = Mathf.Max(0f, guardBreakStunDuration);
        maxGauge = Mathf.Max(1f, maxGauge);
        currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
        /* 패링 히트 게이지 검증 비활성화
        maxParryHitGauge = Mathf.Max(1f, maxParryHitGauge);
        parryHitGauge = Mathf.Clamp(parryHitGauge, 0f, maxParryHitGauge);
        */
    }
}
