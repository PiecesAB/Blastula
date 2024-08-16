using Blastula.Coroutine;
using Godot;
using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Blastula
{
	/// <summary>
	/// A sort of bonus effect class; draws a line between the player and bullet when graze occurs.
	/// </summary>
	public partial class GrazeLines : Node
	{
		[Export] public int maxLineCount = 32;

		private Line2D[] lines = null;
		private ulong[] iterations = null;

		private int nextLine = 0;

		private static GrazeLines main = null;
		private Vector2[] defaultPoints;

        public override void _Ready()
        {
            base._Ready();
			main = this;
			lines = new Line2D[maxLineCount];
			iterations = new ulong[maxLineCount];
			lines[0] = GetChild(0) as Line2D;
			defaultPoints = lines[0].Points;
			iterations[0] = 0;
			for (int i = 1; i < maxLineCount; ++i)
			{
				lines[i] = lines[0].Duplicate(7) as Line2D;
				lines[0].GetParent().AddChild(lines[i]);
                iterations[i] = 0;
            }
        }

		public static IEnumerator ShowLine(Vector2 start, Vector2 end)
		{
			if (main == null) { yield break; }
			int currIndex = main.nextLine;
			main.nextLine = (main.nextLine + 1) % main.maxLineCount;
			ulong currIteration = ++main.iterations[currIndex];
			main.lines[currIndex].Points = new Vector2[2] { start, end };
			for (int i = 2; i >= 0; --i)
			{
				if (currIteration != main.iterations[currIndex]) { break; }
				main.lines[currIndex].DefaultColor = new Color(1, 1, 1, i / 4f);
				if (i == 0) { main.lines[currIndex].Points = main.defaultPoints; }
				yield return new WaitOneFrame();
			}
		}
    }
}
