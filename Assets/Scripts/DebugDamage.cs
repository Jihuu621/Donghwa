using UnityEngine;

public class DebugHealthControl : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            Debug.LogError("Health 컴포넌트를 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        if (health == null) return;

        // K키 누르면 10 데미지
        if (Input.GetKeyDown(KeyCode.K))
        {
            health.TakeDamage(10f);
        }

        // L키 누르면 10 회복
        if (Input.GetKeyDown(KeyCode.L))
        {
            health.Heal(10f);
        }
    }
}
