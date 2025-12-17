using UnityEngine;

using UnityEngine.UI;
using TMPro;
using UDA2.Core;

public class SettingsController : MonoBehaviour
{
    // Temporary state for editing (not applied until Apply is pressed)
    private SettingsState _editingState;
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
        // Copy the current state to the temporary editing state
        _editingState = CopyState(EnsureSettings());

        // Initialize UI values from the temporary editing state
        if (languageDropdown != null)
            languageDropdown.value = GetLanguageIndex(_editingState.language);
        else
            Debug.LogWarning("SettingsController: languageDropdown не назначен.", this);

        if (musicSlider != null)
            musicSlider.value = _editingState.musicVolume;
        else
            Debug.LogWarning("SettingsController: musicSlider не назначен.", this);

        if (sfxSlider != null)
            sfxSlider.value = _editingState.sfxVolume;
        else
            Debug.LogWarning("SettingsController: sfxSlider не назначен.", this);
        if (vibrationToggle != null)
            vibrationToggle.isOn = _editingState.vibrationEnabled;
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
        var lang = SettingsManager.GetLanguageByIndex(index);
        _editingState.language = lang;
        SettingsContext.SetLanguage(lang); // Update UI for language change
    }

    public void OnMusicVolumeChanged(float value)
    {
        _editingState.musicVolume = value;
        AudioManager.SetMusicVolume(value);
    }

    public void OnSfxVolumeChanged(float value)
    {
        _editingState.sfxVolume = value;
        AudioManager.SetSfxVolume(value);
    }

    public void OnVibrationChanged(bool value)
    {
        _editingState.vibrationEnabled = value;
    }

    // Вызывается кнопкой "Apply"
    public void OnApply()
    {
        // Apply the temporary state to the global context and save
        SettingsContext.Current = CopyState(_editingState);
        SettingsManager.Save(SettingsContext.Current);
        Close();
    }

    // Вызывается кнопкой "Reset"
    public void OnReset()
    {
        // Reset the temporary state to default values (does not affect saved data)
        _editingState = new SettingsState();
        if (languageDropdown != null)
            languageDropdown.value = GetLanguageIndex(_editingState.language);
        if (musicSlider != null)
            musicSlider.value = _editingState.musicVolume;
        if (sfxSlider != null)
            sfxSlider.value = _editingState.sfxVolume;
        if (vibrationToggle != null)
            vibrationToggle.isOn = _editingState.vibrationEnabled;
        SettingsContext.SetLanguage(_editingState.language); // Update UI for language change
    }

    public void Open()
    {
        SetActiveState(true);
    }

    public void Close()
    {
        // On close, just discard the temporary state, do not touch the global context
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

    private static SettingsState CopyState(SettingsState source)
    {
        if (source == null)
            return new SettingsState();

        return new SettingsState
        {
            musicVolume = source.musicVolume,
            sfxVolume = source.sfxVolume,
            language = source.language,
            tutorialShown = source.tutorialShown,
            controlScheme = source.controlScheme,
            showSubtitles = source.showSubtitles,
            vibrationEnabled = source.vibrationEnabled
        };
    }

    private int GetLanguageIndex(string lang)
    {
        if (string.IsNullOrEmpty(lang))
            return 0;

        var languages = SettingsManager.SupportedLanguages;
        for (int i = 0; i < languages.Length; i++)
        {
            if (languages[i] == lang)
                return i;
        }

        return 0;
    }
}
