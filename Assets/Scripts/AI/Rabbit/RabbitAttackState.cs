using UnityEngine;

public class RabbitAttackState : EnemyState
{
    private RabbitSetup setup;
    private float prepTimer;
    private float lungeTimer;
    private bool isLunging;
    private Vector2 targetPos;
    private float hopTimer;
    private float baseVx;
    private float initialVy;
    private float lungeTime;

    public RabbitAttackState(EnemyFSM fsm, RabbitSetup setup) : base(fsm)
    {
        this.setup = setup;
    }

    public override void Enter()
    {
        if (fsm.Sr != null) fsm.Sr.color = Color.magenta;
        
        prepTimer = 0f;
        lungeTimer = 0f;
        isLunging = false;
        targetPos = Vector2.zero;
        hopTimer = 0f;
        baseVx = 0f;
        initialVy = 0f;
        lungeTime = 0f;

        fsm.StopMovement();
    }

    public override void Execute()
    {
        if (fsm.Player == null)
        {
            fsm.TransitionTo(fsm.Idle);
            return;
        }

        float dist = Vector2.Distance(fsm.transform.position, fsm.Player.position);
        if (dist > setup.AttackRange * 2f && !isLunging)
        {
            fsm.StopMovement();
            fsm.TransitionTo(fsm.Chase);
            return;
        }

        if (!isLunging)
        {
            prepTimer += Time.deltaTime;
            fsm.StopMovement();

            SetFacing(fsm.Player.position.x > fsm.transform.position.x);

            if (fsm.Rb != null)
            {
                hopTimer += Time.deltaTime;
                if (hopTimer >= setup.HopInterval)
                {
                    fsm.Rb.linearVelocity = new Vector2(fsm.Rb.linearVelocity.x, setup.HopForce);
                    hopTimer = 0f;
                }
            }

            if (prepTimer >= setup.AttackPrepTime)
            {
                StartLunge();
            }
        }
        else
        {
            UpdateLunge();
        }
    }

    private void StartLunge()
    {
        Vector2 startPos = fsm.transform.position;
        Vector2 playerPosAtStart = fsm.Player.position;
        float offsetX = (playerPosAtStart.x - startPos.x) * setup.LungeDistanceMultiplier;
        float targetX = startPos.x + offsetX;
        targetPos = new Vector2(targetX, startPos.y);

        lungeTime = Mathf.Max(0.01f, setup.LungeDuration);

        Vector2 toTarget = targetPos - startPos;
        float dx = toTarget.x;
        float dy = toTarget.y;
        float t = lungeTime;

        if (fsm.Rb != null)
        {
            float gravity = Physics2D.gravity.y * fsm.Rb.gravityScale;
            float vx = dx / t;
            float vy = (dy - 0.5f * gravity * t * t) / t;
            vy *= Mathf.Clamp01(setup.LungeArcLowFactor);

            baseVx = vx;
            initialVy = vy;

            fsm.Rb.linearVelocity = new Vector2(baseVx, initialVy);
        }

        if (fsm.Anim != null)
        {
            if (!string.IsNullOrEmpty(setup.AttackTrigger) && fsm.Anim.HasParameterOfType(setup.AttackTrigger, AnimatorControllerParameterType.Trigger))
            {
                fsm.Anim.SetTrigger(setup.AttackTrigger);
            }
            else if (!string.IsNullOrEmpty(setup.AttackAnimation))
            {
                fsm.Anim.Play(setup.AttackAnimation);
            }
        }

        isLunging = true;
        lungeTimer = 0f;
    }

    private void UpdateLunge()
    {
        lungeTimer += Time.deltaTime;
        float progress = lungeTime <= 0f ? 1f : Mathf.Clamp01(lungeTimer / lungeTime);
        float speedMultiplier = Mathf.Lerp(1f, 1f + setup.LungeAccel, progress);

        if (fsm.Rb != null)
        {
            float vxNow = baseVx * speedMultiplier;
            fsm.Rb.linearVelocity = new Vector2(vxNow, fsm.Rb.linearVelocity.y);
        }

        if (lungeTimer >= lungeTime)
        {
            fsm.StopMovement();
            isLunging = false;

            float postDist = Vector2.Distance(fsm.transform.position, fsm.Player.position);
            if (postDist <= setup.AttackRange)
            {
                fsm.PerformAttack(setup.AttackRange);
                fsm.TransitionTo(fsm.Idle);
            }
            else
            {
                fsm.TransitionTo(fsm.Chase);
            }
        }
    }

    private void SetFacing(bool faceRight)
    {
        Vector3 scale = fsm.transform.localScale;
        float absX = Mathf.Abs(scale.x);
        if (absX < 0.0001f) absX = 1f;

        bool usePositiveX = setup.DefaultFacingRight ? faceRight : !faceRight;
        scale.x = usePositiveX ? absX : -absX;
        fsm.transform.localScale = scale;
    }
}
