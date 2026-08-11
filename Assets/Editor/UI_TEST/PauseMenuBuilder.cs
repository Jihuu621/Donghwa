using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static UiTestEditorUiFactory;

internal static class PauseMenuBuilder
{
    internal static void Build(GameObject canvasObject)
    {
        RectTransform overlay = CreateRect("PauseMenuOverlay", canvasObject.transform);
        SetStretch(overlay);
        Image dimmer = overlay.gameObject.AddComponent<Image>();
        dimmer.color = new Color(0.01f, 0.008f, 0.02f, 0.78f);
        dimmer.raycastTarget = true;

        RectTransform panel = CreateRect("PausePanel", overlay);
        SetCentered(panel, new Vector2(500f, 440f));
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.055f, 0.035f, 0.075f, 0.98f);

        Shadow shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(10f, -10f);

        RectTransform accent = CreateRect("PauseAccent", panel);
        SetTopStretch(accent, 0f, 5f);
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = new Color(0.92f, 0.13f, 0.18f, 1f);
        accentImage.raycastTarget = false;

        Button closeButton = CreateButton(
            panel, "ClosePauseButton", "X", new Vector2(438f, -16f),
            new Color(0.28f, 0.12f, 0.2f, 1f));
        closeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(46f, 42f);

        RectTransform mainContent = CreateRect("PauseMainContent", panel);
        SetStretch(mainContent);
        PauseMainView mainView = BuildMainContent(mainContent);

        SoundSettingsView settings = BuildSoundSettings(panel);
        PauseMenuController controller = canvasObject.GetComponent<PauseMenuController>();
        if (controller == null) controller = canvasObject.AddComponent<PauseMenuController>();

        UnityEventTools.AddPersistentListener(mainView.MainMenuButton.onClick, controller.ReturnToMainMenu);
        UnityEventTools.AddPersistentListener(mainView.SoundSettingsButton.onClick, controller.OpenSoundSettings);
        UnityEventTools.AddPersistentListener(settings.BackButton.onClick, controller.CloseSoundSettings);
        UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseMenu);

        controller.Configure(
            overlay.gameObject,
            mainContent.gameObject,
            settings.Root.gameObject,
            settings.MasterSlider,
            settings.EffectSlider,
            "MainScene");

        settings.Root.gameObject.SetActive(false);
        overlay.gameObject.SetActive(false);
    }

    private static PauseMainView BuildMainContent(RectTransform parent)
    {
        TextMeshProUGUI title = CreateText(
            "PauseTitle", parent, "PAUSED", 40f, FontStyles.Bold,
            new Color(1f, 0.78f, 0.52f, 1f), TextAlignmentOptions.Center);
        SetFixedTopLeft(title.rectTransform, new Vector2(40f, -62f), new Vector2(420f, 58f));
        title.characterSpacing = 6f;

        TextMeshProUGUI detail = CreateText(
            "PauseDetail", parent, "UI TEST IS PAUSED", 15f, FontStyles.Bold,
            new Color(0.82f, 0.76f, 0.82f, 1f), TextAlignmentOptions.Center);
        SetFixedTopLeft(detail.rectTransform, new Vector2(40f, -126f), new Vector2(420f, 28f));
        detail.characterSpacing = 2f;

        Button mainMenu = CreateButton(
            parent, "MainMenuButton", "MAIN MENU", new Vector2(95f, -194f),
            new Color(0.72f, 0.10f, 0.14f, 1f));
        mainMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(310f, 48f);

        Button soundSettings = CreateButton(
            parent, "SoundSettingsButton", "SOUND SETTINGS", new Vector2(95f, -258f),
            new Color(0.24f, 0.17f, 0.34f, 1f));
        soundSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(310f, 48f);

        TextMeshProUGUI hint = CreateText(
            "EscapeHint", parent, "ESC  /  CLOSE", 13f, FontStyles.Bold,
            new Color(0.65f, 0.58f, 0.68f, 1f), TextAlignmentOptions.Center);
        SetFixedTopLeft(hint.rectTransform, new Vector2(95f, -337f), new Vector2(310f, 28f));
        return new PauseMainView(mainMenu, soundSettings);
    }

    private static SoundSettingsView BuildSoundSettings(RectTransform parent)
    {
        RectTransform root = CreateRect("SoundSettingsPanel", parent);
        SetStretch(root);

        TextMeshProUGUI title = CreateText(
            "SoundSettingsTitle", root, "SOUND SETTINGS", 30f, FontStyles.Bold,
            new Color(1f, 0.78f, 0.52f, 1f), TextAlignmentOptions.Center);
        SetFixedTopLeft(title.rectTransform, new Vector2(45f, -54f), new Vector2(410f, 52f));
        title.characterSpacing = 3f;

        TextMeshProUGUI masterLabel = CreateText(
            "MasterVolumeLabel", root, "MASTER VOLUME", 15f, FontStyles.Bold,
            new Color(0.95f, 0.88f, 0.82f, 1f), TextAlignmentOptions.Left);
        SetFixedTopLeft(masterLabel.rectTransform, new Vector2(75f, -132f), new Vector2(350f, 28f));
        Slider masterSlider = CreateVolumeSlider(root, "MasterVolumeSlider", new Vector2(75f, -166f));

        TextMeshProUGUI effectLabel = CreateText(
            "EffectVolumeLabel", root, "SFX / VFX VOLUME", 15f, FontStyles.Bold,
            new Color(0.95f, 0.88f, 0.82f, 1f), TextAlignmentOptions.Left);
        SetFixedTopLeft(effectLabel.rectTransform, new Vector2(75f, -234f), new Vector2(350f, 28f));
        Slider effectSlider = CreateVolumeSlider(root, "EffectVolumeSlider", new Vector2(75f, -268f));

        Button back = CreateButton(
            root, "SoundSettingsBackButton", "BACK", new Vector2(145f, -342f),
            new Color(0.24f, 0.17f, 0.34f, 1f));
        back.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 44f);
        return new SoundSettingsView(root, masterSlider, effectSlider, back);
    }

    private static Slider CreateVolumeSlider(Transform parent, string name, Vector2 topLeftPosition)
    {
        RectTransform root = CreateRect(name, parent);
        SetFixedTopLeft(root, topLeftPosition, new Vector2(350f, 24f));

        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.09f, 0.055f, 0.12f, 1f);

        RectTransform fillArea = CreateRect("Fill Area", root);
        SetStretch(fillArea);
        fillArea.offsetMin = new Vector2(4f, 4f);
        fillArea.offsetMax = new Vector2(-4f, -4f);

        RectTransform fill = CreateRect("Fill", fillArea);
        SetStretch(fill);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.92f, 0.13f, 0.18f, 1f);

        RectTransform handleArea = CreateRect("Handle Slide Area", root);
        SetStretch(handleArea);
        handleArea.offsetMin = new Vector2(7f, 0f);
        handleArea.offsetMax = new Vector2(-7f, 0f);

        RectTransform handle = CreateRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(12f, 26f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = new Color(1f, 0.78f, 0.52f, 1f);

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }

    private readonly struct SoundSettingsView
    {
        internal SoundSettingsView(RectTransform root, Slider master, Slider effect, Button back)
        {
            Root = root;
            MasterSlider = master;
            EffectSlider = effect;
            BackButton = back;
        }

        internal RectTransform Root { get; }
        internal Slider MasterSlider { get; }
        internal Slider EffectSlider { get; }
        internal Button BackButton { get; }
    }

    private readonly struct PauseMainView
    {
        internal PauseMainView(Button mainMenuButton, Button soundSettingsButton)
        {
            MainMenuButton = mainMenuButton;
            SoundSettingsButton = soundSettingsButton;
        }

        internal Button MainMenuButton { get; }
        internal Button SoundSettingsButton { get; }
    }
}
