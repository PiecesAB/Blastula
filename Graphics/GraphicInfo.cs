using Blastula.VirtualVariables;
using Godot;
using System;

namespace Blastula.Graphics
{

    [Icon(Persistent.NODE_ICON_PATH + "/paint.png")]
    [Tool]
    public partial class GraphicInfo : Node
    {
        [Export] public Texture2D texture;
        [Export] public Color modulate = Colors.White;
        [Export] public Vector2 size = Vector2.One * 8;
        [Export] public ShaderMaterial material;
        [Export] public bool unrotatedGraphic = false;
        [Export] public int zIndex = 0;
        [Export] public Node autoRainbow = null;
        [Export] public Collision.Shape collisionShape = Collision.Shape.Circle;
        [Export] public Vector2 collisionSize = new Vector2(12, 0);
        [Flags] public enum ExtraMultimeshFields { Color = 1, CustomData = 2 }
        [Export] public ExtraMultimeshFields extraMultimeshFields = 0;

        public override void _EnterTree()
        {
            if (!Engine.IsEditorHint() && autoRainbow != null && autoRainbow is RainbowInfo)
            {
                RainbowInfo rInfo = autoRainbow as RainbowInfo;
                int rLen = Mathf.Min(rInfo.names.Length, rInfo.colors.Length);
                Node[] children = new Node[rLen];
                for (int i = 0; i < rLen; ++i)
                {
                    Color color = rInfo.colors[i];
                    string sname = rInfo.names[i];
                    GraphicInfo selfClone = (GraphicInfo)Duplicate(7);
                    foreach (Node cloneChild in selfClone.GetChildren())
                    {
                        selfClone.RemoveChild(cloneChild);
                        cloneChild.QueueFree();
                    }
                    selfClone.autoRainbow = null;
                    selfClone.material = (ShaderMaterial)selfClone.material.Duplicate();
                    selfClone.material.SetShaderParameter(rInfo.shaderParamaterName, color);
                    selfClone.Name = sname;
                    children[i] = selfClone;
                }
                foreach (Node child in children)
                {
                    AddChild(child);
                }
            }
        }

        public MultimeshBullet MakeMultimeshBullet(MultimeshBullet selectorSample, MultiMesh multiMeshSample, string newName)
        {
            MultimeshBullet newSelector = (MultimeshBullet)selectorSample.Duplicate(7);
            newSelector.Name = newName;
            selectorSample.GetParent().AddChild(newSelector);
            newSelector.Multimesh = (MultiMesh)multiMeshSample.Duplicate();

            int origCount = newSelector.Multimesh.InstanceCount;
            newSelector.Multimesh.InstanceCount = 0;
            newSelector.Multimesh.UseColors = (extraMultimeshFields & ExtraMultimeshFields.Color) != 0;
            newSelector.Multimesh.UseCustomData = (extraMultimeshFields & ExtraMultimeshFields.CustomData) != 0;
            newSelector.Multimesh.InstanceCount = origCount;

            if (!(newSelector.Multimesh.Mesh is QuadMesh)) { throw new InvalidCastException("Multimesh sample needs quadmesh!!"); }
            QuadMesh newMesh = (QuadMesh)newSelector.Multimesh.Mesh.Duplicate();
            newSelector.Multimesh.Mesh = newMesh;

            newSelector.Texture = texture;
            newSelector.SelfModulate = modulate;
            newMesh.Size = size;
            newSelector.Material = material;
            newSelector.ZIndex = zIndex;

            return newSelector;
        }

        public MeshInstance2D MakeLaserMeshInstance(MeshInstance2D meshInstanceSample, ArrayMesh arrayMeshSample, string newName)
        {
            MeshInstance2D newMI = (MeshInstance2D)meshInstanceSample.Duplicate(7);
            newMI.Name = newName;
            meshInstanceSample.GetParent().AddChild(newMI);
            newMI.Mesh = (ArrayMesh)arrayMeshSample.Duplicate();
            newMI.Texture = texture;
            newMI.SelfModulate = modulate;
            // Size must be rendered by the mesh generation algorithm.
            newMI.Material = material;
            newMI.ZIndex = zIndex;
            return newMI;
        }
    }
}
