using UnityEngine;
public class RopeCutter : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Rope"))
            {
                // 마디를 파괴하여 줄을 끊음
                Destroy(hit.collider.gameObject);
            }
        }
    }
}