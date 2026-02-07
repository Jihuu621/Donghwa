using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class ChessPiece : MonoBehaviour
{
    public float rotateWhileFalling = 180f;
    public float deactivateDelay = 2f;
    private bool hasLanded = false;

    private Rigidbody2D rb;
    private Collider2D col; 
    public Action onDeactivate;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>(); 
    }

    void OnEnable()
    {
        hasLanded = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 10f;

            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            rb.constraints = RigidbodyConstraints2D.None;
        }

        if (col != null) col.enabled = true;
    }

    void Update()
    {
        if (!hasLanded && rotateWhileFalling != 0f)
        {
            transform.Rotate(Vector3.forward, rotateWhileFalling * Time.deltaTime);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;

        hasLanded = true;

        StopPhysics();

        Invoke(nameof(Deactivate), deactivateDelay);
    }

    void StopPhysics()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.bodyType = RigidbodyType2D.Kinematic;

        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    void Deactivate()
    {
        gameObject.SetActive(false);
        onDeactivate?.Invoke();
    }
}