using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    void Update()
    {
        transform.Translate(Vector2.left * speed * Time.deltaTime);
    }
}