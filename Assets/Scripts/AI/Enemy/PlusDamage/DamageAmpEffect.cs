using UnityEngine;

public class DamageAmpEffect : IStatusEffect
{
    readonly float multiplier;
    readonly float duration;
    float endTime;

    public bool IsFinished => Time.time >= endTime;

    public DamageAmpEffect(float multiplier, float duration)
    {
        this.multiplier = Mathf.Max(1f, multiplier);
        this.duration = Mathf.Max(0f, duration);
    }

    public void Apply(GameObject target)
    {
        endTime = Time.time + duration;

        var data = target.GetComponent<EnemyDamageAmpData>();
        if (data == null) data = target.AddComponent<EnemyDamageAmpData>();
        data.Multiplier = multiplier;

        Debug.Log($"<color=orange>[적]</color> 받는 피해 증가 적용! x{multiplier:0.00} ({duration:0.0}초)");
    }

    public void Tick(GameObject target) { }

    public void Remove(GameObject target)
    {
        var data = target.GetComponent<EnemyDamageAmpData>();
        if (data != null) data.Multiplier = 1f;

        Debug.Log("<color=grey>[적]</color> 받는 피해 증가 종료");
    }
}
