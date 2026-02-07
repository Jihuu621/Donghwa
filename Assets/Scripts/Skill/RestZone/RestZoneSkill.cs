using UnityEngine;

public class RestZoneSkill : MonoBehaviour
{
    [Header("휴식구역")]
    public float zoneRadius = 2.5f;
    public float zoneDuration = 2f;
    public KeyCode castKey = KeyCode.E;

    [Header("ㅎㅅㄱㅇ프리팹")]
    public GameObject zoneVisualPrefab;

    [Header("받는 피해 증가")]
    public LayerMask enemyLayer;
    public float ampDuration = 3f;
    public float damageAmpMultiplier = 1.5f;

    [Header("디버그")]
    public bool drawGizmo = true;

    float zoneEndTime = -999f;
    Vector2 zoneCenter;
    GameObject visual;

    public bool IsZoneActive => Time.time < zoneEndTime;
    public Vector2 ZoneCenter => zoneCenter;
    public float ZoneRadius => zoneRadius;

    void Update()
    {
        if (Input.GetKeyDown(castKey))
            Cast();

        if (IsZoneActive)
            ApplyEffectsToEnemiesInside();
        else
            CleanupVisualIfNeeded();
    }

    void Cast()
    {
        zoneCenter = transform.position;
        zoneEndTime = Time.time + zoneDuration;

        if (visual != null) Destroy(visual);

        if (zoneVisualPrefab != null)
        {
            visual = Instantiate(zoneVisualPrefab, zoneCenter, Quaternion.identity);
            float diameter = zoneRadius * 2f;
            visual.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        Debug.Log($"<color=cyan>[휴식시간]</color> 구역 생성! 지속 {zoneDuration:0.0}s");
    }

    void ApplyEffectsToEnemiesInside()
    {
        var hits = Physics2D.OverlapCircleAll(zoneCenter, zoneRadius, enemyLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            var runner = hits[i].GetComponent<EnemyStatusEffectRunner>();
            if (runner == null) runner = hits[i].gameObject.AddComponent<EnemyStatusEffectRunner>();

            runner.AddEffect(new DamageAmpEffect(damageAmpMultiplier, ampDuration));
        }
    }

    public bool IsPlayerInsideZone()
    {
        if (!IsZoneActive) return false;
        return Vector2.Distance(transform.position, zoneCenter) <= zoneRadius;
    }

    void CleanupVisualIfNeeded()
    {
        if (visual != null)
        {
            Destroy(visual);
            visual = null;
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        if (!Application.isPlaying) return;
        if (!IsZoneActive) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(zoneCenter, zoneRadius);
    }
}
