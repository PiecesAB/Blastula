using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Graphics
{
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/particleEffectPool.png")]
    public partial class ParticleEffectPool : Node
    {
        [Export] public string ID;
        [Export] public PackedScene sample;
        [Export] public int count = 50;

        private Node2D[] instances;
        private int nextIndex = 0;
        private static Dictionary<string, ParticleEffectPool> poolByID = new Dictionary<string, ParticleEffectPool>();

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
