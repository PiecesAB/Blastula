using Blastula.Schedules;
using Blastula.Sounds;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Menus;

public partial class MusicForceUnlockMenu : ListMenu
{
    public Music musicSelectionFromMainMenu;

    public void NoUnlock()
    {
        Close();
        MusicMenuOrchestrator.SwapToSelectionList();
    }

    public void YesUnlock()
    {
        Close();
        MusicMenuOrchestrator.SetMusicEncountered(musicSelectionFromMainMenu);
        MusicMenuOrchestrator.SaveAllEncounteredMusic();
        MusicMenuOrchestrator.SwapToDetails(musicSelectionFromMainMenu);
    }
}
