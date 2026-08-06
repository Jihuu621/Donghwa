using UnityEngine;

[RequireComponent(typeof(EnemyFSM))]
public abstract class EnemyAIBase : MonoBehaviour
{
    protected EnemyFSM Fsm { get; private set; }

    protected virtual void Awake()
    {
        Fsm = GetComponent<EnemyFSM>();
    }

    public virtual bool TryStun(float duration)
    {
        return false;
    }
}
