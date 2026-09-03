using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sends the player through the shared loading scene before opening a destination scene.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string destinationSceneName = "Boss_test";
    [SerializeField] private string loadingSceneName = "Loading";

    private bool isTransitioning;

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(destinationSceneName))
        {
            Debug.LogError("[SceneTransitionTrigger] A destination scene has not been assigned.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(loadingSceneName))
        {
            Debug.LogError($"[SceneTransitionTrigger] '{loadingSceneName}' is not included in Build Settings.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(destinationSceneName))
        {
            Debug.LogError($"[SceneTransitionTrigger] '{destinationSceneName}' is not included in Build Settings.", this);
            return;
        }

        isTransitioning = true;
        LoadingSceneController.SetNextScene(destinationSceneName);
        SceneManager.LoadScene(loadingSceneName);
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void EnsureTriggerCollider()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null) triggerCollider.isTrigger = true;
    }
}
