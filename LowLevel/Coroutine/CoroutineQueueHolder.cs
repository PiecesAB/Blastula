using Godot;
using System;

namespace Blastula.Coroutine;

/// <summary>
/// It simply holds the subqueues, each for a single process priority of coroutines.
/// </summary>
public partial class CoroutineQueueHolder : Node
{
	public static CoroutineQueueHolder main;

	public override void _Ready()
	{
		main = this;
	}
}
