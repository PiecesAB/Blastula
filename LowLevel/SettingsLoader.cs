using Godot;
using System;
using System.IO;

namespace Blastula
{
    /// <summary>
    /// Loads and applies the game settings.
    /// </summary>
    public partial class SettingsLoader : Node
    {
        /// <summary>
        /// This is also the default when there is no saved settings file.
        /// </summary>
        [Export] private Godot.Collections.Dictionary settings;
        private Godot.Collections.Dictionary defaultSettings;

        public static SettingsLoader main { get; private set; } = null;

        public const string SAVE_PATH = "user://settings.json";

        public static string Get(string key)
        {
            if (main == null) { return default; }
            if (!main.settings.ContainsKey(key)) { return default; }
            return main.settings[key].AsString();
        }

        private static void Apply(string key)
        {
            switch (key)
            {

            }
        }

        private static void ApplyAll()
        {
            if (main == null) { return; }
            foreach (var kvp in main.settings)
            {
                Apply(kvp.Key.ToString());
            }
        }

        public static void Set(string key, string newValue)
        {
            if (main == null) { return; }
            main.settings[key] = newValue;
            Apply(key);
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            defaultSettings = settings;
            Load();
            ApplyAll();
        }

        private static void LoadDefault()
        {
            if (main == null) { return; }
            main.settings = main.defaultSettings;
        }

        /// <summary>
        /// Load settings from the SAVE_PATH file.
        /// </summary>
        public static Error Load()
        {
            if (!Godot.FileAccess.FileExists(SAVE_PATH)) { LoadDefault(); return Error.FileNotFound; }
            Godot.FileAccess settingsFile = Godot.FileAccess.Open(SAVE_PATH, Godot.FileAccess.ModeFlags.Read);
            if (settingsFile.GetError() != Error.Ok) { return settingsFile.GetError(); }
            string jsonString = settingsFile.GetAsText(true);
            settingsFile.Close();
            main.settings = Json.ParseString(jsonString).AsGodotDictionary();
            return Error.Ok;
        }
        
        /// <summary>
        /// Save main.settings to the file at SAVE_PATH.
        /// </summary>
        public static Error Save()
        {
            string jsonString = Json.Stringify(main.settings);
            Godot.FileAccess settingsFile = Godot.FileAccess.Open(SAVE_PATH, Godot.FileAccess.ModeFlags.Write);
            if (settingsFile.GetError() != Error.Ok) { return settingsFile.GetError(); }
            settingsFile.StoreString(jsonString);
            settingsFile.Close();
            return Error.Ok;
        }
    }
}
