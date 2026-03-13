using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    private const string MusicPrefKey = "MusicVolume";
    private const string SfxPrefKey = "SfxVolume";

    private const float MinVolume = 0.0001f;

    private float currentMusic = 1f;
    private float currentSfx = 1f;

    public float CurrentMusic => currentMusic;
    public float CurrentSfx => currentSfx;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
        ApplyVolumes();
    }

    private void OnEnable()
    {
        // Si iscrive all'evento solo se è l'istanza principale sopravvissuta
        if (Instance == this)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Start()
    {
        ApplyVolumes();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        currentMusic = Mathf.Clamp(value, MinVolume, 1f);
        audioMixer.SetFloat("MusicVolume", LinearToDb(currentMusic));
        PlayerPrefs.SetFloat(MusicPrefKey, currentMusic);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        currentSfx = Mathf.Clamp(value, MinVolume, 1f);
        audioMixer.SetFloat("SFXVolume", LinearToDb(currentSfx));
        PlayerPrefs.SetFloat(SfxPrefKey, currentSfx);
        PlayerPrefs.Save();
    }

    public void LoadVolumes()
    {
        currentMusic = Mathf.Clamp(PlayerPrefs.GetFloat(MusicPrefKey, 1f), MinVolume, 1f);
        currentSfx = Mathf.Clamp(PlayerPrefs.GetFloat(SfxPrefKey, 1f), MinVolume, 1f);
    }

    public void ApplyVolumes()
    {
        audioMixer.SetFloat("MusicVolume", LinearToDb(currentMusic));
        audioMixer.SetFloat("SFXVolume", LinearToDb(currentSfx));
    }

    private float LinearToDb(float value)
    {
        return Mathf.Log10(Mathf.Clamp(value, MinVolume, 1f)) * 20f;
    }
}