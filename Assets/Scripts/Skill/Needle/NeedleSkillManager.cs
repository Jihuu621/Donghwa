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

    [Header("함정(실) 지속 데미지 설정")]
    public float threadDamage = 10f; // 틱당 데미지 (수치를 조금 낮추는 걸 추천합니다)
    public float threadTickInterval = 0.5f; // 0.5초마다 지속 데미지

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

            Debug.Log($"<color=lime>[바늘 액션]</color> {groundNeedles.Count}개의 바늘을 연쇄 연결합니다!");

            float trapDuration = 5f;

            foreach (var needle in groundNeedles)
            {
                needle.SetAsTrapNode(trapDuration);
            }

            for (int i = 0; i < groundNeedles.Count - 1; i++)
            {
                NeedleProjectile n1 = groundNeedles[i];
                NeedleProjectile n2 = groundNeedles[i + 1];

                GameObject trapObj = Instantiate(threadTrapPrefab);
                NeedleThreadTrap trapScript = trapObj.GetComponent<NeedleThreadTrap>();

                if (trapScript != null)
                {
                    //  Setup 함수에 틱 간격(threadTickInterval)을 추가로 넘겨줍니다.
                    trapScript.Setup(n1.transform.position, n2.transform.position, threadDamage, threadTickInterval, gameObject, trapDuration);
                }
            }
        }
    }
}