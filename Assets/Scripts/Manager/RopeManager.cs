using System.Collections.Generic;
using UnityEngine;

public class RopeManager : MonoBehaviour
{
    public LayerMask interactableLayer;
    public GameObject ropePrefab;
    public GameObject collisionEffectPrefab;

    private GameObject firstSelected;
    private List<RopeBridge> activeBridges = new List<RopeBridge>();

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            HandleSelection();
        }

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
            // [수정] transform.root 대신 Rigidbody2D를 가진 실제 블록(또는 합체 C 블록)만 탐색
            GameObject selected = GetTargetObject(hit.collider);

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

    // 부모 방향으로 탐색하며 Rigidbody2D를 소유한 최상위 합체 오브젝트를 반환
    GameObject GetTargetObject(Collider2D col)
    {
        Rigidbody2D rb = col.GetComponentInParent<Rigidbody2D>();
        return rb != null ? rb.gameObject : col.gameObject;
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
                    if (a.GetComponent<CentralPull>() == null)
                        a.AddComponent<CentralPull>().Setup(b.transform, collisionEffectPrefab);

                    if (b.GetComponent<CentralPull>() == null)
                        b.AddComponent<CentralPull>().Setup(a.transform, collisionEffectPrefab);
                }
                Destroy(bridge.gameObject);
            }
        }
        activeBridges.Clear();
    }

    void SetObjectColor(GameObject obj, Color color)
    {
        SpriteRenderer[] sprites = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sprite in sprites) sprite.color = color;
    }

    void ResetObjectColor(GameObject obj)
    {
        SpriteRenderer[] sprites = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sprite in sprites) sprite.color = Color.white;
    }
}