using UnityEngine;
using System.Collections.Generic;

public class RopeManager : MonoBehaviour
{
    public LayerMask interactableLayer;
    public GameObject ropePrefab;
    public GameObject collisionEffectPrefab;

    private GameObject firstSelected;
    // 현재 필드에 생성된 모든 로프 다리들을 관리하는 리스트
    private List<RopeBridge> activeBridges = new List<RopeBridge>();

    void Update()
    {
        // 1. 우클릭으로 두 오브젝트 선택 -> 줄 연결 (RopeBridge)
        if (Input.GetMouseButtonDown(1))
        {
            HandleSelection();
        }

        // 2. 왼쪽 Alt 키를 누르면 -> 연결된 모든 줄의 물체들을 중앙으로 당김 (CentralPull)
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            ExecuteAllCentralPulls();
        }
    }

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
                SetObjectColor(firstSelected, Color.green);
            }
            else if (firstSelected != selected)
            {
                // 무조건 줄 먼저 생성
                CreateRopeBridge(firstSelected, selected);

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

    void CreateRopeBridge(GameObject a, GameObject b)
    {
        GameObject ropeObj = Instantiate(ropePrefab);
        RopeBridge bridge = ropeObj.GetComponent<RopeBridge>();

        // 중요: 나중에 당기기 위해 어떤 물체들이 연결됐는지 정보를 넘겨줌
        bridge.Setup(a.transform, b.transform);
        activeBridges.Add(bridge);
    }

    void ExecuteAllCentralPulls()
    {
        // 리스트를 역순으로 훑으며 당기기 실행 (삭제를 위해)
        for (int i = activeBridges.Count - 1; i >= 0; i--)
        {
            RopeBridge bridge = activeBridges[i];
            if (bridge != null)
            {
                // bridge에 저장된 시작점과 끝점 정보를 가져와서 당기기 실행
                GameObject a = bridge.StartObj;
                GameObject b = bridge.EndObj;

                if (a != null && b != null)
                {
                    Vector2 centerPoint = Vector2.Lerp(a.transform.position, b.transform.position, 0.5f);

                    if (a.GetComponent<CentralPull>() == null)
                        a.AddComponent<CentralPull>().Setup(centerPoint, collisionEffectPrefab);

                    if (b.GetComponent<CentralPull>() == null)
                        b.AddComponent<CentralPull>().Setup(centerPoint, collisionEffectPrefab);
                }

                // 줄(로프) 자체는 당겨지기 시작할 때 파괴
                Destroy(bridge.gameObject);
            }
        }
        activeBridges.Clear();
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
}