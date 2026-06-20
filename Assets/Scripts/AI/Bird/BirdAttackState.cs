using UnityEngine;

public class BirdAttackState : EnemyState
{
    private BirdSetup setup;
    private float prepTimer;
    private float lungeTimer;
    private bool isLunging;
    private Vector2 targetPos;
    private float baseVx;
    private float initialVy;
    private float lungeTime;
    private bool hasDealtDamage;
    private float defaultGravityScale;
    private float hoverY;

    public BirdAttackState(EnemyFSM fsm, BirdSetup setup) : base(fsm)
    {
        this.setup = setup;
        if (fsm.Rb != null) defaultGravityScale = fsm.Rb.gravityScale;
    }

    public override void Enter()
    {
        if (fsm.Sr != null) fsm.Sr.color = Color.magenta;

        prepTimer = 0f;
        lungeTimer = 0f;
        isLunging = false;
        targetPos = Vector2.zero;
        baseVx = 0f;
        initialVy = 0f;
        lungeTime = 0f;
        hasDealtDamage = false;

        fsm.StopMovement();
    }

    public override void Exit()
    {
        if (fsm.Rb != null)
        {
            fsm.Rb.gravityScale = defaultGravityScale;
        }
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
            hoverY = fsm.Player.position.y + setup.HoverHeightAboveTarget;

            if (fsm.Rb != null)
            {
                float vy = GetHoverVy();
                fsm.Rb.linearVelocity = new Vector2(0f, vy);
            }

            if (fsm.Sr != null)
            {
                fsm.Sr.flipX = fsm.Player.position.x > fsm.transform.position.x;
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

        float horiz = playerPosAtStart.x - startPos.x;
        float dir;
        if (Mathf.Abs(horiz) < 0.05f)
        {
            dir = (fsm.Sr != null && fsm.Sr.flipX) ? 1f : -1f;
        }
        else
        {
            dir = Mathf.Sign(horiz);
        }

        float targetX = playerPosAtStart.x + dir * setup.LungeOvershoot;
        float targetY = playerPosAtStart.y;
        targetPos = new Vector2(targetX, targetY);

        lungeTime = Mathf.Max(0.01f, setup.LungeDuration);

        Vector2 toTarget = targetPos - startPos;
        float t = lungeTime;

        if (fsm.Rb != null)
        {
            fsm.Rb.gravityScale = defaultGravityScale * 0.25f;
            float gravity = Physics2D.gravity.y * fsm.Rb.gravityScale;
            float vx = toTarget.x / t;
            float vy = (toTarget.y - 0.5f * gravity * t * t) / t;
            vy *= Mathf.Clamp01(setup.LungeArcLowFactor);

            vy += setup.LungeArcHeight;

            Vector2 v = new Vector2(vx, vy);
            if (setup.MaxDiveSpeed > 0f && v.magnitude > setup.MaxDiveSpeed)
            {
                v = v.normalized * setup.MaxDiveSpeed;
            }

            baseVx = v.x;
            initialVy = v.y;

            fsm.Rb.linearVelocity = v;
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
            Vector2 v = new Vector2(vxNow, fsm.Rb.linearVelocity.y);

            if (setup.MaxDiveSpeed > 0f && v.magnitude > setup.MaxDiveSpeed)
            {
                v = v.normalized * setup.MaxDiveSpeed;
            }
            fsm.Rb.linearVelocity = v;
        }

        if (!hasDealtDamage)
        {
            float duringDist = Vector2.Distance(fsm.transform.position, fsm.Player.position);
            if (duringDist <= setup.AttackRange)
            {
                fsm.PerformAttack(setup.AttackRange);
                hasDealtDamage = true;
            }
        }

        if (lungeTimer >= lungeTime)
        {
            if (fsm.Rb != null)
            {
                fsm.Rb.gravityScale = defaultGravityScale;
                fsm.Rb.linearVelocity = Vector2.zero;
            }

            isLunging = false;
            fsm.TransitionTo(fsm.Chase);
        }
    }

    private float GetHoverVy()
    {
        float oscillation = Mathf.Sin(Time.time * setup.HoverFrequency) * setup.HoverAmplitude;
        float desiredY = hoverY + oscillation;
        float delta = desiredY - fsm.transform.position.y;
        float vy = delta * setup.HoverReturnSpeed;
        return Mathf.Clamp(vy, -setup.HoverMaxVy, setup.HoverMaxVy);
    }
}
