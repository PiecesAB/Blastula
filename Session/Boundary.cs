using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Blastula
{
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/boundary.png")]
    [Tool]
    public unsafe partial class Boundary : Node2D
    {
        public enum Form
        {
            Rectangle, Circle
        }

        [Export] public string ID;
        [Export] public Form form = Form.Rectangle;
        [Export] public Vector2 defaultSize = new Vector2(500, 500);
        [Export] public Control inheritSize = null;
        [Export] public bool updates = false;
        [Export] public bool moveToGlobalPosition = false;
        [Export] public Color colorInEditor = Colors.White;

        public static Dictionary<string, Boundary> boundaryFromID = new Dictionary<string, Boundary>();

        public struct LowLevelInfo
        {
            public Form form;
            public Vector2 center;
            public Vector2 size;
            public Vector2 extent;
        }

        // We can pass it to bullet behaviors.
        public LowLevelInfo* lowLevelInfo = null;

        private Vector2 CalculateSize()
        {
            if (inheritSize == null) { return defaultSize; }
            return inheritSize.Size;
        }

        public static bool IsWithin(LowLevelInfo* boundInfo, Vector2 globalPos, float shrink)
        {
            if (boundInfo == null) { return true; }
            switch (boundInfo->form)
            {
                case Form.Rectangle:
                default:
                    Vector2 tl = boundInfo->center - boundInfo->extent + shrink * Vector2.One;
                    Vector2 br = boundInfo->center + boundInfo->extent - shrink * Vector2.One;
                    return Mathf.Clamp(globalPos.X, tl.X, br.X) == globalPos.X
                        && Mathf.Clamp(globalPos.Y, tl.Y, br.Y) == globalPos.Y;
                case Form.Circle:
                    Vector2 dif = globalPos - boundInfo->center;
                    float maxRadius = Mathf.Max(0, 0.5f * boundInfo->size.X - shrink);
                    return dif.LengthSquared() <= maxRadius * maxRadius;
            }
        }

        public static Vector2 Clamp(LowLevelInfo* boundInfo, Vector2 globalPos, float shrink)
        {
            if (boundInfo == null) { return globalPos; }
            switch (boundInfo->form)
            {
                case Form.Rectangle:
                default:
                    Vector2 tl = boundInfo->center - boundInfo->extent + shrink * Vector2.One;
                    Vector2 br = boundInfo->center + boundInfo->extent - shrink * Vector2.One;
                    return new Vector2(
                        Mathf.Clamp(globalPos.X, tl.X, br.X),
                        Mathf.Clamp(globalPos.Y, tl.Y, br.Y)
                    );
                case Form.Circle:
                    Vector2 dif = globalPos - boundInfo->center;
                    float maxRadius = Mathf.Max(0, 0.5f * boundInfo->size.X - shrink);
                    if (dif.Length() <= maxRadius) { return globalPos; }
                    else { return boundInfo->center + maxRadius * dif.Normalized(); }
            }
        }

        public static Vector2 Wrap(LowLevelInfo* boundInfo, Vector2 globalPos, float shrink)
        {
            if (boundInfo == null) { return globalPos; }
            switch (boundInfo->form)
            {
                case Form.Rectangle:
                default:
                    Vector2 tl = boundInfo->center - boundInfo->extent + shrink * Vector2.One;
                    Vector2 br = boundInfo->center + boundInfo->extent - shrink * Vector2.One;
                    if (tl.X == br.X || tl.Y == br.Y)
                    {
                        return tl; // NO DIVIDE BY ZERO!
                    }
                    float tx = MoreMath.RDMod((globalPos.X - tl.X) / (br.X - tl.X), 1f);
                    float ty = MoreMath.RDMod((globalPos.Y - tl.Y) / (br.Y - tl.Y), 1f);
                    return new Vector2(
                        Mathf.Lerp(tl.X, br.X, tx),
                        Mathf.Lerp(tl.Y, br.Y, ty)
                    );
                case Form.Circle:
                    Vector2 dif = globalPos - boundInfo->center;
                    float maxRadius = Mathf.Max(0, 0.5f * boundInfo->size.X - shrink);
                    float difLen = dif.Length();
                    if (difLen <= maxRadius) { return globalPos; }
                    float excess = difLen - maxRadius;
                    return boundInfo->center - (maxRadius - excess) * dif.Normalized();
            }
        }

        public struct ReflectData
        {
            public Vector2 globalPosition;
            public float rotation;
        }

        public static ReflectData Reflect(LowLevelInfo* boundInfo, ReflectData rdIn, float shrink, bool reflectPerpendicular)
        {
            if (boundInfo == null) { return rdIn; }
            switch (boundInfo->form)
            {
                case Form.Rectangle:
                default:
                    {
                        Vector2 tl = boundInfo->center - boundInfo->extent + shrink * Vector2.One;
                        Vector2 br = boundInfo->center + boundInfo->extent - shrink * Vector2.One;
                        float reflectedRotation = rdIn.rotation;
                        Vector2 reflectedPos = rdIn.globalPosition;
                        if (reflectedPos.X < tl.X)
                        {
                            reflectedRotation = reflectPerpendicular ? 0 : (Mathf.Pi - reflectedRotation);
                            float excess = tl.X - reflectedPos.X;
                            reflectedPos += new Vector2(2 * excess, 0);
                        }
                        if (reflectedPos.X > br.X)
                        {
                            reflectedRotation = reflectPerpendicular ? (-Mathf.Pi) : (Mathf.Pi - reflectedRotation);
                            float excess = reflectedPos.X - br.X;
                            reflectedPos -= new Vector2(2 * excess, 0);
                        }
                        if (reflectedPos.Y < tl.Y)
                        {
                            reflectedRotation = reflectPerpendicular ? (0.5f * Mathf.Pi) : (-reflectedRotation);
                            float excess = tl.Y - reflectedPos.Y;
                            reflectedPos += new Vector2(0, 2 * excess);
                        }
                        if (reflectedPos.Y > br.Y)
                        {
                            reflectedRotation = reflectPerpendicular ? (-0.5f * Mathf.Pi) : (-reflectedRotation);
                            float excess = reflectedPos.Y - br.Y;
                            reflectedPos -= new Vector2(0, 2 * excess);
                        }
                        return new ReflectData { globalPosition = reflectedPos, rotation = reflectedRotation };
                    }
                case Form.Circle:
                    {
                        Vector2 dif = rdIn.globalPosition - boundInfo->center;
                        float axis = Mathf.Atan2(dif.Y, dif.X);
                        float reflectedRotation = 2 * (axis + 0.5f * Mathf.Pi) - rdIn.rotation;
                        float maxRadius = Mathf.Max(0, 0.5f * boundInfo->size.X - shrink);
                        float difLen = dif.Length();
                        float excess = difLen - maxRadius;
                        Vector2 reflectedPos = boundInfo->center + (maxRadius - excess) * dif.Normalized();
                        if (reflectPerpendicular) { reflectedRotation = axis + Mathf.Pi; }
                        return new ReflectData { globalPosition = reflectedPos, rotation = reflectedRotation };
                    }
            }
        }

        public override void _Ready()
        {
            base._Ready();
            if (Engine.IsEditorHint()) { return; }
            boundaryFromID[ID] = this;
            if (moveToGlobalPosition)
            {
                GlobalPosition = Position;
            }
            lowLevelInfo = (LowLevelInfo*)Marshal.AllocHGlobal(sizeof(LowLevelInfo));
            lowLevelInfo->form = form;
            lowLevelInfo->size = CalculateSize();
            lowLevelInfo->extent = 0.5f * lowLevelInfo->size;
            lowLevelInfo->center = GlobalPosition;
            if (!updates) { ProcessMode = ProcessModeEnum.Disabled; }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (Engine.IsEditorHint()) { return; }
            boundaryFromID[ID] = null;
            if (lowLevelInfo != null)
            {
                Marshal.FreeHGlobal((IntPtr)lowLevelInfo);
            }
        }

        public override void _Process(double delta)
        {
            if (Engine.IsEditorHint()) { QueueRedraw(); return; }
            if (lowLevelInfo != null)
            {
                lowLevelInfo->form = form;
                lowLevelInfo->size = CalculateSize();
                lowLevelInfo->extent = 0.5f * lowLevelInfo->size;
                lowLevelInfo->center = GlobalPosition;
            }
        }

        public override void _Draw()
        {
            base._Draw();
            if (!Engine.IsEditorHint()) { return; }
            Vector2 s = CalculateSize();
            Vector2 e = 0.5f * s;
            switch (form)
            {
                case Form.Rectangle:
                default:
                    DrawRect(new Rect2(-e, s), colorInEditor, false, 3f);
                    break;
                case Form.Circle:
                    DrawArc(Vector2.Zero, e.X, 0, Mathf.Tau, 64, colorInEditor, 3f);
                    break;
            }
        }
    }
}
