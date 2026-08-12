using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static UiTestEditorUiFactory;

internal static class YarnHealthHudBuilder
{
    internal static RectTransform Build(
        Transform canvas,
        Health playerHealth,
        UiHudSprites sprites)
    {
        RectTransform hud = BuildHealthPanel(canvas, playerHealth, sprites);
        BuildDemoControls(canvas, playerHealth, hud.GetComponentInChildren<YarnHealthDisplay>(true));
        return hud;
    }

    private static RectTransform BuildHealthPanel(
        Transform canvas,
        Health playerHealth,
        UiHudSprites sprites)
    {
        RectTransform hud = CreateRect("PlayerStatusHUD", canvas);
        SetFixedTopLeft(hud, new Vector2(42f, -42f), new Vector2(620f, 244f));

        Image background = hud.gameObject.AddComponent<Image>();
        background.sprite = sprites.Background;
        background.type = Image.Type.Simple;
        background.preserveAspect = true;
        background.color = Color.white;
        background.raycastTarget = false;

        RectTransform row = CreateRect("HealthSlotRow", hud);
        SetFixedTopLeft(row, new Vector2(55f, -70f), new Vector2(344f, 118f));
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform flashRect = CreateRect("DamageFlash", hud);
        SetStretch(flashRect);
        Image flash = flashRect.gameObject.AddComponent<Image>();
        flash.color = new Color(1f, 0.08f, 0.08f, 0f);
        flash.raycastTarget = false;
        flashRect.SetAsLastSibling();

        YarnHealthDisplay display = row.gameObject.AddComponent<YarnHealthDisplay>();
        display.Configure(
            playerHealth,
            row,
            sprites.HealthFull,
            sprites.HealthDamaged,
            sprites.HealthEmpty,
            null,
            flash,
            3);
        return hud;
    }

    private static void BuildDemoControls(
        Transform canvas,
        Health playerHealth,
        YarnHealthDisplay display)
    {
        RectTransform panel = CreateRect("YarnHealthDemoControls", canvas);
        SetFixedTopLeft(panel, new Vector2(42f, -306f), new Vector2(620f, 96f));
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.025f, 0.045f, 0.86f);
        background.raycastTarget = false;

        TextMeshProUGUI hint = CreateText(
            "Hint",
            panel,
            "UI TEST  /  HIT: FULL > DAMAGED > EMPTY FRAME",
            13f,
            FontStyles.Normal,
            new Color(0.82f, 0.76f, 0.82f, 1f),
            TextAlignmentOptions.Center);
        SetFixedTopLeft(hint.rectTransform, new Vector2(18f, -8f), new Vector2(584f, 25f));

        YarnHealthDemoController demo = panel.gameObject.AddComponent<YarnHealthDemoController>();
        demo.Configure(playerHealth, display, 3, hint);

        Button hit = CreateButton(panel, "StandardHitButton", "1 HIT", new Vector2(55f, -43f), new Color(0.72f, 0.10f, 0.14f, 1f));
        Button boss = CreateButton(panel, "BossHitButton", "BOSS HIT", new Vector2(183f, -43f), new Color(0.39f, 0.17f, 0.48f, 1f));
        Button heal = CreateButton(panel, "HealButton", "+ HALF", new Vector2(311f, -43f), new Color(0.10f, 0.42f, 0.39f, 1f));
        Button reset = CreateButton(panel, "ResetButton", "RESET", new Vector2(439f, -43f), new Color(0.24f, 0.22f, 0.28f, 1f));

        UnityEventTools.AddPersistentListener(hit.onClick, demo.StandardHit);
        UnityEventTools.AddPersistentListener(boss.onClick, demo.BossHit);
        UnityEventTools.AddPersistentListener(heal.onClick, demo.HealHalf);
        UnityEventTools.AddPersistentListener(reset.onClick, demo.ResetHealth);
    }
}
