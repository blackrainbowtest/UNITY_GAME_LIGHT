using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace UDA2.Core
{
    public static class SaveManager
    {
        private static string SavePath(int slot) => Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");

        public static void Save(GameState state, int slot)
        {
            var json = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(SavePath(slot), json);
        }

        public static GameState Load(int slot)
        {
            var path = SavePath(slot);
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<GameState>(json);
        }

        public static bool Exists(int slot) => File.Exists(SavePath(slot));
        public static void Delete(int slot)
        {
            var path = SavePath(slot);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
