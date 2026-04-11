using UnityEngine;

/// <summary>
/// 체크메이트 스킬에서 사용하는 스턴 상태 효과.
/// 기존 IStatusEffect 패턴을 따른다.
/// </summary>
public class StunEffect : IStatusEffect
{
    private readonly float duration;
    private float endTime;

    public bool IsFinished => Time.time >= endTime;

    public StunEffect(float duration)
    {
        this.duration = Mathf.Max(0f, duration);
    }

    public void Apply(GameObject target)
    {
        endTime = Time.time + duration;
        // 적 이동/공격을 Idle 상태로 강제 전환하여 스턴 구현
        var stateManager = target.GetComponent<EnemyStateManager>();
        if (stateManager != null)
        {
            stateManager.TransitionToState(new IdleState());
        }
        Debug.Log($"<color=yellow>[스턴]</color> {target.name} {duration:0.0}초 스턴");
    }

    public void Tick(GameObject target) { }

    public void Remove(GameObject target)
    {
        Debug.Log($"<color=grey>[스턴]</color> {target.name} 스턴 해제");
    }
}