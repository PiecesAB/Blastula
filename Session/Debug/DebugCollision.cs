using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Debug
{
    /// <summary>
    /// Debug commands used for collision testing.
    /// </summary>
    public partial class DebugCollision : Node2D
    {
        public static bool showCollisionShapes = false;
        public static Color collisionShapesColor = Colors.Green;

        private bool lastDrewCollisionShapes = false;

        public static DebugConsole.CommandGroup commandGroup = new DebugConsole.CommandGroup
        {
            groupName = "Collision",
            commands = new System.Collections.Generic.List<DebugConsole.Command>()
            {
                new DebugConsole.Command
                {
                    name = "col_shapes",
                    usageTip = "col_shapes {on/off} {color}",
                    description = "Shows the collision shapes of all active colliders handled by Blastula, including bullets and lasers. " +
                                  "This will likely severely hurt performance.",
                    action = (args) =>
                    {
                        showCollisionShapes = !showCollisionShapes;
                        if (args.Count >= 2) { DebugConsole.SetTruthValue(args[1], ref showCollisionShapes); }
                        if (args.Count >= 3) { collisionShapesColor = new Color(args[2]); }
                        DebugConsole.main.Print(
                            $"Collision shapes are now {(showCollisionShapes ? "visible" : "invisible")} with color {collisionShapesColor.ToHtml()}"
                        );
                    }
                }
            }
        };

        public override void _Ready()
        {
            base._Ready();
            ProcessPriority = Persistent.Priorities.RENDER_DEBUG_COLLISIONS;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (!showCollisionShapes && !lastDrewCollisionShapes) { return; }
            lastDrewCollisionShapes = showCollisionShapes;
            QueueRedraw();
        }

        public unsafe static void DrawCollisionShape(CanvasItem canvas, int bNodeIndex)
        {
            if (bNodeIndex < 0 || bNodeIndex >= mqSize || !masterQueue[bNodeIndex].initialized) { return; }
            Transform2D worldTransform = BulletWorldTransforms.Get(bNodeIndex);
            canvas.DrawSetTransformMatrix(worldTransform);
            int bulletRenderID = masterQueue[bNodeIndex].bulletRenderID;
            int laserRenderID = masterQueue[bNodeIndex].laserRenderID;
            GraphicInfo graphicInfo = null;
            if (bulletRenderID >= 0) { graphicInfo = BulletRendererManager.GetGraphicInfoFromID(bulletRenderID); }
            else if (laserRenderID >= 0) { graphicInfo = LaserRendererManager.GetGraphicInfoFromID(laserRenderID); }
            else { return; }
            switch (graphicInfo.collisionShape)
            {
                case Collision.Shape.None:
                    break;
                case Collision.Shape.Circle:
                    Vector2 scale = BulletWorldTransforms.Get(bNodeIndex).Scale;
                    BulletWorldTransforms.Invalidate(bNodeIndex);
                    canvas.DrawCircle(
                        Vector2.Zero, 
                        Mathf.Max(0, graphicInfo.collisionSize.X), 
                        collisionShapesColor
                    );
                    break;
                default:
                    GD.PushWarning("I can't draw this collision shape. Unsupported?");
                    break;
            }
        }

        public unsafe override void _Draw()
        {
            base._Draw();
            if (!showCollisionShapes) { return; }
            for (int bNodeIndex = mqTail; bNodeIndex != mqHead; bNodeIndex = (bNodeIndex + 1) % mqSize)
            {
                if (!masterQueue[bNodeIndex].initialized) { continue; }
                DrawCollisionShape(this, bNodeIndex);
            }
        }
    }
}

