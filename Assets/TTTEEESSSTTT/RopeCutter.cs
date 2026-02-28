using UnityEngine;

public class RopeCutter : MonoBehaviour
{
    public TrailRenderer trail;
    private Vector2 lastMousePosition;

    void Start()
    {
        // 시작할 때는 선이 보이지 않게 꺼둡니다.
        if (trail != null) trail.emitting = false;
    }

    void Update()
    {
        Vector2 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 마우스 오른쪽 클릭을 시작할 때
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = currentMousePosition;
            if (trail != null)
            {
                trail.emitting = true;
                trail.transform.position = currentMousePosition;
                trail.Clear();
            }
        }

        // 오른쪽 클릭 유지 중
        if (Input.GetMouseButton(1))
        {
            if (trail != null)
            {
                trail.transform.position = currentMousePosition;
            }

            // 줄 및 투사체 자르기 판정 (Linecast)
            RaycastHit2D[] hits = Physics2D.LinecastAll(lastMousePosition, currentMousePosition);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null)
                {
                    // 1. 기존 로프 자르기
                    if (hit.collider.CompareTag("Rope"))
                    {
                        Destroy(hit.collider.gameObject);
                    }
                    // 2. 적 투사체 자르기
                    else if (hit.collider.CompareTag("Projectile"))
                    {
                        OnCutProjectile(hit.collider.gameObject);
                    }
                }
            }
        }

        // 오른쪽 클릭을 뗐을 때
        if (Input.GetMouseButtonUp(1))
        {
            if (trail != null) trail.emitting = false;
        }

        lastMousePosition = currentMousePosition;
    }

    // 투사체를 잘랐을 때 실행될 별도 함수
    void OnCutProjectile(GameObject projectile)
    {
        // 여기에 애니메이션,파티클
        Destroy(projectile);
        Debug.Log("투사체를 베었습니다!");
    }
}