using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal static class UiTestEditorUiFactory
{
    internal static GameObject CreateObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        if (parent != null) gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    internal static RectTransform CreateRect(string name, Transform parent)
    {
        return (RectTransform)CreateObject(name, parent).transform;
    }

    internal static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    internal static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 topLeftPosition,
        Color baseColor)
    {
        RectTransform rect = CreateRect(name, parent);
        SetFixedTopLeft(rect, topLeftPosition, new Vector2(120f, 38f));

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = baseColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI buttonText = CreateText(
            "Label",
            rect,
            label,
            15f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        SetStretch(buttonText.rectTransform);
        return button;
    }

    internal static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        GameObject eventSystem = CreateObject("EventSystem", null);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    internal static void SetFixedTopLeft(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    internal static void SetCentered(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    internal static void SetTopStretch(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(0f, height);
    }

    internal static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
