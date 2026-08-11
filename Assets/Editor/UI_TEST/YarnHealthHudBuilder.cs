using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static UiTestEditorUiFactory;

internal static class YarnHealthHudBuilder
{
    internal static void Build(Transform canvas, Health playerHealth, YarnHealthSprites sprites)
    {
        YarnHealthDisplay display = BuildHealthPanel(canvas, playerHealth, sprites);
        BuildDemoControls(canvas, playerHealth, display);
    }

    private static YarnHealthDisplay BuildHealthPanel(
        Transform canvas,
        Health playerHealth,
        YarnHealthSprites sprites)
    {
        RectTransform hud = CreateRect("YarnHealthHUD", canvas);
        SetFixedTopLeft(hud, new Vector2(42f, -42f), new Vector2(540f, 174f));

        Image background = hud.gameObject.AddComponent<Image>();
        background.color = new Color(0.055f, 0.035f, 0.07f, 0.93f);
        background.raycastTarget = false;

        Shadow shadow = hud.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(7f, -7f);
        shadow.useGraphicAlpha = true;

        RectTransform accent = CreateRect("CrimsonThread", hud);
        SetTopStretch(accent, 0f, 4f);
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = new Color(0.92f, 0.13f, 0.18f, 1f);
        accentImage.raycastTarget = false;

        TextMeshProUGUI title = CreateText(
            "Title", hud, "LIFE THREAD", 22f, FontStyles.Bold,
            new Color(1f, 0.78f, 0.52f, 1f), TextAlignmentOptions.Left);
        SetFixedTopLeft(title.rectTransform, new Vector2(22f, -16f), new Vector2(230f, 34f));
        title.characterSpacing = 3f;

        TextMeshProUGUI healthText = CreateText(
            "HealthValue", hud, "30 / 30", 22f, FontStyles.Bold,
            new Color(1f, 0.92f, 0.82f, 1f), TextAlignmentOptions.Right);
        SetFixedTopLeft(healthText.rectTransform, new Vector2(397f, -69f), new Vector2(120f, 42f));

        RectTransform divider = CreateRect("Divider", hud);
        SetFixedTopLeft(divider, new Vector2(386f, -57f), new Vector2(2f, 74f));
        Image dividerImage = divider.gameObject.AddComponent<Image>();
        dividerImage.color = new Color(1f, 0.55f, 0.38f, 0.25f);
        dividerImage.raycastTarget = false;

        RectTransform row = CreateRect("YarnRow", hud);
        SetFixedTopLeft(row, new Vector2(18f, -51f), new Vector2(356f, 112f));
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
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
        display.Configure(playerHealth, row, sprites.Full, sprites.Half, healthText, flash, 3);
        return display;
    }

    private static void BuildDemoControls(
        Transform canvas,
        Health playerHealth,
        YarnHealthDisplay display)
    {
        RectTransform panel = CreateRect("YarnHealthDemoControls", canvas);
        SetFixedTopLeft(panel, new Vector2(42f, -230f), new Vector2(540f, 96f));
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.025f, 0.045f, 0.86f);
        background.raycastTarget = false;

        TextMeshProUGUI hint = CreateText(
            "Hint", panel,
            "UI TEST  /  1 HIT = 1/2 YARN  /  BOSS = 3 HALF-STEPS",
            13f, FontStyles.Normal, new Color(0.82f, 0.76f, 0.82f, 1f),
            TextAlignmentOptions.Center);
        SetFixedTopLeft(hint.rectTransform, new Vector2(18f, -8f), new Vector2(504f, 25f));

        YarnHealthDemoController demo = panel.gameObject.AddComponent<YarnHealthDemoController>();
        demo.Configure(playerHealth, display, 3, hint);

        Button hit = CreateButton(panel, "StandardHitButton", "1 HIT", new Vector2(18f, -43f), new Color(0.72f, 0.10f, 0.14f, 1f));
        Button boss = CreateButton(panel, "BossHitButton", "BOSS HIT", new Vector2(146f, -43f), new Color(0.39f, 0.17f, 0.48f, 1f));
        Button heal = CreateButton(panel, "HealButton", "+ HALF", new Vector2(274f, -43f), new Color(0.10f, 0.42f, 0.39f, 1f));
        Button reset = CreateButton(panel, "ResetButton", "RESET", new Vector2(402f, -43f), new Color(0.24f, 0.22f, 0.28f, 1f));

        UnityEventTools.AddPersistentListener(hit.onClick, demo.StandardHit);
        UnityEventTools.AddPersistentListener(boss.onClick, demo.BossHit);
        UnityEventTools.AddPersistentListener(heal.onClick, demo.HealHalf);
        UnityEventTools.AddPersistentListener(reset.onClick, demo.ResetHealth);
    }
}
