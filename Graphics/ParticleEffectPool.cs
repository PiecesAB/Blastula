using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Graphics
{
    /// <summary>
    /// A pooling strategy for one-shot particle effects, to avoid the overhead of loading new particle Nodes.
    /// These belong in the kernel.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/particleEffectPool.png")]
    public partial class ParticleEffectPool : Node
    {
        /// <summary>
        /// Unique and global identifier for this ParticleEffectPool.
        /// </summary>
        [Export] public string ID;
        /// <summary>
        /// A sample of the particle effect Node.
        /// </summary>
        [Export] public PackedScene sample;
        /// <summary>
        /// The maximum number of particle effect Nodes in this pool. Upon running out, some effects will be missing.
        /// </summary>
        [Export] public int count = 50;

        private Node2D[] instances;
        private int nextIndex = 0;
        private static Dictionary<string, ParticleEffectPool> poolByID = new Dictionary<string, ParticleEffectPool>();

        /// <summary>
        /// Places and activates a particle effect with the given pool ID, at the given position.
        /// </summary>
        public static void PlayEffect(string ID, Vector2 position)
        {
            if (!poolByID.ContainsKey(ID)) { return; }
            ParticleEffectPool pool = poolByID[ID];
            Node2D effect = pool.instances[pool.nextIndex];
            effect.GlobalPosition = position;
            if (effect is CpuParticles2D)
            {
                // Assume it is a one-shot particle effect
                ((CpuParticles2D)effect).Restart();
                ((CpuParticles2D)effect).Emitting = true;
            }
            pool.nextIndex = (pool.nextIndex + 1) % pool.count;
        }

        public override void _Ready()
        {
            base._Ready();
            if (poolByID.ContainsKey(ID)) { GD.PushWarning("Duplicate ID for ParticleEffectPool."); }
            poolByID[ID] = this;
            instances = new Node2D[count];
            for (int i = 0; i < count; ++i)
            {
                AddChild(instances[i] = sample.Instantiate<Node2D>());
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            poolByID.Remove(ID);
        }
    }
}
