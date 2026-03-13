using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuUI : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        if (AudioSettingsManager.Instance == null)
            return;

        musicSlider.SetValueWithoutNotify(AudioSettingsManager.Instance.CurrentMusic);
        sfxSlider.SetValueWithoutNotify(AudioSettingsManager.Instance.CurrentSfx);

        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
    }

    private void OnDestroy()
    {
        musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
    }

    public void OnMusicSliderChanged(float value)
    {
        AudioSettingsManager.Instance?.SetMusicVolume(value);
    }

    public void OnSfxSliderChanged(float value)
    {
        AudioSettingsManager.Instance?.SetSfxVolume(value);
    }
}