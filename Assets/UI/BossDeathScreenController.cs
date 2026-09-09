using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Scene-local death presentation; attach only to the boss test scene.</summary>
[DisallowMultipleComponent]
public sealed class BossDeathScreenController : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private TMP_FontAsset menuFont;
    [SerializeField] private string mainMenuSceneName = "MainScene";
    [SerializeField, Min(0.1f)] private float backgroundFadeDuration = 2f;
    [SerializeField, Min(0f)] private float titleDelay = 0.35f;
    [SerializeField, Min(0.1f)] private float titleFadeDuration = 1.4f;
    [SerializeField, Min(0.1f)] private float buttonFadeDuration = 0.6f;

    private readonly List<MonoBehaviour> suspendedBehaviours = new List<MonoBehaviour>();
    private CanvasGroup overlay;
    private CanvasGroup titleGroup;
    private CanvasGroup buttonGroup;
    private Image background;
    private Button restartButton;
    private bool showing;
    private bool loading;
    private float previousTimeScale;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    private void Start()
    {
        CreateView();
        if (playerHealth == null)
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (Health health in root.GetComponentsInChildren<Health>())
                {
                    if (health.CompareTag("Player"))
                    {
                        playerHealth = health;
                        break;
                    }
                }
                if (playerHealth != null) break;
            }
        }

        if (playerHealth == null)
        {
            Debug.LogError("[BossDeathScreen] Player Health is missing.", this);
            return;
        }

        playerHealth.OnDeath += ShowDeathScreen;
        if (playerHealth.CurrentHP <= 0f) ShowDeathScreen();
    }

    private void ShowDeathScreen()
    {
        if (showing || loading) return;
        showing = true;
        previousTimeScale = Time.timeScale;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;

        // Time.timeScale alone does not stop Update from accepting attacks or skills.
        foreach (MonoBehaviour behaviour in playerHealth.GetComponentsInChildren<MonoBehaviour>())
        {
            if (behaviour == playerHealth || !behaviour.enabled) continue;
            suspendedBehaviours.Add(behaviour);
            behaviour.enabled = false;
            behaviour.StopAllCoroutines();
        }

        foreach (PauseMenuController pause in FindObjectsByType<PauseMenuController>())
        {
            if (pause.gameObject.scene != gameObject.scene || !pause.enabled) continue;
            if (pause.IsPaused) previousTimeScale = 1f;
            pause.CloseMenu();
            suspendedBehaviours.Add(pause);
            pause.enabled = false;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        overlay.gameObject.SetActive(true);
        overlay.blocksRaycasts = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(AnimateDeath());
    }

    private IEnumerator AnimateDeath()
    {
        float revealButtonsAt = Mathf.Max(backgroundFadeDuration, titleDelay + titleFadeDuration) + 0.4f;
        float elapsed = 0f;
        while (elapsed < revealButtonsAt + buttonFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            background.color = new Color(0f, 0f, 0f, Fade(elapsed / backgroundFadeDuration));
            titleGroup.alpha = Fade((elapsed - titleDelay) / titleFadeDuration);
            titleGroup.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, titleGroup.alpha);
            buttonGroup.alpha = Fade((elapsed - revealButtonsAt) / buttonFadeDuration);
            yield return null;
        }

        background.color = Color.black;
        titleGroup.alpha = 1f;
        buttonGroup.alpha = 1f;
        buttonGroup.interactable = true;
        buttonGroup.blocksRaycasts = true;
        restartButton.Select();
    }

    private static float Fade(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

    public void RestartBossFight() => BeginLoad(gameObject.scene.path);
    public void ReturnToMainMenu() => BeginLoad(mainMenuSceneName);

    private void BeginLoad(string sceneName)
    {
        if (!showing || loading || !buttonGroup.interactable) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[BossDeathScreen] Scene '{sceneName}' is not in Build Settings.", this);
            return;
        }

        loading = true;
        buttonGroup.interactable = false;
        StartCoroutine(LoadScene(sceneName));
    }

    private IEnumerator LoadScene(string sceneName)
    {
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            titleGroup.alpha = buttonGroup.alpha = 1f - Fade(elapsed / 0.35f);
            yield return null;
        }

        // Resume only when the next scene activates, keeping the death frame frozen.
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (operation != null) yield return operation;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnDeath -= ShowDeathScreen;
        StopAllCoroutines();
        if (!showing) return;
        if (!loading)
        {
            foreach (MonoBehaviour behaviour in suspendedBehaviours)
                if (behaviour != null) behaviour.enabled = true;
        }
        suspendedBehaviours.Clear();
        Time.timeScale = loading ? 1f : previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        if (overlay != null) overlay.gameObject.SetActive(false);
    }

    private void CreateView()
    {
        RectTransform root = CreateRect("BossDeathCanvas", transform, Vector2.zero, Vector2.zero);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.gameObject.AddComponent<GraphicRaycaster>();
        overlay = root.gameObject.AddComponent<CanvasGroup>();

        RectTransform backdrop = CreateRect("FadeToBlack", root, Vector2.zero, Vector2.zero);
        Stretch(backdrop);
        background = backdrop.gameObject.AddComponent<Image>();
        background.color = Color.clear;

        RectTransform title = CreateRect("YouDied", root, new Vector2(1200f, 180f), new Vector2(0f, 85f));
        titleGroup = title.gameObject.AddComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        TMP_Text titleText = CreateText(title, "YOU DIED", 112f, new Color(0.65f, 0.055f, 0.045f));
        titleText.characterSpacing = 12f;

        RectTransform actions = CreateRect("Actions", root, new Vector2(480f, 180f), new Vector2(0f, -160f));
        buttonGroup = actions.gameObject.AddComponent<CanvasGroup>();
        buttonGroup.alpha = 0f;
        buttonGroup.interactable = false;
        buttonGroup.blocksRaycasts = false;
        restartButton = CreateButton(actions, "RestartButton", "RESTART", 42f);
        restartButton.onClick.AddListener(RestartBossFight);
        Button mainButton = CreateButton(actions, "MainMenuButton", "MAIN MENU", -42f);
        mainButton.onClick.AddListener(ReturnToMainMenu);
        restartButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = mainButton, selectOnDown = mainButton };
        mainButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = restartButton, selectOnDown = restartButton };
        root.gameObject.SetActive(false);
    }

    private Button CreateButton(Transform parent, string objectName, string label, float y)
    {
        RectTransform rect = CreateRect(objectName, parent, new Vector2(420f, 66f), new Vector2(0f, y));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.07f, 0.065f, 0.95f);
        colors.highlightedColor = colors.selectedColor = new Color(0.32f, 0.22f, 0.13f, 1f);
        colors.pressedColor = new Color(0.45f, 0.3f, 0.16f, 1f);
        colors.fadeDuration = 0.15f;
        button.colors = colors;
        CreateText(rect, label, 36f, new Color(0.88f, 0.84f, 0.75f));
        return button;
    }

    private TMP_Text CreateText(Transform parent, string value, float size, Color color)
    {
        RectTransform rect = CreateRect("Label", parent, Vector2.zero, Vector2.zero);
        Stretch(rect);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = menuFont;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string objectName, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
