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
    public float gizmoDuration = 2.0f;

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
        StartCoroutine(GizmoTimer(position, radius));

        if (defaultExplosionEffect != null)
        {
            Instantiate(defaultExplosionEffect, position, Quaternion.identity);
        }

        Collider2D[] targets = Physics2D.OverlapCircleAll(position, radius);
        foreach (var target in targets)
        {
            if (target.CompareTag("Player")) continue;
            ApplyDamage(target, damage, debuffAmount, debuffDuration);
        }
    }

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

        float finalDamage = baseDamage * ampMult;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(finalDamage, gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

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
        else
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 1.7f);
        }
    }
}