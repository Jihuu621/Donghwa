using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
public class NeedleThreadTrap : MonoBehaviour
{
    private LineRenderer line;
    private EdgeCollider2D edgeCol;

    private float threadDamage;
    private float tickInterval;
    private GameObject playerSource;

    // 적중한 적들을 기억하여 N초마다 쿨타임을 재는 딕셔너리
    private Dictionary<GameObject, float> hitCooldowns = new Dictionary<GameObject, float>();

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        edgeCol = GetComponent<EdgeCollider2D>();
        edgeCol.isTrigger = true;
    }

    public void Setup(Vector3 p1, Vector3 p2, float damage, float interval, GameObject source, float duration)
    {
        threadDamage = damage;
        tickInterval = interval;
        playerSource = source;

        line.positionCount = 2;
        line.SetPosition(0, p1);
        line.SetPosition(1, p2);

        transform.position = p1;
        List<Vector2> points = new List<Vector2>
        {
            Vector2.zero,
            transform.InverseTransformPoint(p2)
        };
        edgeCol.SetPoints(points);

        Destroy(gameObject, duration);
    }

    // OnTriggerEnter 대신 OnTriggerStay2D를 사용하여 머무는 동안 계속 판정합니다.
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) return;

        IDamageable target = collision.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            // IDamageable이 붙어있는 부모 오브젝트를 기준으로 판별
            GameObject targetObj = (target as MonoBehaviour).gameObject;

            // 이미 맞은 적이라면 쿨타임(tickInterval)이 지났는지 확인
            if (hitCooldowns.TryGetValue(targetObj, out float lastHitTime))
            {
                if (Time.time < lastHitTime + tickInterval) return; // 아직 시간이 안 지났으면 패스
            }

            // 데미지 입히기
            target.TakeDamage(threadDamage, playerSource);

            // 찌릿찌릿! 느낌을 내기 위해 아주 짧은 0.15초짜리 스턴을 먹임
            EffectManager effect = targetObj.GetComponent<EffectManager>();
            if (effect != null)
            {
                effect.ApplyStatus(StatusKeyword.Stun, 0.15f, 1f);
            }

            // 맞은 시간 갱신
            hitCooldowns[targetObj] = Time.time;
            Debug.Log($"<color=cyan>[바늘 실 함정]</color> 찌릿! 지속 적중! {threadDamage} 데미지!");
        }
    }
}