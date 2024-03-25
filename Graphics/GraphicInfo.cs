using Blastula.VirtualVariables;
using Godot;
using System;

namespace Blastula.Graphics
{
    /// <summary>
    /// Used to denote graphic info for the BulletRendererManager or LaserRendererManager 
    /// to find and integrate.
    /// </summary>
    [Icon(Persistent.NODE_ICON_PATH + "/paint.png")]
    [Tool]
    public partial class GraphicInfo : Node
    {
        [Export] public Texture2D texture;
        [Export] public Color modulate = Colors.White;
        [Export] public Vector2 size = Vector2.One * 8;
        [Export] public ShaderMaterial material;
        /// <summary>
        /// If true, bullets will be un-rotated to always appear in the texture's rotation.
        /// </summary>
        [Export] public bool unrotatedGraphic = false;
        /// <summary>
        /// The default z-index of the bullet shape.
        /// </summary>
        /// <remarks>
        /// You may dynamically change it during the game using a SetZIndex operation.
        /// </remarks>
        [Export] public int zIndex = 0;
        /// <summary>
        /// If this is a RainbowInfo, auto-generates child GraphicInfos using it.
        /// </summary>
        /// <example>
        /// If you apply a rainbow with names Green and Blue to a GraphicInfo which has ID "Orb/Big", 
        /// It will generate "Orb/Big/Green" and "Orb/Big/Blue" with the appropriate shader parameter changed.
        /// </example>
        [Export] public Node autoRainbow = null;
        /// <summary>
        /// The shape of the bullet collider.
        /// </summary>
        [Export] public Collision.Shape collisionShape = Collision.Shape.Circle;
        /// <summary>
        /// The size of the bullet collider, which is directly related to the shape.
        /// </summary>
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

        public MultimeshBullet MakeMultimeshBullet(MultimeshBullet selectorSample, MultiMesh multiMeshSample, int renderID, string newName)
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
            unsafe
            {
                if (BulletRenderer.zIndexFromRenderIDs != null) 
                {
                    newSelector.ZIndex = BulletRenderer.zIndexFromRenderIDs[renderID];
                }
            }

            return newSelector;
        }

        public MeshInstance2D MakeLaserMeshInstance(MeshInstance2D meshInstanceSample, ArrayMesh arrayMeshSample, int renderID, string newName)
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
            unsafe
            {
                if (LaserRenderer.zIndexFromRenderIDs != null)
                {
                    newMI.ZIndex = LaserRenderer.zIndexFromRenderIDs[renderID];
                }
            }
            return newMI;
        }
    }
}
