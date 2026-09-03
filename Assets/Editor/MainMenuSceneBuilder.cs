using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates only the replaceable placeholder UI for MainScene. Run this again
/// from the Tools menu if the placeholder hierarchy needs to be restored.
/// </summary>
internal static class MainMenuSceneBuilder
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasName = "MainMenuCanvas";
    private const string EventSystemName = "MainMenuEventSystem";

    [MenuItem("Tools/Donghwa/Main Menu/Rebuild MainScene Menu")]
    private static void RebuildFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildMainScene();
    }

    // Kept public so the scene can also be rebuilt from Unity batch mode.
    public static void BuildMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        RemoveGeneratedRoot(scene, CanvasName);

        GameObject canvasObject = CreateCanvas();
        MainMenuController controller = canvasObject.AddComponent<MainMenuController>();

        RectTransform buttonContainer = CreateRect("MenuButtons", canvasObject.transform);
        SetCentered(buttonContainer, new Vector2(360f, 232f));

        Button gameStart = CreateButton(buttonContainer, "GameStartButton", "GAME START", 64f);
        Button settings = CreateButton(buttonContainer, "SettingsButton", "SETTINGS", 0f);
        Button gameExit = CreateButton(buttonContainer, "GameExitButton", "GAME EXIT", -64f);

        RectTransform settingsRoot = CreateSettingsPlaceholder(canvasObject.transform);
        controller.Configure(gameStart, settings, gameExit, settingsRoot.gameObject, "All-In-One");
        Button closeSettings = settingsRoot.GetComponentInChildren<Button>(true);
        UnityEventTools.AddPersistentListener(closeSettings.onClick, controller.CloseSettings);
        controller.CloseSettings();

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MainMenuSceneBuilder] MainScene menu was rebuilt.");
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObject;
    }

    private static Button CreateButton(Transform parent, string name, string label, float y)
    {
        RectTransform root = CreateRect(name, parent);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, y);
        root.sizeDelta = new Vector2(320f, 48f);

        Image image = root.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        TextMeshProUGUI text = CreateText("Label", root, label, 19f);
        Stretch(text.rectTransform);
        return button;
    }

    private static RectTransform CreateSettingsPlaceholder(Transform parent)
    {
        RectTransform root = CreateRect("SettingsPanel", parent);
        SetCentered(root, new Vector2(420f, 170f));

        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.78f);

        TextMeshProUGUI text = CreateText("PlaceholderLabel", root, "SETTINGS PLACEHOLDER", 21f);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = new Vector2(0f, 24f);
        text.rectTransform.sizeDelta = new Vector2(360f, 42f);

        Button closeButton = CreateButton(root, "CloseSettingsButton", "CLOSE", -46f);
        closeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 40f);
        return root;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return (RectTransform)gameObject.transform;
    }

    private static void SetCentered(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        GameObject eventSystem = new GameObject(
            EventSystemName,
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystem.layer = LayerMask.NameToLayer("UI");
    }

    private static void RemoveGeneratedRoot(Scene scene, string rootName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == rootName) Object.DestroyImmediate(root);
        }
    }
}
