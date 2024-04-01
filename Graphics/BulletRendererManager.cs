using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blastula.Graphics
{
    /// <summary>
    /// This node is meant to be a singleton in the kernel. 
    /// Drives the rendering of bullets, not lasers.
    /// </summary>
	public unsafe partial class BulletRendererManager : Node
    {
        /// <summary>
        /// Descendants of this node will have graphics registered using a GraphicInfo,
        /// with names generated as described in UtilityFunctions.PathBuilder.
        /// </summary>
        [Export] public Node bulletGraphicsRoot;
        [Export] public MultimeshBullet selectorSample;
        [Export] public MultiMesh multiMeshSample;

        /// <summary>
        /// The single instance of BulletRendererManager.
        /// </summary>
        public static BulletRendererManager main;

        private Dictionary<int, string> nameFromID = new Dictionary<int, string>();
        private Dictionary<string, int> IDFromName = new Dictionary<string, int>();
        private Dictionary<int, GraphicInfo> graphicInfoFromID = new Dictionary<int, GraphicInfo>();

        private int registeredCount = 0;
        private static MultimeshBullet[] multimeshInstancesByID = null;

        public static Stopwatch debugTimer;

        /// <summary>
        /// Name of the shader global for stage time in seconds.
        /// </summary>
        /// <remarks>
        /// This gives more control to sync shader time with game time. 
        /// Notably, it is also unscaled by the game speed.
        /// </remarks>
        public static readonly string STAGE_TIME_NAME = "STAGE_TIME";
        /// <summary>
        /// After this number of seconds, the stage time shader global is looped back to 0.
        /// </summary>
        /// <remarks>
        /// Due to the nature of a single-precision float, after a day or so, time becomes noticably choppy.
        /// Looping prevents this if you choose to have a game open for such ridiculously long times.
        /// The compromise is that at the looping point, the shader will appear disjoint.
        /// </remarks>
        public const double STAGE_TIME_ROLLOVER = 60.0 * 60.0 * 3.0;

        private void RegisterGraphicInfos(Node root)
        {
            UtilityFunctions.PathBuilder(root, (n, path) =>
            {
                if (n is GraphicInfo)
                {
                    if (IDFromName.ContainsKey(path))
                    {
                        GD.PushWarning($"There's a duplicate bullet graphic at {path}. It will be ignored.");
                        return;
                    }
                    nameFromID[registeredCount] = path;
                    IDFromName[path] = registeredCount;
                    graphicInfoFromID[registeredCount] = (GraphicInfo)n;
                    //GD.Print($"Bullet graphic {path} registered with ID {registeredCount}");
                    registeredCount++;
                }
            }, true);
        }

        public static GraphicInfo GetGraphicInfoFromID(int id)
        {
            if (main == null) { return null; }
            if (!main.graphicInfoFromID.ContainsKey(id)) { return null; }
            return main.graphicInfoFromID[id];
        }

        /// <summary>
        /// Converts a readable name into an internal bullet render ID.
        /// </summary>
        public static int GetIDFromName(string name)
        {
            if (main == null) { return -1; }
            if (!main.IDFromName.ContainsKey(name)) { return -1; }
            return main.IDFromName[name];
        }

        /// <summary>
        /// Converts an internal bullet render ID to a readable name.
        /// </summary>
        public static string GetNameFromID(int ID)
        {
            if (main == null) { return "None"; }
            if (!main.nameFromID.ContainsKey(ID)) { return "None"; }
            return main.nameFromID[ID];
        }

        public static MultimeshBullet GetMultiMeshInstanceFromID(int ID)
        {
            return multimeshInstancesByID[ID];
        }

        public static float GetStageTimeGlobalValue()
        {
            return (float)(FrameCounter.GetStageTime() % STAGE_TIME_ROLLOVER);
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            RegisterGraphicInfos(bulletGraphicsRoot);
            multimeshInstancesByID = new MultimeshBullet[registeredCount];
            BulletRenderer.Initialize(registeredCount);
            ProcessPriority = Persistent.Priorities.RENDER;
            RenderingServer.GlobalShaderParameterAdd(STAGE_TIME_NAME, RenderingServer.GlobalShaderParameterType.Float, 0);
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (Debug.StatsViews.currentMode == "timings") { debugTimer = Stopwatch.StartNew(); }
            List<int> removed = BulletRenderer.RenderAll();
            foreach (int nonzeroID in BulletRenderer.nonzeroRenderIDs)
            {
                if (multimeshInstancesByID[nonzeroID] == null)
                {
                    GraphicInfo gi = graphicInfoFromID[nonzeroID];
                    string newName = nameFromID[nonzeroID];
                    multimeshInstancesByID[nonzeroID] = gi.MakeMultimeshBullet(selectorSample, multiMeshSample, nonzeroID, newName);
                }
                int stride = BulletRenderer.strideFromRenderIDs[nonzeroID];
                // Why does the visible count have 1 added?
                // Because of a hack around a Multimesh AABB problem, in which we render a tiny secret bullet.
                multimeshInstancesByID[nonzeroID].SetBuffer(
                    BulletRenderer.renderedTransformArrays[nonzeroID],
                    BulletRenderer.bNodesFromRenderIDs[nonzeroID].Count() + 1,
                    stride
                );
            }
            foreach (int removedID in removed)
            {
                if (multimeshInstancesByID[removedID] == null) { continue; }
                else
                {
                    multimeshInstancesByID[removedID].QueueFree();
                    multimeshInstancesByID[removedID] = null;
                }
            }

            // Set stageTime global for shaders
            // Loops every three hours... hopefully nobody makes a three hour stage and witnesses the rollover.
            float stageTime = GetStageTimeGlobalValue();
            RenderingServer.GlobalShaderParameterSet(STAGE_TIME_NAME, stageTime);

            if (Debug.StatsViews.currentMode == "timings") { debugTimer.Stop(); }
        }
    }
}
