using System;
using UnityEngine;

public class EnemyAttackNotifier : MonoBehaviour, IAttackNotifier
{
    public event Action OnAttackPerformed;

    public void NotifyAttackPerformed()
    {
        OnAttackPerformed?.Invoke();
    }
}