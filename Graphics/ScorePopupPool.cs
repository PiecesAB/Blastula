using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula.Graphics
{
    /// <summary>
    /// A singleton for handling the small score popups which appear upon touching a collectible.
    /// </summary>
    /// <remarks>
    /// This is sort of incidental:
    /// It assumes the sample is a Label with a ShaderMaterial, and changes the shader parameter "start_time"
    /// to the current STAGE_TIME shader global, in order to set up the animation.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/particleEffectPool.png")]
    public partial class ScorePopupPool : Node
    {
        /// <summary>
        /// A sample of the score popup effect.
        /// </summary>
        [Export] public PackedScene sample;
        /// <summary>
        /// The maximum number of Nodes in this pool. Upon running out, some effects will be missing.
        /// </summary>
        [Export] public int count = 100;
        [Export] public int effectDurationFrames = 120;

        private static ScorePopupPool main = null;
        private Label[] instances;
        private int nextIndex = 0;
        private ulong lastEffectStageFrame = 0;

        private struct RemovalOrder
        {
            public ulong removalFrame;
            public int instanceIndex;
        }
        /// <summary>
        /// Used to track and remove score popups from rendering.
        /// </summary>
        private Queue<RemovalOrder> removeQueue = new Queue<RemovalOrder>();

        private static string GetScoreString(System.Numerics.BigInteger bigInteger)
        {
            string raw = bigInteger.ToString();
            if (raw.Length <= 6) { return raw; }
            else { return raw[0] + "." + raw.Substring(1, 2) + "e" + (raw.Length - 1).ToString(); }
        }

        /// <summary>
        /// Places and activates a score popup effect.
        /// </summary>
        public static void Play(Vector2 position, System.Numerics.BigInteger scoreNumber, Color color)
        {
            if (main == null) { return; }
            if (main.lastEffectStageFrame == FrameCounter.stageFrame) { return; }
            Label effect = main.instances[main.nextIndex];
            effect.Modulate = color;
            effect.Text = GetScoreString(scoreNumber);
            effect.Visible = true;
            effect.GlobalPosition = position - 0.5f * effect.Size + new Vector2(0, -30);
            float stageTime = BulletRendererManager.GetStageTimeGlobalValue();
            ((ShaderMaterial)effect.Material).SetShaderParameter("start_time", stageTime);
            main.removeQueue.Enqueue(new RemovalOrder {
                instanceIndex = main.nextIndex,
                removalFrame = FrameCounter.stageFrame + (ulong)main.effectDurationFrames 
            });
            main.nextIndex = (main.nextIndex + 1) % main.count;
            main.lastEffectStageFrame = FrameCounter.stageFrame;
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            instances = new Label[count];
            for (int i = 0; i < count; ++i)
            {
                AddChild(instances[i] = sample.Instantiate<Label>());
                // Make them initially invisible
                instances[i].Visible = false;
                instances[i].Material = (Material)instances[i].Material.Duplicate();
                
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            while (removeQueue.Count > 0 && removeQueue.Peek().removalFrame <= FrameCounter.stageFrame)
            {
                RemovalOrder r = removeQueue.Dequeue();
                instances[r.instanceIndex].Visible = false;
            }
        }
    }
}
