using UnityEngine;
using System.Collections;

public class PlayerParry : MonoBehaviour
{
    [Header("패링/가드 구간 설정")]
    public float failWindow = 0.7f;
    public float parryWindow = 1.0f;
    public float guardWindow = 1.0f;

    [Header("패링/가드 옵션")]
    public float parryCooldown = 0.5f;
    public float guardDamageReduce = 0.6f;

    [Header("패링 게이지")]
    public float maxGauge = 100f;
    public float currentGauge;
    public float gaugeRegenPerSec = 1f;
    public float parryReward = 5f;
    public float guardPenalty = 20f;

    [Header("패링 히트 시스템")]
    public float parryHitGauge = 0f;
    public float maxParryHitGauge = 600f;
    public float parryHitGain = 30f;
    public float parryHitDecay = 10f;
    private float parryHitTimer = 0f; 
    private float lastParryHitTime = 0f; 

    private bool isFailTime = false;
    private bool isParryTime = false;
    private bool isGuardTime = false;
    private bool isCooldown = false;
    private bool isStunned = false;

    public bool IsFailTime => isFailTime;
    public bool IsParryTime => isParryTime;
    public bool IsGuardTime => isGuardTime;
    public bool IsStunned => isStunned;

    void Start()
    {
        currentGauge = maxGauge;
    }

    void Update()
    {
        if (isStunned) return;

        currentGauge += gaugeRegenPerSec * Time.deltaTime;
        currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);

        //패링 히트 게이지 감소 시스템
        if (Time.time - lastParryHitTime > 2f)
        {
            if (parryHitGauge > 0f)
                parryHitGauge -= parryHitDecay * Time.deltaTime;

            if (parryHitGauge < 0f)
                parryHitGauge = 0f;
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
            TryActivateParryGuard();
    }

    void TryActivateParryGuard()
    {
        if (isCooldown) return;
        if (isStunned) return;
        if (currentGauge < 5f) return;

        currentGauge -= 5f;

        StartCoroutine(ParryGuardRoutine());
        StartCoroutine(CooldownRoutine());
    }

    IEnumerator ParryGuardRoutine()
    {
        isFailTime = true;
        isParryTime = false;
        isGuardTime = false;
        yield return new WaitForSeconds(failWindow);

        isFailTime = false;
        isParryTime = true;
        yield return new WaitForSeconds(parryWindow);

        isParryTime = false;
        isGuardTime = true;
        yield return new WaitForSeconds(guardWindow);

        isGuardTime = false;
    }

    IEnumerator CooldownRoutine()
    {
        isCooldown = true;
        yield return new WaitForSeconds(parryCooldown);
        isCooldown = false;
    }

    IEnumerator StunRoutine()
    {
        isStunned = true;
        yield return new WaitForSeconds(1f);
        isStunned = false;
    }

    public float OnHit(float damage)
    {
        RestZoneSkill rest = GetComponent<RestZoneSkill>();
        if (rest != null && rest.IsZoneActive && rest.IsPlayerInsideZone())
        {
            Debug.Log("<color=cyan>[스킬이름이 어떻게 휴식시간]</color> 구역 안 무적! (피해 0)");
            return 0f;
        }

        // 패링 실패 구간 (그대로 맞음)
        if (isFailTime)
        {
            Debug.Log("<color=grey>[플레이어] 패링 실패 구간 — 데미지 그대로</color>");
            return damage;
        }

        // 완벽한 패링
        if (isParryTime)
        {
            Debug.Log("<color=lime>[플레이어] 완벽한 패링 성공! 데미지 0 / 패링 히트 +30 / 게이지 +5</color>");

            currentGauge = Mathf.Min(currentGauge + parryReward, maxGauge);

            parryHitGauge = Mathf.Min(parryHitGauge + parryHitGain, maxParryHitGauge);

            //패링 히트 타이머 초기화
            lastParryHitTime = Time.time;

            return 0f;
        }

        // 가드
        if (isGuardTime)
        {
            float reduced = damage * (1f - guardDamageReduce);
            currentGauge -= guardPenalty;

            Debug.Log($"<color=yellow>[플레이어] 가드 성공! 피해 {reduced} / 게이지 -20</color>");

            if (currentGauge <= 0f)
            {
                currentGauge = 0f;
                Debug.Log("<color=purple>[플레이어] 가드 게이지 소진 → 기절!</color>");
                StartCoroutine(StunRoutine());
            }

            return reduced;
        }

        // 일반 피격
        Debug.Log("<color=white>[플레이어] 일반 피격</color>");
        return damage;
    }
}
