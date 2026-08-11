using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class YarnHealthDemoController : MonoBehaviour
{
    [SerializeField] private Health targetHealth;
    [SerializeField] private YarnHealthDisplay display;
    [SerializeField, Min(1)] private int bossDamageInHalfUnits = 3;
    [SerializeField] private TMP_Text hintText;

    public void Configure(
        Health health,
        YarnHealthDisplay yarnDisplay,
        int bossHalfUnits,
        TMP_Text hintLabel)
    {
        targetHealth = health;
        display = yarnDisplay;
        bossDamageInHalfUnits = Mathf.Max(1, bossHalfUnits);
        hintText = hintLabel;

        UpdateHint();
    }

    private void Start()
    {
        if (targetHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetHealth = player.GetComponent<Health>();
            }
        }

        if (display != null && targetHealth != null)
        {
            display.Bind(targetHealth);
        }

        UpdateHint();
    }

    public void StandardHit()
    {
        if (targetHealth == null)
        {
            return;
        }

        targetHealth.ReduceHP(GetHalfUnitValue());
    }

    public void BossHit()
    {
        if (targetHealth == null)
        {
            return;
        }

        targetHealth.ReduceHP(GetHalfUnitValue() * bossDamageInHalfUnits);
    }

    public void HealHalf()
    {
        if (targetHealth == null)
        {
            return;
        }

        targetHealth.Heal(GetHalfUnitValue());
    }

    public void ResetHealth()
    {
        if (targetHealth == null)
        {
            return;
        }

        targetHealth.Init(targetHealth.MaxHP);
    }

    private float GetHalfUnitValue()
    {
        int totalUnits = display != null ? display.TotalHalfUnits : 6;
        return targetHealth != null && totalUnits > 0 ? targetHealth.MaxHP / totalUnits : 0f;
    }

    private void UpdateHint()
    {
        if (hintText == null)
        {
            return;
        }

        hintText.text = $"UI TEST  /  1 HIT = 1/2 YARN  /  BOSS = {bossDamageInHalfUnits} HALF-STEPS";
    }
}
