using System;

namespace UDA2.Core
{
    [Serializable]
    public class SettingsState
    {
        public float musicVolume = 0.5f;
        public float sfxVolume = 0.4f;
        public float uiVolume = 0.8f;
        public string language = "en";
        public bool tutorialShown = false;
        public string controlScheme = "touch";
        public bool showSubtitles = true;
        public bool vibrationEnabled = true;
    }
}
