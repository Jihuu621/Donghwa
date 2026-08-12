using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UiTestEditorUiFactory;

internal static class ParryNeedleGaugeBuilder
{
    internal static void Build(
        Transform playerHud,
        NeedleSkillManager needleSkill,
        IReadOnlyList<Sprite> chargeSprites)
    {
        RectTransform root = CreateRect("NeedleChargeHUD", playerHud);
        SetFixedTopLeft(root, new Vector2(405f, -46f), new Vector2(155f, 155f));

        Image mainImage = CreateStateImage("NeedleChargeMain", root);
        Image ghostImage = CreateStateImage("NeedleChargeGhost", root);
        ghostImage.enabled = false;
        ghostImage.color = Color.clear;

        ParryNeedleGaugeDisplay display = root.gameObject.AddComponent<ParryNeedleGaugeDisplay>();
        display.Configure(
            needleSkill,
            mainImage,
            ghostImage,
            chargeSprites,
            null,
            root);
    }

    private static Image CreateStateImage(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent);
        SetStretch(rect);
        Image image = rect.gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }
}
