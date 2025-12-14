using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace UDA2.Core
{
    public static class SettingsManager
    {
        private static string SettingsPath => Path.Combine(Application.persistentDataPath, "settings.json");

        public static void Save(SettingsState state)
        {
            var json = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }

        public static SettingsState Load()
        {
            if (!File.Exists(SettingsPath)) return new SettingsState();
            var json = File.ReadAllText(SettingsPath);
            return JsonConvert.DeserializeObject<SettingsState>(json);
        }
    }
}
