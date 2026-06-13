using UnityEngine;
using System.Collections.Generic;

public class SimplePool : MonoBehaviour
{
    public GameObject prefab;
    public int initialPoolSize = 10;

    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewObject();
        }
    }

    private GameObject CreateNewObject()
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        pool.Enqueue(obj);
        return obj;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        if (pool.Count == 0) CreateNewObject();

        GameObject obj = pool.Dequeue();

        obj.transform.SetParent(null);

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pool.Enqueue(obj);
    }
}