using Blastula.Sounds;
using Godot;
using System;

namespace Blastula.Menus;

public partial class MusicDetailsListNode : ListNode
{
	public void PlayCommonSFX(string sfxName)
	{
		CommonSFXManager.StopByName(sfxName);
		CommonSFXManager.PlayByName(sfxName);
	}
}
