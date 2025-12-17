using UnityEngine;

using UnityEngine.UI;
using TMPro;
using UDA2.Core;

public class SettingsController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject window;
    public TMP_Dropdown languageDropdown;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle vibrationToggle;
    public TextMeshProUGUI versionText;
    public GameObject settingsPanel;

    private void OnEnable()
    {
        var settings = EnsureSettings();

        // Инициализация значений UI из текущих настроек
        if (languageDropdown != null)
            languageDropdown.value = SettingsManager.GetLanguageIndex();
        else
            Debug.LogWarning("SettingsController: languageDropdown не назначен.", this);

        if (musicSlider != null)
            musicSlider.value = settings.musicVolume;
        else
            Debug.LogWarning("SettingsController: musicSlider не назначен.", this);

        if (sfxSlider != null)
            sfxSlider.value = settings.sfxVolume;
        else
            Debug.LogWarning("SettingsController: sfxSlider не назначен.", this);
        if (vibrationToggle != null)
            vibrationToggle.isOn = settings.vibrationEnabled;
        if (versionText != null)
            versionText.text = $"Version {Application.version}";
    }

    private void Start()
    {
        // Подписка на события UI
        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
    }

    public void OnLanguageChanged(int index)
    {
        var settings = EnsureSettings();
        var lang = SettingsManager.GetLanguageByIndex(index);
        settings.language = lang;
        LocalizationManager.SetLanguage(lang);
        SettingsManager.Save(settings);
    }

    public void OnMusicVolumeChanged(float value)
    {
        EnsureSettings().musicVolume = value;
        AudioManager.SetMusicVolume(value);
    }

    public void OnSfxVolumeChanged(float value)
    {
        EnsureSettings().sfxVolume = value;
        AudioManager.SetSfxVolume(value);
    }

    public void OnVibrationChanged(bool value)
    {
        EnsureSettings().vibrationEnabled = value;
    }

    // Вызывается кнопкой "Apply"
    public void OnApply()
    {
        SettingsManager.Save(EnsureSettings());
        Close();
    }

    // Вызывается кнопкой "Reset"
    public void OnReset()
    {
        SettingsManager.ResetToDefault();
        var settings = EnsureSettings();
        if (languageDropdown != null)
            languageDropdown.value = SettingsManager.GetLanguageIndex();
        if (musicSlider != null)
            musicSlider.value = settings.musicVolume;
        if (sfxSlider != null)
            sfxSlider.value = settings.sfxVolume;
        if (vibrationToggle != null)
            vibrationToggle.isOn = settings.vibrationEnabled;
    }

    public void Open()
    {
        SetActiveState(true);
    }

    public void Close()
    {
        SetActiveState(false);
    }

    private void SetActiveState(bool isActive)
    {
        if (window != null)
            window.SetActive(isActive);
        if (settingsPanel != null)
            settingsPanel.SetActive(isActive);
    }

    private SettingsState EnsureSettings()
    {
        if (SettingsContext.Current == null)
        {
            SettingsContext.Current = SettingsManager.Load();
            if (SettingsContext.Current == null)
                SettingsContext.Current = new SettingsState();
        }

        return SettingsContext.Current;
    }
}
