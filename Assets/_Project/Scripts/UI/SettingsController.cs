using UnityEngine;

using UnityEngine.UI;
using TMPro;
using UDA2.Core;

public class SettingsController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown languageDropdown;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle vibrationToggle;
    public TextMeshProUGUI versionText;
    public GameObject settingsPanel;

    private void OnEnable()
    {
        // Инициализация значений UI из текущих настроек
        languageDropdown.value = SettingsManager.GetLanguageIndex();
        musicSlider.value = SettingsContext.Current.musicVolume;
        sfxSlider.value = SettingsContext.Current.sfxVolume;
        if (vibrationToggle != null)
            vibrationToggle.isOn = SettingsContext.Current.vibrationEnabled;
        if (versionText != null)
            versionText.text = $"Version {Application.version}";
    }

    private void Start()
    {
        // Подписка на события UI
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
    }

    public void OnLanguageChanged(int index)
    {
        var lang = SettingsManager.GetLanguageByIndex(index);
        SettingsContext.Current.language = lang;
        LocalizationManager.SetLanguage(lang);
        SettingsManager.Save(SettingsContext.Current);
    }

    public void OnMusicVolumeChanged(float value)
    {
        SettingsContext.Current.musicVolume = value;
        AudioManager.SetMusicVolume(value);
    }

    public void OnSfxVolumeChanged(float value)
    {
        SettingsContext.Current.sfxVolume = value;
        AudioManager.SetSfxVolume(value);
    }

    public void OnVibrationChanged(bool value)
    {
        SettingsContext.Current.vibrationEnabled = value;
    }

    public void OnApply()
    {
        SettingsManager.Save(SettingsContext.Current);
        settingsPanel.SetActive(false);
    }

    public void OnBack()
    {
        settingsPanel.SetActive(false);
    }

    public void OnResetToDefault()
    {
        SettingsManager.ResetToDefault();
        OnEnable(); // обновить UI
        SettingsManager.Save(SettingsContext.Current);
    }
}
