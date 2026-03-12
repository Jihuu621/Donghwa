using UnityEngine;

public interface IMovableEnemy
{
    Vector2 PreviousPosition { get; }
    Vector2 CurrentPosition { get; }
}