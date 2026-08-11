using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UiTestEditorUiFactory;

internal static class ParryNeedleGaugeBuilder
{
    internal static void Build(Transform canvas, NeedleSkillManager needleSkill)
    {
        RectTransform panel = CreateRect("ParryNeedleGaugeHUD", canvas);
        SetFixedTopLeft(panel, new Vector2(42f, -338f), new Vector2(540f, 106f));

        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.025f, 0.045f, 0.9f);
        background.raycastTarget = false;

        Shadow shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(5f, -5f);
        shadow.useGraphicAlpha = true;

        RectTransform accent = CreateRect("ParryAccent", panel);
        SetTopStretch(accent, 0f, 3f);
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = new Color(0.92f, 0.13f, 0.18f, 1f);
        accentImage.raycastTarget = false;

        TextMeshProUGUI title = CreateText(
            "ParryTitle", panel, "PERFECT GUARD CHARGE", 14f, FontStyles.Bold,
            new Color(1f, 0.78f, 0.52f, 1f), TextAlignmentOptions.Left);
        SetFixedTopLeft(title.rectTransform, new Vector2(18f, -9f), new Vector2(300f, 27f));
        title.characterSpacing = 1.5f;

        TextMeshProUGUI chargeText = CreateText(
            "NeedleChargeValue", panel, "NEEDLE  0 / 3", 14f, FontStyles.Bold,
            new Color(1f, 0.9f, 0.82f, 1f), TextAlignmentOptions.Right);
        SetFixedTopLeft(chargeText.rectTransform, new Vector2(340f, -9f), new Vector2(182f, 27f));

        RectTransform row = CreateRect("ParryGaugeRow", panel);
        SetFixedTopLeft(row, new Vector2(18f, -44f), new Vector2(504f, 46f));

        List<Image> fills = new List<Image>(6);
        for (int slotIndex = 0; slotIndex < 3; slotIndex++)
        {
            RectTransform slot = CreateRect($"ParryGaugeSlot_{slotIndex + 1}", row);
            SetFixedTopLeft(slot, new Vector2(slotIndex * 172f, 0f), new Vector2(160f, 42f));

            Image slotBackground = slot.gameObject.AddComponent<Image>();
            slotBackground.color = new Color(0.075f, 0.025f, 0.04f, 0.95f);
            slotBackground.raycastTarget = false;

            fills.Add(CreateGaugePart("LeftHalf", slot, new Vector2(5f, -5f), new Vector2(73.5f, 32f)));
            fills.Add(CreateGaugePart("RightHalf", slot, new Vector2(81.5f, -5f), new Vector2(73.5f, 32f)));

            Color border = new Color(0.94f, 0.08f, 0.16f, 1f);
            CreateGaugeLine("BorderTop", slot, new Vector2(0f, 0f), new Vector2(160f, 4f), border);
            CreateGaugeLine("BorderBottom", slot, new Vector2(0f, -38f), new Vector2(160f, 4f), border);
            CreateGaugeLine("BorderLeft", slot, new Vector2(0f, -4f), new Vector2(4f, 34f), border);
            CreateGaugeLine("BorderRight", slot, new Vector2(156f, -4f), new Vector2(4f, 34f), border);
            CreateGaugeLine("HalfDivider", slot, new Vector2(78.5f, -4f), new Vector2(3f, 34f), new Color(0.38f, 0.1f, 0.16f, 1f));
        }

        ParryNeedleGaugeDisplay display = panel.gameObject.AddComponent<ParryNeedleGaugeDisplay>();
        display.Configure(needleSkill, fills, chargeText, panel);
    }

    private static Image CreateGaugePart(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent);
        SetFixedTopLeft(rect, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.035f, 0.055f, 0.9f);
        image.raycastTarget = false;
        return image;
    }

    private static void CreateGaugeLine(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        SetFixedTopLeft(rect, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }
}
