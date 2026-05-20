using UnityEngine;
using System.Collections.Generic;

public class RopeManager : MonoBehaviour
{
    public LayerMask interactableLayer;
    public GameObject ropePrefab;
    public GameObject collisionEffectPrefab;

    private GameObject firstSelected;
    private List<RopeBridge> activeBridges = new List<RopeBridge>();

    void Update()
    {
        // 1. 우클릭으로 두 오브젝트 선택 -> 줄 연결 
        if (Input.GetMouseButtonDown(1))
        {
            HandleSelection();
        }

        // 2. 왼쪽 Alt 키를 누르면 -> 물체들을 중앙으로 당김
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

        bridge.Setup(a.transform, b.transform);
        activeBridges.Add(bridge);
    }

    void ExecuteAllCentralPulls()
    {
        for (int i = activeBridges.Count - 1; i >= 0; i--)
        {
            RopeBridge bridge = activeBridges[i];
            if (bridge != null)
            {
                GameObject a = bridge.StartObj;
                GameObject b = bridge.EndObj;

                if (a != null && b != null)
                {
                    // 즉사 로직 제거됨: 물체가 직접 날아가서 때리도록 냅둡니다.
                    Vector2 centerPoint = Vector2.Lerp(a.transform.position, b.transform.position, 0.5f);

                    if (a.GetComponent<CentralPull>() == null)
                        a.AddComponent<CentralPull>().Setup(centerPoint, collisionEffectPrefab);

                    if (b.GetComponent<CentralPull>() == null)
                        b.AddComponent<CentralPull>().Setup(centerPoint, collisionEffectPrefab);
                }
                Destroy(bridge.gameObject);
            }
        }
        activeBridges.Clear();
    }

    void SetObjectColor(GameObject obj, Color color)
    {
        var sprite = obj.GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = color;
    }

    void ResetObjectColor(GameObject obj)
    {
        var sprite = obj.GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = Color.white;
    }
}