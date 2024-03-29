using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Blastula.Wind
{
	/// <summary>
	/// A source that produces a field of wind to move bullets using WindForth.
	/// You can inherit from this and create your own custom wind region for special equations.
	/// </summary>
	/// <remarks>
	/// In each channel, BaseWindSource items work together additively.
	/// So in the editor, selecting one BaseWindSource will display the field which results from all BaseWindSource in the channel.
	/// </remarks>
	[GlobalClass, Tool]
	[Icon(Persistent.NODE_ICON_PATH + "/wind.png")]
	public abstract unsafe partial class BaseWindSource : Node2D
	{
        /// <summary>
        /// Which channel this wind source is on. WindForth only responds to one channel at a time.
        /// </summary>
        [Export] public string channel = "Main";
		private string channelEditorStored = "";

		private static Dictionary<string, int> channelNumbers = new Dictionary<string, int>();

        protected struct WindFieldData
		{
			public Transform2D transform;
			public void* otherData;
			// Parameters are global transform, otherData, sample global position. Output is force at the sample position.
			// Yeah this is a bootleg virtual function, but I don't want to deal with static inheritance
			// or other managed implementations that could cause lag at massive scale.
            public delegate*<Transform2D, void*, Vector2, Vector2> sampleForce;
        }

        private int IDNumber = -1;
        /// <summary>
		/// Array index is channel number / IDNumber. IntPtr is WindFieldData*.
		/// </summary>
        private static UnsafeArray<LowLevel.LinkedList<WindFieldData>> all;
        protected LowLevel.LinkedList<WindFieldData>.Node* myNode = null;

		public static Vector2 SampleForce(int channelNumber, Vector2 sampleGlobalPosition)
		{
            if (channelNumber >= all.count || channelNumber < 0 || all[channelNumber].count == 0) { return Vector2.Zero; }
            Vector2 total = Vector2.Zero;
            LowLevel.LinkedList<WindFieldData>.Node* currNode = all[channelNumber].head;
            while (currNode != null)
            {
                WindFieldData* wf = &(currNode->data);
                if (wf->sampleForce != null && wf->transform != default)
                {
                    total += wf->sampleForce(wf->transform, wf->otherData, sampleGlobalPosition);
                }
                currNode = currNode->next;
            }
            return total;
        }

        public static Vector2 SampleForce(string channel, Vector2 sampleGlobalPosition)
		{
			if (!channelNumbers.ContainsKey(channel)) { return Vector2.Zero; }
			int channelNumber = channelNumbers[channel];
			return SampleForce(channelNumber, sampleGlobalPosition);
		}

		public static int GetNumberOfChannel(string channel)
		{
            if (!channelNumbers.ContainsKey(channel)) 
			{
				int newIDNumber = channelNumbers.Count;
                channelNumbers[channel] = newIDNumber;
                return newIDNumber; 
			}
			return channelNumbers[channel];
        }

		protected virtual void AddSelf()
		{
            if (channelNumbers.ContainsKey(channel)) { IDNumber = channelNumbers[channel]; }
            else
            {
                IDNumber = channelNumbers.Count;
				channelNumbers[channel] = IDNumber;
                all.Expand(channelNumbers.Count, new LowLevel.LinkedList<WindFieldData> { count = 0, head = null, tail = null });
            }
            myNode = (all.array + IDNumber)->AddTail(new WindFieldData
			{
				sampleForce = null, 
				otherData = null, 
				transform = default
			});

            if (Engine.IsEditorHint())
			{
				channelEditorStored = channel;
			}
		}

		protected virtual void RemoveSelf()
		{
            if (myNode != null) 
			{
				(all.array + IDNumber)->RemoveByNode(myNode);
				myNode = null;
            }
		}

		public override void _EnterTree()
		{
			base._EnterTree();
			AddSelf();
		}

        public override void _ExitTree()
        {
            base._ExitTree();
			RemoveSelf();
        }

        public override void _Process(double delta)
		{
			if (myNode != null)
			{
				myNode->data = new WindFieldData
				{
					otherData = myNode->data.otherData,
					sampleForce = myNode->data.sampleForce,
					transform = GlobalTransform
				};
			}

			if (Engine.IsEditorHint())
			{
				if (Get("channel").AsString() != channelEditorStored)
				{
					RemoveSelf();
					channel = Get("channel").AsString();
					AddSelf();
				}
				QueueRedraw();
			}
		}

#if TOOLS
		private const float ARROW_SPACING = 64;
		private const float ARROW_FORCE_MULTIPLIER = 0.15f;
		public override void _Draw()
        {
            base._Draw();
			if (!Engine.IsEditorHint()) { return; }
            if ((BlastulaPlugin.selection?.GetInstanceId() ?? ulong.MaxValue) == GetInstanceId())
			{
                foreach (MainBoundary boundary in MainBoundary.boundPerMode)
                {
					if (boundary == null) { continue; }
					Vector2 boundarySize = boundary.CalculateSize();
                    (float minX, float maxX, float minY, float maxY) = (
						boundary.GlobalPosition.X - 0.5f * boundarySize.X,
                        boundary.GlobalPosition.X + 0.5f * boundarySize.X,
                        boundary.GlobalPosition.Y - 0.5f * boundarySize.Y,
                        boundary.GlobalPosition.Y + 0.5f * boundarySize.Y
                    );
					(minX, maxX, minY, maxY) = (
						Mathf.Ceil(minX / ARROW_SPACING) * ARROW_SPACING,
						Mathf.Floor(maxX / ARROW_SPACING) * ARROW_SPACING,
                        Mathf.Ceil(minY / ARROW_SPACING) * ARROW_SPACING,
                        Mathf.Floor(maxY / ARROW_SPACING) * ARROW_SPACING
                    );
                    DrawSetTransformMatrix(GlobalTransform.AffineInverse());
                    for (float x = minX; x < maxX + 0.01f; x += ARROW_SPACING)
					{
                        for (float y = minY; y < maxY + 0.01f; y += ARROW_SPACING)
                        {
							Vector2 pos = new Vector2(x, y);
							DrawCircle(pos, 4f, Colors.Gray);
							Vector2 force = SampleForce(Get("channel").AsString(), pos);
							DrawLine(pos, pos + ARROW_FORCE_MULTIPLIER * force, Colors.Gray, 2);
                        }
                    }
                }
            }
        }
#endif
    }
}
