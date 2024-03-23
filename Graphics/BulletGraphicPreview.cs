using Godot;

namespace Blastula.Graphics
{
    /// <summary>
    /// A tool to preview bullet graphics and their collision shape in the editor.
    /// Doesn't support RainbowInfo generation yet.
    /// </summary>
    [Tool]
    public partial class BulletGraphicPreview : Node2D
    {
        [Export] public Node graphicsContainer;
        [Export] public string graphicName = "Default";
        [Export] public bool useMaterial = true;
        [Export] public int countInCircle = 12;
        [Export] public float circleRadiusMultiplier = 1;
        [Export] public Color collisionColor = new Color(0, 0, 0, 0);
        [Export] public bool render = false;
        private GraphicInfo graphicInfo = null;

        public override void _Ready()
        {
            if (!Engine.IsEditorHint()) { QueueFree(); return; }
        }

        private GraphicInfo FindGraphic()
        {
            if (graphicName == "") { return null; }
            Node currChild = graphicsContainer;
            string[] subnames = graphicName.Split("/");
            foreach (string s in subnames)
            {
                if (currChild == null) { return null; }
                currChild = currChild.FindChild(s, true);
            }
            return (currChild != null && currChild is GraphicInfo) ? (GraphicInfo)currChild : null;
        }

        public override void _Process(double delta)
        {
            if (!Engine.IsEditorHint()) { QueueFree(); return; }
            if (render)
            {
                render = false;
                graphicInfo = FindGraphic();
                Material = useMaterial ? graphicInfo?.material : null;
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            base._Draw();
            if (graphicInfo == null) { return; }
            for (int a = 0; a < countInCircle; ++a)
            {
                float prog = a / (float)countInCircle;
                float sc = 0.5f * (graphicInfo.size.X + graphicInfo.size.Y);
                DrawSetTransformMatrix(
                    new Transform2D(
                        prog * Mathf.Tau,
                        0.25f * sc * circleRadiusMultiplier * countInCircle * Vector2.Right.Rotated(prog * Mathf.Tau)
                    )
                );
                DrawTextureRect(
                    graphicInfo.texture,
                    new Rect2(-0.5f * graphicInfo.size, graphicInfo.size),
                    false, graphicInfo.modulate
                );
                switch (graphicInfo.collisionShape)
                {
                    case Collision.Shape.None:
                        break;
                    case Collision.Shape.Circle:
                        DrawCircle(Vector2.Zero, Mathf.Max(0, graphicInfo.collisionSize.X), collisionColor);
                        break;
                    default:
                        GD.PushWarning("I can't draw this collision shape. Unsupported?");
                        break;
                }
            }
        }
    }
}
