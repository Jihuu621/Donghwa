using UnityEngine;

public interface IStatusEffect
{
    void Apply(GameObject target);
    void Tick(GameObject target);
    bool IsFinished { get; }
    void Remove(GameObject target);
}
