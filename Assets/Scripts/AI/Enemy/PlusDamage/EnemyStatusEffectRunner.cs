using System.Collections.Generic;
using UnityEngine;

public class EnemyStatusEffectRunner : MonoBehaviour
{
    readonly List<IStatusEffect> effects = new List<IStatusEffect>();

    public void AddEffect(IStatusEffect effect)
    {
        effect.Apply(gameObject);
        effects.Add(effect);
    }

    void Update()
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            effects[i].Tick(gameObject);
            if (effects[i].IsFinished)
            {
                effects[i].Remove(gameObject);
                effects.RemoveAt(i);
            }
        }
    }
}
