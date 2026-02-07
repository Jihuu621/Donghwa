using UnityEngine;

public class RabbitManager : MonoBehaviour
{
    public IRabbitState CurrentState;

    [HideInInspector] public Animator Anim;

    [Header("Rabbit Settings")]
    public float PatrolRadius = 3f;
    public float HopForce = 2f;
    public float HopInterval = 0.6f;
    public float LungeForce = 6f;
    public float AttackPrepTime = 0.5f;
    public float LungeDuration = 0.4f;

    // 새로 추가: 돌진 동작 튜닝용
    [Header("Lunge Tuning")]
    public float LungeDistanceMultiplier = 1.6f; // 플레이어 기준으로 얼마나 더 멀리 도달할지 배수
    public float LungeAccel = 1.2f;              // 돌진 중 최종 수평속도 증가량(비율 계수)
    public float LungeArcLowFactor = 0.5f;       // 수직 성분을 얼마나 낮출지(0..1)

    // 지면 검사 설정 (선택)
    public LayerMask GroundLayer;
    public Vector2 GroundCheckOffset = new Vector2(0f, -0.5f);
    public float GroundCheckRadius = 0.12f;

    // 내부 상태 공유값
    [HideInInspector] public int PatrolDirection = 1; // 1 = 오른쪽, -1 = 왼쪽
    [HideInInspector] public Vector2 StartPosition;

    // 컴포넌트
    public Rigidbody2D Rb;
    public SpriteRenderer Sprite;
    public EnemyDataManager DataManager;

    private void Awake()
    {
        if (Rb == null) Rb = GetComponent<Rigidbody2D>();
        if (Sprite == null) Sprite = GetComponent<SpriteRenderer>();
        if (DataManager == null) DataManager = GetComponent<EnemyDataManager>();

        StartPosition = transform.position;
    }

    private void Start()
    {
        TransitionToState(new Rabbit_Idle());
    }

    private void Update()
    {
        CurrentState?.UpdateState(this);
    }

    public void TransitionToState(IRabbitState newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);
    }

    public bool IsGrounded()
    {
        Vector2 pos = (Vector2)transform.position + GroundCheckOffset;
        return Physics2D.OverlapCircle(pos, GroundCheckRadius, GroundLayer) != null;
    }

    // 깡총 동작: 기존 방식 유지 (상태에서 hopTimer 전달)
    public bool TryHop(ref float hopTimer)
    {
        hopTimer += Time.deltaTime;
        if (hopTimer >= HopInterval)
        {
            if (Rb == null) Rb = GetComponent<Rigidbody2D>();
            if (Rb != null)
            {
                Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, HopForce);
            }
            hopTimer = 0f;
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position + (Vector3)GroundCheckOffset;
        Gizmos.DrawWireSphere(pos, GroundCheckRadius);
    }
}
