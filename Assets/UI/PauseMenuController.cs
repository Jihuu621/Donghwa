using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private GameObject mainContent;
    [SerializeField] private GameObject soundSettingsPanel;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider effectVolumeSlider;
    [SerializeField] private string mainMenuSceneName = "MainScene";

    public bool IsPaused => pauseOverlay != null && pauseOverlay.activeSelf;

    public void Configure(
        GameObject overlay,
        GameObject pauseMainContent,
        GameObject settingsPanel,
        Slider masterSlider,
        Slider effectSlider,
        string mainScene)
    {
        pauseOverlay = overlay;
        mainContent = pauseMainContent;
        soundSettingsPanel = settingsPanel;
        masterVolumeSlider = masterSlider;
        effectVolumeSlider = effectSlider;
        mainMenuSceneName = mainScene;
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        if (pauseOverlay != null) pauseOverlay.SetActive(false);
        ShowMainContent();
        BindVolumeControls();
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

    private void OnDestroy()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (effectVolumeSlider != null) effectVolumeSlider.onValueChanged.RemoveListener(SetEffectVolume);
    }

    public void TogglePause()
    {
        if (IsPaused) CloseMenu();
        else OpenMenu();
    }

    public void OpenMenu()
    {
        if (pauseOverlay == null) return;
        ShowMainContent();
        pauseOverlay.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseMenu()
    {
        if (pauseOverlay != null) pauseOverlay.SetActive(false);
        ShowMainContent();
        Time.timeScale = 1f;
    }

    public void OpenSoundSettings()
    {
        if (mainContent != null) mainContent.SetActive(false);
        if (soundSettingsPanel != null) soundSettingsPanel.SetActive(true);
        SyncVolumeControls();
    }

    public void CloseSoundSettings()
    {
        ShowMainContent();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void BindVolumeControls()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (effectVolumeSlider != null)
        {
            effectVolumeSlider.onValueChanged.RemoveListener(SetEffectVolume);
            effectVolumeSlider.onValueChanged.AddListener(SetEffectVolume);
        }

        SyncVolumeControls();
    }

    private void SyncVolumeControls()
    {
        SoundManager manager = SoundManager.Instance;
        if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(manager.MasterVolume);
        if (effectVolumeSlider != null) effectVolumeSlider.SetValueWithoutNotify(manager.EffectVolume);
    }

    private void ShowMainContent()
    {
        if (mainContent != null) mainContent.SetActive(true);
        if (soundSettingsPanel != null) soundSettingsPanel.SetActive(false);
    }

    private static void SetMasterVolume(float value)
    {
        SoundManager.Instance.SetMasterVolume(value);
    }

    private static void SetEffectVolume(float value)
    {
        SoundManager.Instance.SetEffectVolume(value);
    }
}

[DisallowMultipleComponent]
public sealed class SoundManager : MonoBehaviour
{
    private const string MasterVolumeKey = "Audio.MasterVolume";
    private const string EffectVolumeKey = "Audio.EffectVolume";

    private static SoundManager instance;

    [SerializeField] private AudioSource effectSource;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float effectVolume = 1f;

    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<SoundManager>();
                if (instance == null)
                {
                    GameObject managerObject = new GameObject(nameof(SoundManager));
                    instance = managerObject.AddComponent<SoundManager>();
                }
            }

            return instance;
        }
    }

    public float MasterVolume => masterVolume;
    public float EffectVolume => effectVolume;

    public event Action<float> MasterVolumeChanged;
    public event Action<float> EffectVolumeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeSceneLoad()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (effectSource == null) effectSource = gameObject.AddComponent<AudioSource>();
        effectSource.playOnAwake = false;
        effectSource.loop = false;
        effectSource.spatialBlend = 0f;

        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        effectVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectVolumeKey, 1f));
        ApplyVolumes();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        AudioListener.volume = masterVolume;
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.Save();
        MasterVolumeChanged?.Invoke(masterVolume);
    }

    public void SetEffectVolume(float value)
    {
        effectVolume = Mathf.Clamp01(value);
        if (effectSource != null) effectSource.volume = effectVolume;
        PlayerPrefs.SetFloat(EffectVolumeKey, effectVolume);
        PlayerPrefs.Save();
        EffectVolumeChanged?.Invoke(effectVolume);
    }

    public void PlayEffect(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || effectSource == null) return;
        effectSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayEffectAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, effectVolume * Mathf.Clamp01(volumeScale));
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = masterVolume;
        if (effectSource != null) effectSource.volume = effectVolume;
    }
}
