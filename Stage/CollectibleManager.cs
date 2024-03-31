using Blastula.Schedules;
using Godot;
using System;

namespace Blastula
{
	public partial class CollectibleManager : Node
	{
        public static CollectibleManager main { get; private set; } = null;

        public override void _Ready()
        {
            base._Ready();
            main = this;
        }
    }
}
