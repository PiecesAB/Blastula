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
        /// <summary>
        /// This determines the timestep of behaviors. The actual framerate may be lower.
        /// </summary>
        public const int SIMULATED_FPS = 60;
        /// <summary>
        /// This is the space between a collision object and a bullet,
        /// such that a lazy bullet feels safe to sleep for a few frames before checking collision again.
        /// </summary>
        public const float LAZY_SAFE_DISTANCE = 140;

        public static class Priorities
        {
            public const int FRAME_COUNTER_INCREMENT = int.MinValue;
            public const int EXECUTE = 40000;
            public const int COLLISION = 50000;
            public const int POST_EXECUTE = 60000;
            public const int RENDER = 70000;
            public const int RENDER_DEBUG_COLLISIONS = 70001;
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
