using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
public class NeedleThreadTrap : MonoBehaviour
{
    private LineRenderer line;
    private EdgeCollider2D edgeCol;
    private float threadDamage;
    private GameObject playerSource;

    private NeedleProjectile node1;
    private NeedleProjectile node2;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        edgeCol = GetComponent<EdgeCollider2D>();

        edgeCol.isTrigger = true;
    }

    public void Setup(NeedleProjectile n1, NeedleProjectile n2, float damage, GameObject source)
    {
        node1 = n1;
        node2 = n2;
        threadDamage = damage;
        playerSource = source;

        line.positionCount = 2;
        line.SetPosition(0, n1.transform.position);
        line.SetPosition(1, n2.transform.position);

        transform.position = n1.transform.position;
        List<Vector2> points = new List<Vector2>
        {
            Vector2.zero,
            transform.InverseTransformPoint(n2.transform.position)
        };
        edgeCol.SetPoints(points);

        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) return;

        IDamageable target = collision.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(threadDamage, playerSource);
            Debug.Log($"<color=cyan>[바늘 실 함정]</color> 적 적중! {threadDamage} 데미지!");
        }
    }

    private void OnDestroy()
    {
        if (node1 != null) node1.ReturnToPool();
        if (node2 != null) node2.ReturnToPool();
    }
}