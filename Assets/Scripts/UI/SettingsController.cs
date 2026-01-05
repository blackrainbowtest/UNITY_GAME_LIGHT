using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UDA2.Core;

public class SettingsController : MonoBehaviour
{
    private SettingsState _editingState;

    [Header("UI References")]
    public GameObject window;
    public GameObject settingsPanel;

    public TMP_Dropdown languageDropdown;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public Toggle vibrationToggle;
    public TextMeshProUGUI versionText;

    // ===== LIFECYCLE =====

    private void OnEnable()
    {
        _editingState = CopyState(EnsureSettings());

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

        if (uiSlider != null)
            uiSlider.value = _editingState.uiVolume;
        else
            Debug.LogWarning("SettingsController: uiSlider не назначен.", this);

        if (vibrationToggle != null)
            vibrationToggle.isOn = _editingState.vibrationEnabled;
    }

    private void Start()
    {
        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

        if (uiSlider != null)
            uiSlider.onValueChanged.AddListener(OnUiVolumeChanged);

        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
    }

    // ===== OPEN / CLOSE =====

    public void Open()
    {
        SetActiveState(true);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        SetActiveState(false);

        if (UDA2.Audio.AudioManager.Instance != null && SettingsContext.Current != null)
        {
            UDA2.Audio.AudioManager.Instance.SetMusicVolume(SettingsContext.Current.musicVolume);
            UDA2.Audio.AudioManager.Instance.SetSfxVolume(SettingsContext.Current.sfxVolume);
            UDA2.Audio.AudioManager.Instance.SetUiVolume(SettingsContext.Current.uiVolume);
        }
    }

    private void SetActiveState(bool isActive)
    {
        if (window != null)
            window.SetActive(isActive);

        if (settingsPanel != null)
            settingsPanel.SetActive(isActive);
    }

    // ===== UI CALLBACKS =====

    public void OnLanguageChanged(int index)
    {
        var lang = SettingsManager.GetLanguageByIndex(index);
        _editingState.language = lang;
        SettingsContext.SetLanguage(lang);
    }

    public void OnMusicVolumeChanged(float value)
    {
        _editingState.musicVolume = value;

        if (UDA2.Audio.AudioManager.Instance != null)
            UDA2.Audio.AudioManager.Instance.SetMusicVolume(value, 1f);
    }

    public void OnSfxVolumeChanged(float value)
    {
        _editingState.sfxVolume = value;

        if (UDA2.Audio.AudioManager.Instance != null)
            UDA2.Audio.AudioManager.Instance.SetSfxVolume(value, 1f);
    }

    public void OnUiVolumeChanged(float value)
    {
        _editingState.uiVolume = value;

        if (UDA2.Audio.AudioManager.Instance != null)
            UDA2.Audio.AudioManager.Instance.SetUiVolume(value);
    }

    public void OnVibrationChanged(bool value)
    {
        _editingState.vibrationEnabled = value;
    }

    // ===== APPLY / RESET =====

    public void OnApply()
    {
        SettingsContext.Current = CopyState(_editingState);
        SettingsManager.Save(SettingsContext.Current);

        if (UDA2.Audio.AudioManager.Instance != null)
        {
            UDA2.Audio.AudioManager.Instance.SetMusicVolume(_editingState.musicVolume, 1f);
            UDA2.Audio.AudioManager.Instance.SetSfxVolume(_editingState.sfxVolume, 1f);
            UDA2.Audio.AudioManager.Instance.SetUiVolume(_editingState.uiVolume);
        }

        Close();
    }

    public void OnReset()
    {
        _editingState = new SettingsState();

        if (languageDropdown != null)
            languageDropdown.value = GetLanguageIndex(_editingState.language);

        if (musicSlider != null)
            musicSlider.value = _editingState.musicVolume;

        if (sfxSlider != null)
            sfxSlider.value = _editingState.sfxVolume;

        if (uiSlider != null)
            uiSlider.value = _editingState.uiVolume;

        if (vibrationToggle != null)
            vibrationToggle.isOn = _editingState.vibrationEnabled;

        SettingsContext.SetLanguage(_editingState.language);

        if (UDA2.Audio.AudioManager.Instance != null)
        {
            UDA2.Audio.AudioManager.Instance.SetMusicVolume(_editingState.musicVolume, 1f);
            UDA2.Audio.AudioManager.Instance.SetSfxVolume(_editingState.sfxVolume, 1f);
            UDA2.Audio.AudioManager.Instance.SetUiVolume(_editingState.uiVolume);
        }
    }

    // ===== INTERNAL =====

    private SettingsState EnsureSettings()
    {
        if (SettingsContext.Current == null)
            SettingsContext.Current = new SettingsState();

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
            uiVolume = source.uiVolume,
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
