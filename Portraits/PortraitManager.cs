using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Portraits;

/// <summary>
/// As is usual for manager scripts, there should be only one in the kernel scene.
/// </summary>
[GlobalClass]
[Icon(Persistent.NODE_ICON_PATH + "/portraitManager.png")]
public partial class PortraitManager : Node
{
    private Dictionary<string, PortraitEntry> portraitEntriesByPath = new();
    public static PortraitManager main;

    public PortraitController GetPortraitClone(string nodeName)
    {
        if (!portraitEntriesByPath.TryGetValue(nodeName, out PortraitEntry pc)) { return null; }
        else { return pc.portraitScene.Instantiate<PortraitController>(); }
    }

    public override void _Ready()
    {
        base._Ready();
        main = this;
        UtilityFunctions.PathBuilder(
            this,
            (c, path) => {
                if (c is PortraitEntry pe)
                {
                    portraitEntriesByPath[path] = pe;
                }
            },
            true
        );
    }
}
