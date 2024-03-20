using Godot;
using Godot.Collections;
using System;
using System.Runtime.InteropServices;

namespace Blastula
{
    /// <summary>
    /// A boundary for the game's main screen(s).
    /// </summary>
    [GlobalClass]
    [Tool]
    public unsafe partial class MainBoundary : Boundary
    {
        public enum MainType
        {
            /// <summary>
            /// The single screen of a one-player game.
            /// </summary>
            Single,
            /// <summary>
            /// The left screen of a two-player game.
            /// </summary>
            Left,
            /// <summary>
            /// The right screen of a two-player game.
            /// </summary>
            Right,
            /// <summary>
            /// Not a boundary name. Used to be the number of main boundary types.
            /// </summary>
            Count
        }

        [Export] public MainType mode = MainType.Single;

        public static MainBoundary[] boundPerMode = new MainBoundary[(int)MainType.Count];
        public static Boundary.LowLevelInfo** mainLowLevelInfos = null;

        /// <summary>
        /// Returns true if this point is on any screen (a.k.a. MainBoundary).
        /// Used to check if a bullet is on screen (negative shrink value to account for the bullet graphic).
        /// </summary>
        public static bool IsOnScreen(Vector2 globalPos, float shrink)
        {
            if (mainLowLevelInfos == null) { return false; }
            for (int i = 0; i < (int)MainType.Count; ++i)
            {
                if (mainLowLevelInfos[i] == null) { continue; }
                if (IsWithin(mainLowLevelInfos[i], globalPos, shrink)) { return true; }
            }
            return false;
        }

        public override void _Ready()
        {
            if (mainLowLevelInfos == null)
            {
                mainLowLevelInfos = (LowLevelInfo**)Marshal.AllocHGlobal(sizeof(LowLevelInfo*) * (int)MainType.Count);
                for (int i = 0; i < (int)MainType.Count; ++i)
                {
                    mainLowLevelInfos[i] = null;
                }
            }
            boundPerMode[(int)mode] = this;
            base._Ready();
            mainLowLevelInfos[(int)mode] = lowLevelInfo;
        }

        public override void _ExitTree()
        {
            mainLowLevelInfos[(int)mode] = null;
            bool noLowLevelInfosRemain = true;
            for (int i = 0; i < (int)MainType.Count; ++i)
            {
                if (mainLowLevelInfos[i] != null) { noLowLevelInfosRemain = false; break; }
            }
            if (noLowLevelInfosRemain)
            {
                Marshal.FreeHGlobal((IntPtr)mainLowLevelInfos);
            }
            boundPerMode[(int)mode] = null;
            base._ExitTree();
        }
    }
}
