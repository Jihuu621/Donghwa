using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private string mainMenuSceneName = "MainScene";

    public bool IsPaused => pauseOverlay != null && pauseOverlay.activeSelf;

    public void Configure(GameObject overlay, string mainScene)
    {
        pauseOverlay = overlay;
        mainMenuSceneName = mainScene;
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        if (pauseOverlay != null) pauseOverlay.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        if (IsPaused) CloseMenu();
        else OpenMenu();
    }

    public void OpenMenu()
    {
        if (pauseOverlay == null) return;
        pauseOverlay.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseMenu()
    {
        if (pauseOverlay != null) pauseOverlay.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
