using UnityEngine;

public class BirdManager : MonoBehaviour
{
    public IBirdState CurrentState;

    [Header("Bird Settings")]
    public float PatrolRadius = 3f;
    public float HopForce = 2f;
    public float HopInterval = 0.6f;
    public float LungeForce = 6f;
    public float AttackPrepTime = 0.5f;
    public float LungeDuration = 0.4f;

    // 새로 추가: 돌진 동작 튜닝용
    [Header("Lunge Tuning")]
    public float LungeDistanceMultiplier = 1.6f; // 기본 배수
    public float LungeDistanceMultiplierFar = 3.0f; // 훨씬 멀리 도달용 배수
    public float LungeTargetYOffset = -0.5f; // 목표 지점의 Y 오프셋(아래로 활강 느낌)
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

    // 호버 튜닝
    [Header("Hover Tuning")]
    public float HoverAmplitude = 0.6f;   // 상하 진폭(velocity 기반)
    public float HoverFrequency = 1.2f;   // 상하 진동 빈도
    public float HoverDriftSpeed = 0.6f;  // 기본 떠다니는 수평 속도

    // 호버 목표 Y값(기본: StartPosition.y)
    public float HoverY = 0f;
    public float HoverReturnSpeed = 4f; // 원래 고도로 복귀하는 반응 속도
    public float HoverMaxVy = 3f; // 최대 수직 속도

    // 기본 중력값 저장
    [HideInInspector] public float DefaultGravityScale = 1f;

    private void Awake()
    {
        if (Rb == null) Rb = GetComponent<Rigidbody2D>();
        if (Sprite == null) Sprite = GetComponent<SpriteRenderer>();
        if (DataManager == null) DataManager = GetComponent<EnemyDataManager>();

        StartPosition = transform.position;
        if (Rb != null) DefaultGravityScale = Rb.gravityScale;

        // 기본 호버 Y를 시작 위치 Y로 설정
        HoverY = StartPosition.y;
    }

    private void Start()
    {
        TransitionToState(new Bird_Idle());
    }

    private void Update()
    {
        CurrentState?.UpdateState(this);
    }

    public void TransitionToState(IBirdState newState)
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

    // 호버용 수직 속도 계산: 지정한 HoverY를 중심으로 진동하며 해당 Y로 복귀하려는 속도
    public float GetHoverVy()
    {
        float oscillation = Mathf.Sin(Time.time * HoverFrequency) * HoverAmplitude;
        float desiredY = HoverY + oscillation;
        float delta = desiredY - transform.position.y;
        float vy = delta * HoverReturnSpeed;
        return Mathf.Clamp(vy, -HoverMaxVy, HoverMaxVy);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position + (Vector3)GroundCheckOffset;
        Gizmos.DrawWireSphere(pos, GroundCheckRadius);

        // 에디터에서 HoverY 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(transform.position.x - 1f, HoverY, transform.position.z), new Vector3(transform.position.x + 1f, HoverY, transform.position.z));
    }
}
