using Blastula.Collision;
using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
	/// <summary>
	/// Split a ray (a.k.a. solid or straight laser), created using the Ray operation, into a series of segments along it as children, 
	/// with the expectation of cancelling or deletion purposes. 
	/// But if you can handle the limitations of having created a Ray, perhaps it can be used more creatively.
	/// The segments will have the same graphic, though it's expected they'll be deleted immediately.
	/// </summary>
	/// <remarks>
	/// - This will do nothing if the bullet hasn't been marked as a ray (due to being created)<br/>
	/// - This can also be used externally in the codebase (without needing an operation node).
	/// </remarks>
	[GlobalClass]
	[Icon(Persistent.NODE_ICON_PATH + "/ray.png")]
	public unsafe partial class RaySplit : Modifier
	{
		/// <summary>
		/// The intended spacing between each segment of the ray. 
		/// It may be streched or squashed slightly to produce even segments.
		/// Leave blank to use the default amount in BulletRendererManager.
		/// </summary>
		[Export] public string segmentLength = "";
		/// <summary>
		/// If true, the start and end of the ray will produce a segment. 
		/// Otherwise, there will only be internal segments.
		/// </summary>
		[Export] public bool includeEndpoints = true;
		/// <summary>
		/// If true, this goes into the parent and makes the parent and all other children invisible.
		/// </summary>
		[Export] public bool clearEndpointGraphics = true;

		/// <returns>True when a ray was successfully split.</returns>
		public static bool ModifyStructureExternal(int inStructure, float segmentLength, bool includeEndpoints = true, bool clearEndpointGraphics = true)
		{
			if (inStructure < 0) { return false; }
			if (masterQueue[inStructure].children.count > 0) { return false; }
			if (!masterQueue[inStructure].rayHint) { return false; }
			masterQueue[inStructure].collisionLayer = CollisionManager.NONE_BULLET_LAYER;

			if (segmentLength <= 0) {
				GD.PushError("Tried to place segments of nonpositive length when splitting a ray. Attempting this would certainly crash the game.");
				return false;
			}

			GraphicInfo graphicInfo = BulletRendererManager.GetGraphicInfoFromID(masterQueue[inStructure].bulletRenderID);
			if (graphicInfo == null) {
				return false;
			}

			float rayLength = graphicInfo.size.X * masterQueue[inStructure].transform.Scale.X;
			int internalSegmentCount = Mathf.RoundToInt(rayLength / segmentLength) - 1;

			if (!includeEndpoints && internalSegmentCount <= 0) {
				return false;
			}

			List<int> newChildren = new();

			void SetUpSegment(int childNodeIndex, Vector2 pos)
			{
				masterQueue[childNodeIndex].transform = new Transform2D(0f, Vector2.One, 0, pos);
				masterQueue[childNodeIndex].collisionLayer = CollisionManager.NONE_BULLET_LAYER;
				masterQueue[childNodeIndex].rayHint = false;
				newChildren.Add(childNodeIndex);
			}

			if (includeEndpoints)
			{
				int endSegments = CloneN(inStructure, 2);
				for (int i = 0; i < 2; ++i)
				{
					SetUpSegment(
						(endSegments + i) % mqSize,
						(i == 0) ? Vector2.Zero : rayLength * Vector2.Right
					);
				}
			}

			int internalSegments = CloneN(inStructure, internalSegmentCount);
			for (int i = 0; i < internalSegmentCount; ++i)
			{
				float subLength = rayLength * ((i + 1) / (float)(internalSegmentCount + 1));
				SetUpSegment(
					(internalSegments + i) % mqSize,
					subLength * Vector2.Right
				);
			}

			MakeSpaceForChildren(inStructure, newChildren.Count);
			for (int i = 0; i < newChildren.Count; ++i)
			{
				SetChild(inStructure, i, newChildren[i]);
			}

			BulletRenderer.SetRenderID(inStructure, -1);
			masterQueue[inStructure].transform = Transform2D.Identity;

			if (clearEndpointGraphics)
			{
				int parentIndex = masterQueue[inStructure].parentIndex;
				if (parentIndex < 0) {
					return true; // Still true at this point; we did technically split, but there may be weird consequences. Why is there no parent, anyway?
				}
				BulletRenderer.SetRenderID(parentIndex, -1);
				for (int ci = 0; ci < masterQueue[parentIndex].children.count; ++ci)
				{
					int siblingIndex = masterQueue[parentIndex].children[ci];
					if (siblingIndex < 0 || siblingIndex == inStructure) continue;
					BulletRenderer.SetRenderID(siblingIndex, -1);
				}
			}

			return true;
		}

		public override void ModifyStructure(int inStructure)
		{
			float rayLengthSolved = 0;
			if (segmentLength is null or "")
			{
				rayLengthSolved = BulletRendererManager.main.defaultRaySegmentLength;
			} 
			else
			{
				rayLengthSolved = Solve(PropertyName.segmentLength).AsSingle();
			}

			_ = ModifyStructureExternal(inStructure, rayLengthSolved, includeEndpoints);
		}
	}
}

