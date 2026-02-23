using UnityEngine;

public class RopeCutter : MonoBehaviour
{
    public TrailRenderer trail; // 에디터에서 MouseTrail 오브젝트를 넣어주세요
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
                trail.emitting = true; // 선 그리기 시작
                trail.transform.position = currentMousePosition; // 마우스 위치로 순간이동
                trail.Clear(); // 이전 잔상 삭제
            }
        }

        // 오른쪽 클릭 유지 중
        if (Input.GetMouseButton(1))
        {
            // 궤적 오브젝트를 마우스 위치로 계속 이동시킴
            if (trail != null)
            {
                trail.transform.position = currentMousePosition;
            }

            // 줄 자르기 판정 (Linecast)
            RaycastHit2D[] hits = Physics2D.LinecastAll(lastMousePosition, currentMousePosition);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Rope"))
                {
                    Destroy(hit.collider.gameObject);
                }
            }
        }

        // 오른쪽 클릭을 뗐을 때
        if (Input.GetMouseButtonUp(1))
        {
            if (trail != null) trail.emitting = false; // 선 그리기 중단
        }

        lastMousePosition = currentMousePosition;
    }
}