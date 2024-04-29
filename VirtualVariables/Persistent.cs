using Blastula.Graphics;
using Godot;
using System.Collections.Generic;

namespace Blastula.VirtualVariables
{
    /// <summary>
    /// These variables are meant to persist as long as the program is running.
    /// </summary>
	public partial class Persistent : Node, IVariableContainer
    {
        public const string BLASTULA_ROOT_PATH = "res://addons/Blastula";
        /// <summary>
        /// Used for the editor to find custom icons.
        /// </summary>
        public const string NODE_ICON_PATH = BLASTULA_ROOT_PATH + "/Graphics/NodeIcons";
        public const string KERNEL_PATH = BLASTULA_ROOT_PATH + "/Kernel.tscn";
        public const string MAIN_SCENE_PATH = BLASTULA_ROOT_PATH + "/MainScene.tscn";
        public const string TITLE_MENU_PATH = BLASTULA_ROOT_PATH + "/TitleMenu.tscn";
        public const string SETTINGS_MENU_PATH = BLASTULA_ROOT_PATH + "/SettingsMenu.tscn";
        /// <summary>
        /// This determines the timestep of behaviors. The actual framerate may be lower.
        /// </summary>
        public const int SIMULATED_FPS = 60;
        /// <summary>
        /// This is the space between a collision object and a bullet,
        /// such that a lazy bullet feels safe to sleep for a few frames before checking collision again.
        /// </summary>
        public const float LAZY_SAFE_DISTANCE = 140;

        /// <summary>
        /// Constants used to set ProcessPriority for specific nodes used by Blastula.
        /// Godot's default ProcessPriority is 0.
        /// </summary>
        /// <example>
        /// FRAME_COUNTER_INCREMENT is as early as possible to increment the frame,
        /// so that caches are correctly invalidated, and all other programming gets the same frame count.
        /// </example>
        /// <example>
        /// CONSUME_INPUT is as late as possible to reset player input state,
        /// so that everything in the frame is able to consistently react to the player's input.
        /// This works because inputs are updated by Godot before all _Process.
        /// </example>
        public static class Priorities
        {
            public const int FRAME_COUNTER_INCREMENT = int.MinValue;
            public const int EXECUTE = 40000;
            public const int POST_EXECUTE = 50000;
            public const int COLLISION = 60000;
            public const int RENDER = 70000;
            public const int RENDER_DEBUG_COLLISIONS = 70001;
            public const int MUSIC_MANAGER = 71000;
            public const int PAUSE = 100000;
            public const int CONSUME_INPUT = int.MaxValue;
        }

        public static Persistent main { get; private set; } = null;

        public Dictionary<string, Variant> customData { get; set; } = new Dictionary<string, Variant>();

        public HashSet<string> specialNames { get; set; } = new HashSet<string>()
        {
            "fps", "simulated_fps"
        };

        public override void _Ready()
        {
            base._Ready();
            main = this;
        }


        private static Node2D cachedMainScene = null;
        public static Node2D GetMainScene()
        {
            if (cachedMainScene != null) { return cachedMainScene; }
            if (main == null) { return null; }
            foreach (Node n in main.GetWindow().GetChildren())
            {
                if (n.IsInGroup("MainScene"))
                {
                    return cachedMainScene = (Node2D)n;
                }
            }
            return null;
        }

        public Variant GetSpecial(string varName)
        {
            switch (varName)
            {
                case "simulated_fps":
                    return SIMULATED_FPS;
                case "fps":
                    return (FPSDisplay.main != null) ? FPSDisplay.currFPS : Engine.GetFramesPerSecond();
            }
            return default;
        }
    }
}
