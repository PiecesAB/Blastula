using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blastula.Graphics
{
    /// <summary>
    /// Drives the rendering of bullets. For lasers, see LaserRendererNode.
    /// </summary>
	public unsafe partial class BulletRendererManager : Node
    {
        [Export] public Node bulletGraphicsRoot;
        [Export] public MultimeshBullet selectorSample;
        [Export] public MultiMesh multiMeshSample;

        public static BulletRendererManager main;

        private Dictionary<int, string> nameFromID = new Dictionary<int, string>();
        private Dictionary<string, int> IDFromName = new Dictionary<string, int>();
        private Dictionary<int, GraphicInfo> graphicInfoFromID = new Dictionary<int, GraphicInfo>();

        private int registeredCount = 0;
        private MultimeshBullet[] multimeshInstancesByID = null;

        public static Stopwatch debugTimer;

        public const double STAGE_TIME_ROLLOVER = 60.0 * 60.0 * 3.0; // three hours
        public static readonly string STAGE_TIME_NAME = "STAGE_TIME";

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
                    multimeshInstancesByID[nonzeroID] = gi.MakeMultimeshBullet(selectorSample, multiMeshSample, newName);
                }
                int stride = BulletRenderer.strideFromRenderIDs[nonzeroID];
                multimeshInstancesByID[nonzeroID].SetBuffer(
                    BulletRenderer.renderedTransformArrays[nonzeroID],
                    BulletRenderer.bNodesFromRenderIDs[nonzeroID].Count(),
                    stride
                );
            }
            foreach (int removedID in removed)
            {
                if (multimeshInstancesByID[removedID] == null) { continue; }
                int stride = BulletRenderer.strideFromRenderIDs[removedID];
                var singleBlankElement = new float[stride];
                multimeshInstancesByID[removedID].SetBuffer(singleBlankElement, 1, stride);
            }

            // Set stageTime global for shaders
            // Loops every three hours... hopefully nobody makes a three hour stage and witnesses the rollover.
            // Also, for optimization purposes, we're halving the framerate.
            if (FrameCounter.stageFrame % 2 == 0)
            {
                float stageTime = (float)(FrameCounter.GetStageTime() % STAGE_TIME_ROLLOVER);
                RenderingServer.GlobalShaderParameterSet(STAGE_TIME_NAME, stageTime);
            }

            if (Debug.StatsViews.currentMode == "timings") { debugTimer.Stop(); }
        }
    }
}
