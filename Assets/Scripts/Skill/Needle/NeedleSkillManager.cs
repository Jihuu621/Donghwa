using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(SimplePool))]
public class NeedleSkillManager : MonoBehaviour
{
    public static NeedleSkillManager Instance;

    [Header("스킬 설정")]
    public float cooldown = 1f;
    public float throwSpeed = 30f;

    [Header("데미지 및 효과 설정")]
    public float needleDamage = 15f;
    public float stunDuration = 1.5f;
    public float stunValue = 1f;
    public float knockbackForce = 3f;
    public float threadDamage = 30f;

    [Header("프리팹 참조")]
    public Transform firePoint;
    public SimplePool needlePool;
    public GameObject threadTrapPrefab;

    private float lastFireTime = -999f;
    private List<NeedleProjectile> activeNeedles = new List<NeedleProjectile>();

    private void Awake()
    {
        Instance = this;
        needlePool = GetComponent<SimplePool>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TryThrowNeedle();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ExecuteAction();
        }
    }

    private void TryThrowNeedle()
    {
        if (Time.time < lastFireTime + cooldown) return;

        lastFireTime = Time.time;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector2 direction = (mousePos - firePoint.position).normalized;

        GameObject needleObj = needlePool.Get(firePoint.position, Quaternion.identity);
        NeedleProjectile needle = needleObj.GetComponent<NeedleProjectile>();

        needle.Launch(direction, throwSpeed, needleDamage, stunDuration, stunValue, knockbackForce, gameObject);
    }

    public void RegisterNeedle(NeedleProjectile needle)
    {
        if (!activeNeedles.Contains(needle)) activeNeedles.Add(needle);
    }

    public void UnregisterNeedle(NeedleProjectile needle)
    {
        if (activeNeedles.Contains(needle)) activeNeedles.Remove(needle);
    }

    private void ExecuteAction()
    {
        activeNeedles.RemoveAll(n => n == null || !n.gameObject.activeInHierarchy);

        NeedleProjectile enemyNeedle = activeNeedles.Find(n => n.currentState == NeedleProjectile.NeedleState.StuckInEnemy);
        if (enemyNeedle != null)
        {
            Debug.Log("<color=lime>[바늘 액션]</color> 적에게 날아갑니다!");
            transform.DOMove(enemyNeedle.transform.position, 0.25f).SetEase(Ease.OutQuad);
            enemyNeedle.ReturnToPool();
            return;
        }

        List<NeedleProjectile> groundNeedles = activeNeedles.FindAll(n => n.currentState == NeedleProjectile.NeedleState.StuckInGround);
        if (groundNeedles.Count >= 2)
        {
            if (threadTrapPrefab == null)
            {
                Debug.LogError("[에러] Thread Trap Prefab이 할당되지 않았습니다!");
                return;
            }

            Debug.Log("<color=lime>[바늘 액션]</color> 두 바늘을 연결하여 함정을 만듭니다!");

            NeedleProjectile n1 = groundNeedles[0];
            NeedleProjectile n2 = groundNeedles[1];

            n1.SetAsTrapNode();
            n2.SetAsTrapNode();

            GameObject trapObj = Instantiate(threadTrapPrefab);
            NeedleThreadTrap trapScript = trapObj.GetComponent<NeedleThreadTrap>();

            if (trapScript != null)
            {
                trapScript.Setup(n1, n2, threadDamage, gameObject);
            }
        }
    }
}