using UnityEngine;
using System.Collections;

public class ExplosionManager : MonoBehaviour
{
    public static ExplosionManager Instance { get; private set; }

    [Header("Visual Effects")]
    public GameObject defaultExplosionEffect;

    [Header("Debug Settings (Editor Only)")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(1, 0, 0, 0.3f);
    public float gizmoDuration = 2.0f; // 기즈모가 화면에 유지될 시간 (초)

    private Vector3 lastExplosionPos;
    private float lastRadius = 0f;
    private bool isDrawingGizmo = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void CreateExplosion(Vector3 position, float radius, float damage, float debuffAmount, float debuffDuration)
    {
        // 기즈모 표시 데이터 설정 및 타이머 시작
        StartCoroutine(GizmoTimer(position, radius));

        // 1. 시각 효과 생성
        if (defaultExplosionEffect != null)
        {
            Instantiate(defaultExplosionEffect, position, Quaternion.identity);
        }

        // 2. 물리 판정
        Collider2D[] targets = Physics2D.OverlapCircleAll(position, radius);
        foreach (var target in targets)
        {
            if (target.CompareTag("Player")) continue;
            ApplyDamage(target, damage, debuffAmount, debuffDuration);
        }
    }

    // 일정 시간 동안만 기즈모를 그리도록 제어하는 코루틴
    private IEnumerator GizmoTimer(Vector3 pos, float radius)
    {
        lastExplosionPos = pos;
        lastRadius = radius;
        isDrawingGizmo = true;

        yield return new WaitForSeconds(gizmoDuration);

        isDrawingGizmo = false;
        lastRadius = 0f;
    }

    private void ApplyDamage(Collider2D other, float baseDamage, float debuffAmount, float debuffDuration)
    {
        float ampMult = 1f;
        var amp = other.GetComponent<EnemyDamageAmpData>();
        if (amp != null) ampMult = amp.Multiplier;

        int finalDamage = Mathf.RoundToInt(baseDamage * ampMult);

        var shield = other.GetComponentInChildren<ShieldController>();
        if (shield != null && !shield.IsBroken)
        {
            shield.TakeShieldDamage(finalDamage);
            return;
        }

        var sEnemy = other.GetComponentInParent<ShieldEnemyManager>();
        if (sEnemy != null) sEnemy.TakeDamage(finalDamage);

        var health = other.GetComponent<Health>();
        if (health != null) health.TakeDamage(finalDamage);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 게임 실행 중일 때: 폭발이 발생한 지점에 지정된 시간 동안만 그림
        if (Application.isPlaying)
        {
            if (isDrawingGizmo)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawSphere(lastExplosionPos, lastRadius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(lastExplosionPos, lastRadius);
            }
        }
        // 에디터 모드일 때: 매니저 위치에 기본 가이드라인 표시
        else
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 1.7f);
        }
    }
}