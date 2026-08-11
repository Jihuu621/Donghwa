using UnityEngine;
using System.Collections.Generic;

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

    [Header("적 바늘 그래플")]
    public float grappleAcceleration = 120f;
    public float grappleMaxSpeed = 28f;
    public float grappleReleaseDistance = 1.2f;
    public float grappleMaxDuration = 1.1f;
    [Range(0f, 1f)] public float grappleGravityMultiplier = 0.35f;
    public float grappleMomentumDuration = 0.18f;

    [Header("함정(실) 지속 데미지 설정")]
    public float threadDamage = 10f; // 틱당 데미지 (수치를 조금 낮추는 걸 추천합니다)
    public float threadTickInterval = 0.5f; // 0.5초마다 지속 데미지

    [Header("프리팹 참조")]
    public Transform firePoint;
    public SimplePool needlePool;
    public GameObject threadTrapPrefab;

    private float lastFireTime = -999f;
    private List<NeedleProjectile> activeNeedles = new List<NeedleProjectile>();
    private Rigidbody2D playerRigidbody;
    private PlayerController playerController;
    private EffectManager playerStatusEffects;
    private NeedleProjectile grappleNeedle;
    private float grappleTimer;
    private float originalGravityScale;
    private bool isGrappling;

    private void Awake()
    {
        Instance = this;
        needlePool = GetComponent<SimplePool>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        playerStatusEffects = GetComponent<EffectManager>();
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

    private void FixedUpdate()
    {
        if (!isGrappling) return;
        if (grappleNeedle == null || !grappleNeedle.gameObject.activeInHierarchy ||
            (playerStatusEffects != null && playerStatusEffects.BlocksMovement))
        {
            EndGrapple(false);
            return;
        }

        Vector2 toNeedle = (Vector2)grappleNeedle.transform.position - playerRigidbody.position;
        float distance = toNeedle.magnitude;
        grappleTimer += Time.fixedDeltaTime;
        if (distance <= grappleReleaseDistance || grappleTimer >= grappleMaxDuration)
        {
            EndGrapple(true);
            return;
        }

        Vector2 direction = toNeedle / distance;
        float pullSpeed = Vector2.Dot(playerRigidbody.linearVelocity, direction);
        if (pullSpeed < grappleMaxSpeed)
        {
            playerRigidbody.AddForce(direction * grappleAcceleration, ForceMode2D.Force);
        }

        if (playerRigidbody.linearVelocity.sqrMagnitude > grappleMaxSpeed * grappleMaxSpeed)
        {
            playerRigidbody.linearVelocity = playerRigidbody.linearVelocity.normalized * grappleMaxSpeed;
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
        if (isGrappling) return;
        activeNeedles.RemoveAll(n => n == null || !n.gameObject.activeInHierarchy);

        NeedleProjectile enemyNeedle = activeNeedles.Find(n => n.currentState == NeedleProjectile.NeedleState.StuckInEnemy);
        if (enemyNeedle != null)
        {
            StartGrapple(enemyNeedle);
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

    private void StartGrapple(NeedleProjectile targetNeedle)
    {
        if (playerRigidbody == null || targetNeedle == null) return;

        grappleNeedle = targetNeedle;
        grappleTimer = 0f;
        isGrappling = true;
        originalGravityScale = playerRigidbody.gravityScale;
        playerRigidbody.gravityScale = originalGravityScale * grappleGravityMultiplier;
        playerController?.SetExternalMovementOverride(true);
    }

    private void EndGrapple(bool consumeNeedle)
    {
        if (!isGrappling) return;

        isGrappling = false;
        if (playerRigidbody != null) playerRigidbody.gravityScale = originalGravityScale;
        if (consumeNeedle) playerController?.ReleaseExternalMovementOverride(grappleMomentumDuration);
        else playerController?.SetExternalMovementOverride(false);

        NeedleProjectile completedNeedle = grappleNeedle;
        grappleNeedle = null;
        if (consumeNeedle && completedNeedle != null && completedNeedle.gameObject.activeInHierarchy)
        {
            completedNeedle.ReturnToPool();
        }
    }

    private void OnDisable()
    {
        EndGrapple(false);
    }
}
