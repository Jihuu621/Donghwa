using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Holds only the behaviour of the MainScene menu. The button objects and the
/// settings root are assigned in the inspector, so their images/prefabs can be
/// replaced without changing this code.
/// </summary>
[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("The scene loaded when Game Start is pressed. It must be in Build Settings.")]
    [SerializeField] private string gameplaySceneName = "All-In-One";

    [Header("Actions")]
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button gameExitButton;

    [Header("Optional settings view")]
    [Tooltip("Any settings prefab/root can be assigned here. It is shown by the Settings button.")]
    [SerializeField] private GameObject settingsRoot;

    public string GameplaySceneName
    {
        get => gameplaySceneName;
        set => gameplaySceneName = value;
    }

    public bool IsSettingsOpen => settingsRoot != null && settingsRoot.activeSelf;

    /// <summary>
    /// Used by the MainScene builder. It also makes it simple to replace the
    /// placeholder buttons with a final UI prefab later.
    /// </summary>
    public void Configure(
        Button startButton,
        Button optionsButton,
        Button exitButton,
        GameObject optionsRoot,
        string gameScene)
    {
        UnbindButtons();

        gameStartButton = startButton;
        settingsButton = optionsButton;
        gameExitButton = exitButton;
        settingsRoot = optionsRoot;
        gameplaySceneName = gameScene;

        BindButtons();
    }

    private void Awake()
    {
        BindButtons();
        CloseSettings();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void StartGame()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError("[MainMenu] A gameplay scene has not been assigned.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError($"[MainMenu] '{gameplaySceneName}' is not included in Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OpenSettings()
    {
        if (settingsRoot != null) settingsRoot.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsRoot != null) settingsRoot.SetActive(false);
    }

    public void QuitGame()
    {
        PlayerPrefs.Save();

#if UNITY_EDITOR
        // Application.Quit is ignored in the editor, so make the button
        // testable by ending Play mode there.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindButtons()
    {
        if (gameStartButton != null)
        {
            gameStartButton.onClick.RemoveListener(StartGame);
            gameStartButton.onClick.AddListener(StartGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (gameExitButton != null)
        {
            gameExitButton.onClick.RemoveListener(QuitGame);
            gameExitButton.onClick.AddListener(QuitGame);
        }
    }

    private void UnbindButtons()
    {
        if (gameStartButton != null) gameStartButton.onClick.RemoveListener(StartGame);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        if (gameExitButton != null) gameExitButton.onClick.RemoveListener(QuitGame);
    }
}
