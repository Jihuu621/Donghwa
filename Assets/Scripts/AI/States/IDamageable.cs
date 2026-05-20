using UnityEngine;

public interface IDamageable
{
    // 기존에 있던 데미지 처리 (호환용)
    void TakeDamage(float damage);

    // 새로 추가할 데미지 처리 (누가 때렸는지 식별 및 넉백 용도)
    void TakeDamage(float damage, GameObject source);
}