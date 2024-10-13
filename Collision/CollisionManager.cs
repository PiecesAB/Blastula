using Godot;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blastula.Collision
{
    /// <summary>
    /// This node is meant to be a singleton in the kernel. It executes collision checks every frame,
    /// and handles the setup of collision layers for BNodes and BlastulaColliders.
    /// </summary>
    public partial class CollisionManager : Node
    {
        [Export] public Node objectLayersHolder;
        [Export] public Node bulletLayersHolder;
        /// <summary>
        /// Rays (a.k.a. straight lasers) will split into bullets that attempt to have this much space between them.
        /// </summary>
        [Export] public float raySplitSpacing = 40;

        public static Dictionary<string, int> bulletLayerIDFromName = new Dictionary<string, int>();
        public static Dictionary<string, int> objectLayerIDFromName = new Dictionary<string, int>();
        public static List<List<int>> objectsDetectedByBulletLayers = new List<List<int>>();

        public static int bulletLayerCount = 1;
        public static int objectLayerCount = 1;

        public static Stopwatch debugTimer;

        public const int NONE_BULLET_LAYER = 0;

        public static int GetObjectLayerIDFromName(string name)
        {
            if (!objectLayerIDFromName.ContainsKey(name)) { return 0; }
            return objectLayerIDFromName[name];
        }

        public static int GetBulletLayerIDFromName(string name)
        {
            if (!bulletLayerIDFromName.ContainsKey(name)) { return NONE_BULLET_LAYER; }
            return bulletLayerIDFromName[name];
        }

        private void PopulateLayerInfo()
        {
            foreach (Node o in objectLayersHolder.GetChildren())
            {
                objectLayerIDFromName[o.Name] = objectLayerCount;
                ++objectLayerCount;
            }
            objectsDetectedByBulletLayers.Add(new List<int>());
            foreach (Node b in bulletLayersHolder.GetChildren())
            {
                bulletLayerIDFromName[b.Name] = bulletLayerCount;
                List<int> objectsForBulletLayer = new List<int>();
                foreach (Node o in b.GetChildren())
                {
                    if (objectLayerIDFromName.ContainsKey(o.Name))
                    {
                        objectsForBulletLayer.Add(objectLayerIDFromName[o.Name]);
                    }
                }
                objectsDetectedByBulletLayers.Add(objectsForBulletLayer);
                ++bulletLayerCount;
            }
        }

        public override void _Ready()
        {
            PopulateLayerInfo();
            CollisionSolver.Initialize();
            ProcessPriority = VirtualVariables.Persistent.Priorities.COLLISION;
        }

        public override void _Process(double delta)
        {
            if (Debug.StatsViews.currentMode == "timings") { debugTimer = Stopwatch.StartNew(); }
            CollisionSolver.ExecuteCollisionAll();
            if (Debug.StatsViews.currentMode == "timings") { debugTimer.Stop(); }
        }
    }
}
