using System.Collections.Generic;
using UnityEngine;

public class RopeManager : MonoBehaviour
{
    public LayerMask interactableLayer;
    public GameObject ropePrefab;
    public GameObject collisionEffectPrefab;

    [Header("합체 설정")]
    [SerializeField, Min(0.1f)] private float pullSpeed = 20f;
    // Collider 경계는 맞아도 스프라이트의 투명 여백 때문에 화면상 틈이 보일 수 있다.
    [SerializeField, Min(0f)] private float horizontalOverlap = 0.025f;

    private GameObject firstSelected;
    private List<RopeBridge> activeBridges = new List<RopeBridge>();

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            HandleSelection();
        }

        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
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
            GameObject selected = GetTargetObject(hit.collider);

            if (selected == null || selected.GetComponent<CentralPull>() != null)
            {
                ClearSelection();
                return;
            }

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
            ClearSelection();
        }
    }

    // 합체 직후에는 자식 Rigidbody2D의 Destroy가 한 프레임 늦게 반영될 수 있다.
    // 클릭한 콜라이더의 활성 Rigidbody를 우선 사용하고, 필요하면 부모 방향으로 찾는다.
    GameObject GetTargetObject(Collider2D col)
    {
        Rigidbody2D attachedBody = col.attachedRigidbody;
        if (attachedBody != null && attachedBody.simulated)
        {
            return attachedBody.gameObject;
        }

        Rigidbody2D[] parentBodies = col.GetComponentsInParent<Rigidbody2D>(true);
        foreach (Rigidbody2D body in parentBodies)
        {
            if (body != null && body.simulated)
            {
                return body.gameObject;
            }
        }

        return col.gameObject;
    }

    void CreateRopeBridge(GameObject a, GameObject b)
    {
        if (a == null || b == null || a == b || IsReservedByActiveBridge(a) || IsReservedByActiveBridge(b))
        {
            Debug.LogWarning("[RopeManager] 하나의 블록 그룹에는 한 번에 하나의 합체 연결만 만들 수 있습니다.");
            return;
        }

        if (ropePrefab == null)
        {
            Debug.LogError("[RopeManager] Rope Prefab이 지정되지 않았습니다.");
            return;
        }

        GameObject ropeObj = Instantiate(ropePrefab);
        RopeBridge bridge = ropeObj.GetComponent<RopeBridge>();

        if (bridge == null)
        {
            Debug.LogError("[RopeManager] Rope Prefab에 RopeBridge가 없습니다.");
            Destroy(ropeObj);
            return;
        }

        bridge.Setup(a.transform, b.transform);
        activeBridges.Add(bridge);
    }

    bool IsReservedByActiveBridge(GameObject target)
    {
        foreach (RopeBridge bridge in activeBridges)
        {
            if (bridge != null && (bridge.StartObj == target || bridge.EndObj == target))
            {
                return true;
            }
        }

        return false;
    }

    void ExecuteAllCentralPulls()
    {
        // Alt 전에 남아 있던 반쪽 선택이 합체 후 자식 블록을 직접 가리키지 않도록 비운다.
        ClearSelection();

        for (int i = activeBridges.Count - 1; i >= 0; i--)
        {
            RopeBridge bridge = activeBridges[i];
            if (bridge != null)
            {
                GameObject a = bridge.StartObj;
                GameObject b = bridge.EndObj;

                if (a != null && b != null)
                {
                    bridge.ReleaseForPull();

                    if (!CentralPull.TryStartPair(
                            a,
                            b,
                            collisionEffectPrefab,
                            pullSpeed,
                            -horizontalOverlap))
                    {
                        Debug.LogWarning("[RopeManager] 블록 합체를 시작하지 못했습니다.");
                    }
                }

                Destroy(bridge.gameObject);
            }
        }
        activeBridges.Clear();
    }

    void ClearSelection()
    {
        if (firstSelected != null)
        {
            ResetObjectColor(firstSelected);
        }

        firstSelected = null;
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
