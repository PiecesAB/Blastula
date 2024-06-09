using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
	/// <summary>
	/// Simple but versatile schedule that loads a scene and places it somewhere, or deletes such scenes.
	/// </summary>
	[GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/emplace.png")]
    public partial class Emplace : BaseSchedule
	{
		public enum PlacementMode
		{
			/// <summary>
			/// The new item becomes a parent of the schedule's execution source (must be a Node2D).
			/// </summary>
			Source,
			/// <summary>
			/// The new item is added to a boss with the parent ID.
			/// </summary>
			Boss,
			/// <summary>
			/// The new item is added to a node in the main scene with the group name of parent ID.
			/// </summary>
			Group,
			/// <summary>
			/// The new item is added as a child of the main scene.
			/// </summary>
			MainScene,
			/// <summary>
			/// The new item is provided directly.
			/// </summary>
			Direct,
			/// <summary>
			/// Deletes all previously emplaced items with the reference ID.
			/// </summary>
			Delete
		}

		[Export] PlacementMode placementMode = PlacementMode.Source;
		/// <summary>
		/// The item which will be cloned and placed.
		/// </summary>
		[Export] public PackedScene item;
		[Export] public string parentId = "";
		[Export] public Node2D parentDirect;
		/// <summary>
		/// A name to track emplaced items for later deletion; also used for the act of deletion.
		/// </summary>
		[Export] public string referenceId = "";
		/// <summary>
		/// In Boss and Group placement modes, and this is true,
		/// the scene will be cloned into all possible parents instead of just the first one.
		/// </summary>
		[Export] public bool multi = false;

		private static Dictionary<string, HashSet<Node2D>> tracked = new Dictionary<string, HashSet<Node2D>>();

		private List<Node2D> FindTargetParents()
		{
			switch (placementMode)
			{
				case PlacementMode.Boss:
					return BossEnemy.GetBosses(parentId).ConvertAll((x) => (Node2D)x);
				case PlacementMode.Group:
					List<Node2D> nodesInGroup = new List<Node2D>();
					foreach (var n in Persistent.GetMainScene().GetTree().GetNodesInGroup(parentId))
					{
						if (n is Node2D)
						{
							nodesInGroup.Add((Node2D)n);
						}
					}
					return nodesInGroup;
				default:
                    throw new Exception("Tried to use nonsense multi-find");
            }
		}

		private Node2D FindTargetParent(IVariableContainer source)
		{
			switch (placementMode)
			{
				case PlacementMode.Source:
					return (source is Node2D) ? (Node2D)source : null;
				case PlacementMode.Boss:
                case PlacementMode.Group:
					List<Node2D> targets = FindTargetParents();
					return (targets.Count > 0) ? targets[0] : null;
				case PlacementMode.MainScene:
					return Persistent.GetMainScene();
                case PlacementMode.Direct:
					return parentDirect;
				default:
                    throw new Exception("Tried to use nonsense find");
            }
		}

		private bool CanUseMulti()
		{
			return placementMode == PlacementMode.Boss || placementMode == PlacementMode.Group;
		}

		private void PlaceInParent(Node2D parent)
		{
			if (parent == null) { return; }
			Node2D newItem = item.Instantiate<Node2D>();
			parent.AddChild(newItem);
			if (!string.IsNullOrEmpty(referenceId))
			{
				if (!tracked.ContainsKey(referenceId)) { tracked[referenceId] = new HashSet<Node2D>(); }
				tracked[referenceId].Add(newItem);
				string rid = referenceID;
				newItem.Connect(SignalName.TreeExiting, Callable.From(
					() => { if (tracked.ContainsKey(rid)) { tracked[rid].Remove(newItem); } }
				));
			}
			newItem.Position = Vector2.Zero;
		}

        public override Task Execute(IVariableContainer source)
        {
			if (base.Execute(source) == null) { return null; }

			if (placementMode == PlacementMode.Delete)
			{
				if (tracked.ContainsKey(referenceId))
				{
					foreach (var item in tracked[referenceId]) { 
						if (!IsInstanceValid(item) || item.IsQueuedForDeletion()) { continue; }
						item.QueueFree();
					}
					tracked.Remove(referenceId);
				}
			}
			else if (item == null) { return Task.CompletedTask; }
            else if (multi && CanUseMulti())
			{
				foreach (var parent in FindTargetParents())
				{
					PlaceInParent(parent);
				}
			}
			else
			{
				var parent = FindTargetParent(source);
				PlaceInParent(parent);
			}

			return Task.CompletedTask;
        }
    }
}
