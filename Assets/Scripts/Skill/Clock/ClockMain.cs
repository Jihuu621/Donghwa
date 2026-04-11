using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockMain : MonoBehaviour
{
    [Header("Combo Settings")]
    public float comboLeeway = 1.0f;
    private int comboStep = 0;
    private float lastAttackTime;
    private bool isAttacking = false;
    private bool inputBuffered = false;

    [Header("Combat 2D")]
    public LayerMask enemyLayer;
    public float damage = 10f;
    public float hitRadius = 1.2f;

    [Header("References")]
    public GameObject watchModel;
    public Transform handTransform;
    public ChainPhysics2D chainPhysics;

    // 체인 ropeLength 동기화를 Update 한 곳에서만 관리
    private bool syncChainLength = false;

    void Start()
    {
        if (chainPhysics != null)
        {
            chainPhysics.enabled = false;

            // lineRenderer null 체크 추가
            if (chainPhysics.lineRenderer != null)
                chainPhysics.lineRenderer.enabled = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (!isAttacking) StartAttack();
            else inputBuffered = true;
        }

        // 콤보 리셋: 공격 중이 아닐 때만 타이머 체크
        if (!isAttacking && Time.time - lastAttackTime > comboLeeway)
        {
            comboStep = 0;
        }

        // 체인 길이 동기화를 Update 한 곳에서만 처리 (중복 제거)
        if (syncChainLength && chainPhysics != null && chainPhysics.enabled)
        {
            chainPhysics.ropeLength = Vector2.Distance(handTransform.position, watchModel.transform.position);
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        inputBuffered = false;

        syncChainLength = true;

        if (chainPhysics != null)
        {
            chainPhysics.tightness = 0.3f;
            chainPhysics.enabled = true;

            if (chainPhysics.lineRenderer != null)
                chainPhysics.lineRenderer.enabled = true;

            watchModel.transform.position = handTransform.position;
        }

        switch (comboStep)
        {
            case 0: StartCoroutine(ComboA_Elevator()); break;
            case 1: StartCoroutine(ComboB_AroundWorld()); break;
            case 2: StartCoroutine(ComboC_SkyRocket()); break;
        }
    }

    // --- 1타: 아래에서 수평까지 올리기 ---
    // 수정: y + radius 제거 → 손 위치 기준 올바른 원호 궤적으로 수정
    // 수정: 모션 중 매 프레임 히트 판정 추가 (HashSet으로 중복 방지)
    IEnumerator ComboA_Elevator()
    {
        float elapsed = 0;
        float attackDuration = 0.18f;
        float returnDuration = 0.07f;
        float radius = 2.0f;
        float lookDir = transform.localScale.x > 0 ? 1f : -1f;
        HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

        while (elapsed < attackDuration)
        {
            float t = elapsed / attackDuration;
            float curveT = Mathf.Sin(t * Mathf.PI * 0.5f);

            float startAngle = -Mathf.PI / 2f;
            float endAngle = 0f;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, curveT);

            float x = Mathf.Cos(currentAngle) * radius * lookDir * 1.5f;
            float y = Mathf.Sin(currentAngle) * radius;

            // [버그 수정] y + radius 제거 → 올바른 원호 궤적
            watchModel.transform.position = (Vector2)handTransform.position + new Vector2(x, y);

            // [버그 수정] 매 프레임 히트 판정 (모션 도중 지나치는 적도 타격)
            Collider2D[] hits = Physics2D.OverlapCircleAll(watchModel.transform.position, hitRadius, enemyLayer);
            foreach (var hit in hits)
            {
                if (alreadyHit.Add(hit)) ApplyDamage(hit);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 빠른 회수
        elapsed = 0;
        Vector2 endPos = watchModel.transform.position;
        while (elapsed < returnDuration)
        {
            float t = elapsed / returnDuration;
            watchModel.transform.position = Vector2.Lerp(endPos, handTransform.position, t * t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        OnAttackComplete();
    }

    // --- 2타: 돌리기 ---
    // 수정: transform.position → handTransform.position 기준으로 통일
    IEnumerator ComboB_AroundWorld()
    {
        float duration = 0.25f, elapsed = 0;
        HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

        while (elapsed < duration)
        {
            float angle = (elapsed / duration) * Mathf.PI * 2.05f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.0f;

            // [버그 수정] transform.position → handTransform.position으로 통일
            watchModel.transform.position = (Vector2)handTransform.position + offset;

            Collider2D[] hits = Physics2D.OverlapCircleAll(watchModel.transform.position, hitRadius * 0.8f, enemyLayer);
            foreach (var hit in hits)
            {
                if (alreadyHit.Add(hit)) ApplyDamage(hit);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 회수: 마지막 위치에서 손으로 부드럽게 복귀
        float returnDuration = 0.1f;
        elapsed = 0;
        Vector2 returnStart = watchModel.transform.position;
        while (elapsed < returnDuration)
        {
            float t = elapsed / returnDuration;
            watchModel.transform.position = Vector2.Lerp(returnStart, handTransform.position, t * t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        syncChainLength = false;
        OnAttackComplete();
    }

    // --- 3타: 전방 투척 및 실시간 회수 ---
    IEnumerator ComboC_SkyRocket()
    {
        float lookDir = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 targetPos = (Vector2)transform.position + new Vector2(lookDir * 7.0f, 0);

        // 1. 던지기
        yield return MoveWatch(handTransform.position, targetPos, 0.15f);

        // 2. 타격 판정
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, hitRadius * 1.5f, enemyLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits) ApplyDamage(hit);
            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(0.1f);

        // 3. 회수 (Cubic Ease-In: 처음엔 느리고 끝에 빠르게)
        if (chainPhysics != null) chainPhysics.tightness = 1.0f;
        float elapsed = 0;
        float recallDuration = 0.1f;
        Vector2 recallStart = watchModel.transform.position;

        while (elapsed < recallDuration)
        {
            float t = elapsed / recallDuration;
            float tightT = t * t * t;
            watchModel.transform.position = Vector2.Lerp(recallStart, handTransform.position, tightT);
            elapsed += Time.deltaTime;
            yield return null;
        }

        syncChainLength = false;
        OnAttackComplete();
    }

    // --- 공통 유틸리티 ---
    IEnumerator MoveWatch(Vector2 start, Vector2 end, float time)
    {
        float elapsed = 0;
        while (elapsed < time)
        {
            watchModel.transform.position = Vector2.Lerp(start, end, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        watchModel.transform.position = end;
    }

    void ApplyDamage(Collider2D enemy)
    {
        Debug.Log($"{enemy.name} 타격! 데미지: {damage}");
    }

    void OnAttackComplete()
    {
        // [버그 수정] comboStep을 먼저 증가시킨 뒤 isAttacking = false 처리
        // → 리셋 타이머(comboLeeway)가 Update에서 즉시 0으로 덮어쓰는 것을 방지
        comboStep = (comboStep + 1) % 3;
        lastAttackTime = Time.time;

        isAttacking = false;
        syncChainLength = false;

        watchModel.transform.position = handTransform.position;

        if (chainPhysics != null)
        {
            chainPhysics.enabled = false;

            if (chainPhysics.lineRenderer != null)
                chainPhysics.lineRenderer.enabled = false;
        }

        // [버그 수정] inputBuffered 소비 후 StartAttack 호출
        // StartAttack 내부에서 inputBuffered = false를 다시 세팅하지 않도록
        // 버퍼 확인을 OnAttackComplete에서만 담당
        if (inputBuffered)
        {
            inputBuffered = false;
            StartAttack();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (watchModel != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(watchModel.transform.position, hitRadius);
        }
    }
}