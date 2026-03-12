using System;

public interface IAttackNotifier
{
    event Action OnAttackPerformed;
}