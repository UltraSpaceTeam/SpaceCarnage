using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class SettingsController : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;

    private void Start()
    {
        LoadSettings();
        musicSlider.onValueChanged.AddListener(delegate { ApplyAudioSettings(); });
        sfxSlider.onValueChanged.AddListener(delegate { ApplyAudioSettings(); });
        graphicsDropdown.onValueChanged.AddListener(delegate { ApplyGraphicsQuality(graphicsDropdown.value); });
        applyButton.onClick.AddListener(ApplySettings);
        backButton.onClick.AddListener(BackToPause);
    }

    private float VolumeMapping(float sliderValue)
    {
        if (sliderValue <= 0.0001f) return -200f;
        return Mathf.Log10(sliderValue) * 20f;
    }

    private void ApplySettings()
    {
        // Save graphics settings
        PlayerPrefs.SetInt("GraphicsQuality", graphicsDropdown.value);

        // Save audio settings
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);

        PlayerPrefs.Save();

        ApplyAudioSettings();
        ApplyGraphicsQuality(graphicsDropdown.value);

        Debug.Log("Settings applied");

        BackToPause();
    }

    private void ApplyAudioSettings()
    {
        sfxGroup.audioMixer.SetFloat("SFXVolume", VolumeMapping(sfxSlider.value));
        musicGroup.audioMixer.SetFloat("MusicVolume", VolumeMapping(musicSlider.value));
    }

    private void LoadSettings()
    {
        int graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", 1);
        graphicsDropdown.value = graphicsQuality;
        ApplyGraphicsQuality(graphicsQuality);

        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        ApplyAudioSettings();
    }

    private void ApplyGraphicsQuality(int qualityLevel)
    {
        QualitySettings.SetQualityLevel(qualityLevel, true);
    }

    private void BackToPause()
    {
        PauseMenuController.Instance.ReturnToPauseMenu();
    }
}