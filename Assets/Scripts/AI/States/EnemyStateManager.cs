using UnityEngine;

public class EnemyStateManager : MonoBehaviour
{
    public IEnemyState CurrentState;

    private void Start()
    {
        TransitionToState(new IdleState());
    }

    private void Update()
    {
        CurrentState?.UpdateState(this);
    }

    public void TransitionToState(IEnemyState newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);
        // Debug.Log($"[TransitionToState] {newState}로 스테이츠 변경");
    }

    public void PerformAttack(float range)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        if (dist > range) return;

        Health playerHealth = player.GetComponent<Health>();
        PlayerParry parry = player.GetComponent<PlayerParry>();

        float damage = 10f;
        if (TryGetComponent<EnemyDataManager>(out var data))
        {
            damage = data.EnemyData.Damage;
        }

        if (parry != null)
        {
            float finalDamage = parry.OnHit(damage);

            if (finalDamage <= 0)
            {
                Debug.Log("<color=green>[패링 성공]</color> 공격이 무효화되었습니다.");
            }
            else
            {
                playerHealth?.TakeDamage(finalDamage);
            }
        }
        else
        {
            playerHealth?.TakeDamage(damage);
        }
    }
}