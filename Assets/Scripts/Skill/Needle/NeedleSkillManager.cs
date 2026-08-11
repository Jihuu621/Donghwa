using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SimplePool))]
public class NeedleSkillManager : MonoBehaviour
{
    public static NeedleSkillManager Instance;

    [Header("Throw Settings")]
    public float cooldown = 1f;
    public float throwSpeed = 30f;

    [Header("Damage and Effects")]
    public float needleDamage = 15f;
    public float stunDuration = 1.5f;
    public float stunValue = 1f;
    public float knockbackForce = 3f;

    [Header("Enemy Needle Grapple")]
    public float grappleAcceleration = 120f;
    public float grappleMaxSpeed = 28f;
    public float grappleReleaseDistance = 1.2f;
    public float grappleMaxDuration = 1.1f;
    [Range(0f, 1f)] public float grappleGravityMultiplier = 0.35f;
    public float grappleMomentumDuration = 0.18f;

    [Header("Thread Trap Damage")]
    public float threadDamage = 10f;
    public float threadTickInterval = 0.5f;

    [Header("Prefab References")]
    public Transform firePoint;
    public SimplePool needlePool;
    public GameObject threadTrapPrefab;

    [Header("Perfect Guard Charge")]
    [SerializeField, Min(1)] private int maximumNeedleCharges = 3;
    [SerializeField, HideInInspector] private int parryHalfUnits;

    private float lastFireTime = -999f;
    private readonly List<NeedleProjectile> activeNeedles = new List<NeedleProjectile>();
    private Rigidbody2D playerRigidbody;
    private PlayerController playerController;
    private EffectManager playerStatusEffects;
    private PlayerParry playerParry;
    private NeedleProjectile grappleNeedle;
    private float grappleTimer;
    private float originalGravityScale;
    private bool isGrappling;

    public event Action<int, int> OnParryChargeChanged;
    public event Action OnNeedleThrowDenied;

    public int ParryHalfUnits => parryHalfUnits;
    public int MaximumParryHalfUnits => Mathf.Max(1, maximumNeedleCharges) * 2;
    public int AvailableNeedleThrows => parryHalfUnits / 2;
    public int MaximumNeedleCharges => Mathf.Max(1, maximumNeedleCharges);

    private void Awake()
    {
        Instance = this;
        needlePool = GetComponent<SimplePool>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        playerStatusEffects = GetComponent<EffectManager>();
        playerParry = GetComponent<PlayerParry>();
        parryHalfUnits = Mathf.Clamp(parryHalfUnits, 0, MaximumParryHalfUnits);
    }

    private void OnEnable()
    {
        if (playerParry == null)
        {
            playerParry = GetComponent<PlayerParry>();
        }

        if (playerParry != null)
        {
            playerParry.PerfectGuardSucceeded -= HandlePerfectGuardSucceeded;
            playerParry.PerfectGuardSucceeded += HandlePerfectGuardSucceeded;
        }
    }

    private void Start()
    {
        NotifyParryChargeChanged();
    }

    private void OnDisable()
    {
        if (playerParry != null)
        {
            playerParry.PerfectGuardSucceeded -= HandlePerfectGuardSucceeded;
        }

        EndGrapple(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        maximumNeedleCharges = Mathf.Max(1, maximumNeedleCharges);
        parryHalfUnits = Mathf.Clamp(parryHalfUnits, 0, MaximumParryHalfUnits);
    }

    private void Update()
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

    public bool TryThrowNeedle()
    {
        if (parryHalfUnits < 2)
        {
            OnNeedleThrowDenied?.Invoke();
            Debug.Log("<color=yellow>[Needle Throw]</color> Charge with two perfect guards first.");
            return false;
        }

        if (Time.time < lastFireTime + cooldown)
        {
            return false;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null || firePoint == null || needlePool == null)
        {
            Debug.LogError("[Needle Throw] Camera, Fire Point, or Needle Pool is missing.");
            return false;
        }

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector2 direction = (mousePos - firePoint.position).normalized;

        GameObject needleObj = needlePool.Get(firePoint.position, Quaternion.identity);
        if (needleObj == null)
        {
            return false;
        }

        NeedleProjectile needle = needleObj.GetComponent<NeedleProjectile>();
        if (needle == null)
        {
            needlePool.ReturnToPool(needleObj);
            Debug.LogError("[Needle Throw] Pooled object does not have NeedleProjectile.");
            return false;
        }

        lastFireTime = Time.time;
        ConsumeNeedleCharge();
        needle.Launch(direction, throwSpeed, needleDamage, stunDuration, stunValue, knockbackForce, gameObject);
        return true;
    }

    private void HandlePerfectGuardSucceeded()
    {
        int nextValue = Mathf.Min(MaximumParryHalfUnits, parryHalfUnits + 1);
        if (nextValue == parryHalfUnits) return;

        parryHalfUnits = nextValue;
        NotifyParryChargeChanged();
    }

    private void ConsumeNeedleCharge()
    {
        parryHalfUnits = Mathf.Max(0, parryHalfUnits - 2);
        NotifyParryChargeChanged();
    }

    private void NotifyParryChargeChanged()
    {
        OnParryChargeChanged?.Invoke(parryHalfUnits, MaximumParryHalfUnits);
    }

    public void RegisterNeedle(NeedleProjectile needle)
    {
        if (needle != null && !activeNeedles.Contains(needle)) activeNeedles.Add(needle);
    }

    public void UnregisterNeedle(NeedleProjectile needle)
    {
        activeNeedles.Remove(needle);
    }

    private void ExecuteAction()
    {
        if (isGrappling) return;

        activeNeedles.RemoveAll(needle => needle == null || !needle.gameObject.activeInHierarchy);
        NeedleProjectile enemyNeedle = activeNeedles.Find(needle => needle.currentState == NeedleProjectile.NeedleState.StuckInEnemy);
        if (enemyNeedle != null)
        {
            StartGrapple(enemyNeedle);
            return;
        }

        List<NeedleProjectile> groundNeedles = activeNeedles.FindAll(needle => needle.currentState == NeedleProjectile.NeedleState.StuckInGround);
        if (groundNeedles.Count < 2) return;

        if (threadTrapPrefab == null)
        {
            Debug.LogError("[Thread Trap] Thread Trap Prefab is not assigned.");
            return;
        }

        const float trapDuration = 5f;
        foreach (NeedleProjectile needle in groundNeedles)
        {
            needle.SetAsTrapNode(trapDuration);
        }

        for (int i = 0; i < groundNeedles.Count - 1; i++)
        {
            NeedleProjectile first = groundNeedles[i];
            NeedleProjectile second = groundNeedles[i + 1];
            GameObject trapObj = Instantiate(threadTrapPrefab);
            NeedleThreadTrap trap = trapObj.GetComponent<NeedleThreadTrap>();
            if (trap != null)
            {
                trap.Setup(first.transform.position, second.transform.position, threadDamage, threadTickInterval, gameObject, trapDuration);
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
}
