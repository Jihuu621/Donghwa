using UnityEngine;

public class ShieldController : MonoBehaviour
{
    public int MaxShieldHP = 50;
    public int CurrentShieldHP;

    public Sprite NormalSprite;
    public Sprite CrackedSprite;
    public Sprite BrokenSprite;

    SpriteRenderer sr;
    public bool IsBroken => CurrentShieldHP <= 0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        CurrentShieldHP = MaxShieldHP;
        sr.sprite = NormalSprite;
    }

    public void TakeShieldDamage(int damage)
    {
        if (IsBroken) return;

        CurrentShieldHP -= damage;
        Debug.Log($"[πÊ∆–] «««ÿ {damage}");

        if (CurrentShieldHP <= MaxShieldHP / 2 && CurrentShieldHP > 0)
            sr.sprite = CrackedSprite;

        if (CurrentShieldHP <= 0)
            BreakShield();
    }

    void BreakShield()
    {
        CurrentShieldHP = 0;
        sr.sprite = BrokenSprite;
        Debug.Log("[πÊ∆–] ∆ƒ±´µ ");
        gameObject.SetActive(false);
    }
}
