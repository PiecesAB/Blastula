using Godot;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blastula.Graphics
{
    /// <summary>
    /// Drives the rendering of lasers.
    /// </summary>
    public partial class LaserRendererManager : Node
    {
        [Export] public Node laserGraphicsRoot;
        [Export] public MeshInstance2D meshInstanceSample;
        [Export] public ArrayMesh arrayMeshSample;

        public static LaserRendererManager main;

        private System.Collections.Generic.Dictionary<int, string> nameFromID = new System.Collections.Generic.Dictionary<int, string>();
        private System.Collections.Generic.Dictionary<string, int> IDFromName = new System.Collections.Generic.Dictionary<string, int>();
        private System.Collections.Generic.Dictionary<int, GraphicInfo> graphicInfoFromID = new System.Collections.Generic.Dictionary<int, GraphicInfo>();

        private int registeredCount = 0;
        private MeshInstance2D[] meshInstancesByID = null;

        public static Stopwatch debugTimer;

        private void RegisterLaserType(Node curr, string incompleteName)
        {
            string subName = incompleteName + (incompleteName == "" ? "" : "/") + curr.Name;
            if (curr is GraphicInfo)
            {
                if (IDFromName.ContainsKey(subName))
                {
                    GD.PushWarning($"There's a duplicate laser graphic of name {curr.Name}. It will be ignored.");
                    return;
                }
                nameFromID[registeredCount] = subName;
                IDFromName[subName] = registeredCount;
                graphicInfoFromID[registeredCount] = (GraphicInfo)curr;
                //GD.Print($"Laser graphic {subName} registered with ID {registeredCount}");
                registeredCount++;
            }
            foreach (var child in curr.GetChildren()) { RegisterLaserType(child, subName); }
        }

        public static GraphicInfo GetGraphicInfoFromID(int id)
        {
            if (main == null) { return null; }
            if (!main.graphicInfoFromID.ContainsKey(id)) { return null; }
            return main.graphicInfoFromID[id];
        }

        public static int GetIDFromName(string name)
        {
            if (main == null) { return -1; }
            if (!main.IDFromName.ContainsKey(name)) { return -1; }
            return main.IDFromName[name];
        }

        public static string GetNameFromID(int ID)
        {
            if (main == null) { return "None"; }
            if (!main.nameFromID.ContainsKey(ID)) { return "None"; }
            return main.nameFromID[ID];
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            foreach (var child in laserGraphicsRoot.GetChildren()) { RegisterLaserType(child, ""); }
            meshInstancesByID = new MeshInstance2D[registeredCount];
            LaserRenderer.Initialize(registeredCount);
            ProcessPriority = VirtualVariables.Persistent.Priorities.RENDER;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (Debug.StatsViews.currentMode == "timings") { debugTimer = Stopwatch.StartNew(); }
            List<int> removed = LaserRenderer.RenderAll();
            foreach (int nonzeroID in LaserRenderer.nonzeroRenderIDs)
            {
                if (meshInstancesByID[nonzeroID] == null)
                {
                    GraphicInfo gi = graphicInfoFromID[nonzeroID];
                    string newName = nameFromID[nonzeroID];
                    meshInstancesByID[nonzeroID] = gi.MakeLaserMeshInstance(meshInstanceSample, arrayMeshSample, newName);
                }
                Godot.Collections.Array newMesh = new Godot.Collections.Array();
                newMesh.Resize((int)Mesh.ArrayType.Max);
                newMesh[(int)Mesh.ArrayType.Vertex] = LaserRenderer.renderedVertices[nonzeroID];
                newMesh[(int)Mesh.ArrayType.TexUV] = LaserRenderer.renderedUVs[nonzeroID];
                ArrayMesh am = meshInstancesByID[nonzeroID].Mesh as ArrayMesh;
                am.ClearSurfaces();
                am.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleStrip, newMesh);
            }
            foreach (int removedID in removed)
            {
                if (meshInstancesByID[removedID] == null) { continue; }
                ArrayMesh am = meshInstancesByID[removedID].Mesh as ArrayMesh;
                am.ClearSurfaces();
            }
            if (Debug.StatsViews.currentMode == "timings") { debugTimer.Stop(); }
        }
    }
}
