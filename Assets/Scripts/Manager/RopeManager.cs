using UnityEngine;

public class RopeManager : MonoBehaviour
{
    public LayerMask interactableLayer;
    public GameObject ropePrefab;           // 발판용 실 프리팹
    public GameObject collisionEffectPrefab; // 충돌 시 터질 파티클 프리팹

    private GameObject firstSelected;

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // 우클릭으로 선택
        {
            HandleSelection();
        }
    }

    // RopeManager.cs 내부 수정
    void HandleSelection()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, interactableLayer);

        if (hit.collider != null)
        {
            GameObject selected = hit.collider.gameObject;

            if (firstSelected == null)
            {
                firstSelected = selected;
                // [추가] 첫 번째 선택 시 초록색으로 변경
                SetObjectColor(firstSelected, Color.green);
            }
            else if (firstSelected != selected)
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    ExecuteCentralAttack(firstSelected, selected);
                }
                else
                {
                    CreateRopeBridge(firstSelected, selected);
                }

                // [추가] 작업 완료 후 색상 복구 및 초기화
                ResetObjectColor(firstSelected);
                firstSelected = null;
            }
        }
        else
        {
            if (firstSelected != null) ResetObjectColor(firstSelected);
            firstSelected = null;
        }
    }

    // 색상 변경 함수
    void SetObjectColor(GameObject obj, Color color)
    {
        var sprite = obj.GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = color;
    }

    // 색상 복구 함수 (흰색으로 복구)
    void ResetObjectColor(GameObject obj)
    {
        var sprite = obj.GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = Color.white;
    }

    //중앙 충돌 공격
    void ExecuteCentralAttack(GameObject a, GameObject b)
    {
        Vector2 centerPoint = Vector2.Lerp(a.transform.position, b.transform.position, 0.5f);

       a.AddComponent<CentralPull>().Setup(centerPoint, collisionEffectPrefab);
       b.AddComponent<CentralPull>().Setup(centerPoint, collisionEffectPrefab);
    }

    //실 발판 만들기
    void CreateRopeBridge(GameObject a, GameObject b)
    {
        GameObject rope = Instantiate(ropePrefab);
        rope.GetComponent<RopeBridge>().Setup(a.transform, b.transform);
    }
}