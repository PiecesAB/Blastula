using Blastula.Sounds;
using Blastula.VirtualVariables;
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
            if (!main.settings.ContainsKey(key)) { 
                if (main.defaultSettings.ContainsKey(key))
                {
                    main.settings[key] = main.defaultSettings[key];
                    return main.settings[key].AsString();
                }
                else
                {
                    return default;
                }
            }
            return main.settings[key].AsString();
        }

        private static void Apply(string key)
        {
            switch (key)
            {
                case "music":
                    MusicManager.UseMusicSetting(main.settings["music"].AsString());
                    break;
                case "sfx":
                    CommonSFXManager.UseSFXSetting(main.settings["sfx"].AsString());
                    break;
                case "start_life":
                    if (Session.main != null) { Session.main.SetLifeOverride(main.settings["start_life"].AsString()); }
                    break;
                case "start_bomb":
                    if (Session.main != null) { Session.main.SetBombOverride(main.settings["start_bomb"].AsString()); }
                    break;
                case "immortality":
                    Player.settingInvulnerable = (main.settings["immortality"].AsString() == "on");
                    break;
                case "show_bg":
                    BackgroundHolder.SetVisible(main.settings["show_bg"].AsString() == "on");
                    break;
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

        public static void LoadDefault()
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
            if (settingsFile == null) { return Godot.FileAccess.GetOpenError(); }
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
            if (settingsFile == null) { return Godot.FileAccess.GetOpenError(); }
            settingsFile.StoreString(jsonString);
            settingsFile.Close();
            return Error.Ok;
        }
    }
}
