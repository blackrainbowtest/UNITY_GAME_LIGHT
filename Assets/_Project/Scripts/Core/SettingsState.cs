using System;

namespace UDA2.Core
{
    [Serializable]
    public class SettingsState
    {
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public string language = "en";
        public bool tutorialShown = false;
        public string controlScheme = "keyboard";
        public bool showSubtitles = true;
    }
}
