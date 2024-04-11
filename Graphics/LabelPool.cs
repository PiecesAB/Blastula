using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;

namespace Blastula.Graphics
{
    /// <summary>
    /// One PopupPool node handles one style of popups which appear upon touching a collectible.
    /// </summary>
    /// <remarks>
    /// This is sort of incidental:
    /// It assumes the sample is a Label with a ShaderMaterial, and changes the shader parameter "start_time"
    /// to the current STAGE_TIME shader global, in order to set up the animation.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/particleEffectPool.png")]
    public partial class LabelPool : Node
    {
        /// <summary>
        /// Globally identify this pool for later access.
        /// </summary>
        [Export] public string ID = "ScoreNumber";
        /// <summary>
        /// A sample of the score popup effect. Must be a label.
        /// </summary>
        [Export] public PackedScene sample;
        /// <summary>
        /// The maximum number of Nodes in this pool. Upon running out, some effects will be missing.
        /// </summary>
        [Export] public int count = 100;
        [Export] public int effectDurationFrames = 120;

        private static Dictionary<string, LabelPool> poolByID = new Dictionary<string, LabelPool>();
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

        public static string GetScoreString(System.Numerics.BigInteger bigInteger)
        {
            string raw = bigInteger.ToString();
            if (raw.Length <= 6) { return raw; }
            else { return raw[0] + "." + raw.Substring(1, 2) + "e" + (raw.Length - 1).ToString(); }
        }

        /// <summary>
        /// Places and activates a score popup effect.
        /// </summary>
        public static void Play(string ID, Vector2 position, string text, Color color)
        {
            if (!poolByID.ContainsKey(ID)) { return; }
            LabelPool pool = poolByID[ID];
            if (pool.lastEffectStageFrame == FrameCounter.stageFrame) { return; }
            Label effect = pool.instances[pool.nextIndex];
            effect.Modulate = color;
            effect.Text = text;
            effect.Visible = true;
            effect.GlobalPosition = position - 0.5f * effect.Size + new Vector2(0, -30);
            float stageTime = BulletRendererManager.GetStageTimeGlobalValue();
            ((ShaderMaterial)effect.Material).SetShaderParameter("start_time", stageTime);
            pool.removeQueue.Enqueue(new RemovalOrder {
                instanceIndex = pool.nextIndex,
                removalFrame = FrameCounter.stageFrame + (ulong)pool.effectDurationFrames 
            });
            pool.nextIndex = (pool.nextIndex + 1) % pool.count;
            pool.lastEffectStageFrame = FrameCounter.stageFrame;
        }

        public override void _Ready()
        {
            base._Ready();
            poolByID[ID] = this;
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
