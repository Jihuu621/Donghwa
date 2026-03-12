using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 체스 기물 프리팹을 위한 오브젝트 풀 관리자.
/// 런타임 Instantiate/Destroy를 방지한다.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    // 프리팹별 풀을 딕셔너리로 관리
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

    /// <summary>
    /// 특정 프리팹에 대한 풀을 초기화한다.
    /// </summary>
    public void InitializePool(GameObject prefab, int size, Transform parent = null)
    {
        if (prefab == null || pools.ContainsKey(prefab)) return;

        Queue<GameObject> queue = new Queue<GameObject>(size);
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, parent != null ? parent : transform);
            obj.SetActive(false);
            queue.Enqueue(obj);
            instanceToPrefab[obj] = prefab;
        }
        pools[prefab] = queue;
    }

    /// <summary>
    /// 풀에서 비활성 오브젝트를 꺼내 활성화 후 반환한다.
    /// 풀이 비어 있으면 새로 생성한다.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab))
        {
            InitializePool(prefab, 4);
        }

        Queue<GameObject> queue = pools[prefab];
        GameObject obj;

        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            // 풀 부족 시 동적 확장
            obj = Instantiate(prefab, transform);
            instanceToPrefab[obj] = prefab;
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 사용 완료된 오브젝트를 풀로 반환한다.
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        if (instanceToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            if (pools.ContainsKey(prefab))
            {
                pools[prefab].Enqueue(obj);
            }
        }
    }
}