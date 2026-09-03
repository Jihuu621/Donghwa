using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A visual-only loading screen. It deliberately waits for the configured
/// duration before moving to the requested scene; it does not perform an
/// asynchronous scene load.
/// </summary>
[DisallowMultipleComponent]
public sealed class LoadingSceneController : MonoBehaviour
{
    private static readonly string[] LoadingFrames = { "Loading.", "Loading..", "Loading..." };

    private static string pendingSceneName;

    [Header("Destination")]
    [Tooltip("Used when the Loading scene is opened directly rather than from the main menu.")]
    [SerializeField] private string fallbackSceneName = "Puz";

    [Header("Visual-only animation")]
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField, Min(0.01f)] private float duration = 2.5f;
    [SerializeField, Min(0.01f)] private float frameDuration = 0.3f;

    /// <summary>
    /// Sets the gameplay scene for the next visit to Loading. The fallback is
    /// used if the Loading scene was launched directly in the editor.
    /// </summary>
    public static void SetNextScene(string sceneName)
    {
        pendingSceneName = sceneName;
    }

    private IEnumerator Start()
    {
        string nextSceneName = string.IsNullOrWhiteSpace(pendingSceneName)
            ? fallbackSceneName
            : pendingSceneName;
        pendingSceneName = null;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            int frameIndex = Mathf.FloorToInt(elapsed / frameDuration) % LoadingFrames.Length;
            if (loadingText != null) loadingText.text = LoadingFrames[frameIndex];

            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"[Loading] '{nextSceneName}' is not included in Build Settings.", this);
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        frameDuration = Mathf.Max(0.01f, frameDuration);
    }
}
