using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.VirtualVariables
{
    public interface IPersistForReplay
    {
        Godot.Collections.Dictionary<string, string> CreateReplaySnapshot();
        void LoadReplaySnapshot(Godot.Collections.Dictionary<string, string> snapshot);
    }
}
