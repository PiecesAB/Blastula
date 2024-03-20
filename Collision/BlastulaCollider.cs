using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Runtime.InteropServices;

namespace Blastula.Collision
{
    [GlobalClass]
    [Tool]
    [Icon(Persistent.NODE_ICON_PATH + "/collider.png")]
    public unsafe partial class BlastulaCollider : Node2D
    {
        public enum ShowMode
        {
            Never, Editor, Always
        }

        [Export] public Shape shape = Shape.Circle;
        [Export] public Vector2 size = new Vector2(12, 0);
        [Export] public string objectLayer = "None";
        private int objectLayerID = 0;
        [Export] public ShowMode showMode = ShowMode.Editor;

        [Signal] public delegate void CollisionEventHandler(int bNodeIndex);

        public ObjectColliderInfo* colliderInfo = null;
        public LinkedList<Collision>* collisions = null;
        public IntPtr deletionPtr = IntPtr.Zero;

        public long ID = -1;
        private long IDCounter = 0;

        private bool prevDebugColShapes = false;

        public override void _Ready()
        {
            base._Ready();
            if (Engine.IsEditorHint()) { return; }
            ID = IDCounter++;
            collisions = (LinkedList<Collision>*)Marshal.AllocHGlobal(sizeof(LinkedList<Collision>));
            *collisions = LinkedListFunctions.Create<Collision>();
            colliderInfo = (ObjectColliderInfo*)Marshal.AllocHGlobal(sizeof(ObjectColliderInfo));
            colliderInfo->collisionListPtr = (IntPtr)collisions;
            colliderInfo->shape = shape;
            colliderInfo->size = size;
            colliderInfo->transform = GlobalTransform;
            colliderInfo->colliderID = ID;
            objectLayerID = CollisionManager.GetObjectLayerIDFromName(objectLayer);
            deletionPtr = CollisionSolver.RegisterObject((IntPtr)colliderInfo, objectLayerID);
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (Engine.IsEditorHint() || ID == -1) { return; }
            CollisionSolver.UnregisterObject(deletionPtr, objectLayerID);
            Marshal.FreeHGlobal((IntPtr)colliderInfo);
            Marshal.FreeHGlobal((IntPtr)collisions);
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (Engine.IsEditorHint())
            {
                QueueRedraw();
            }
            else
            {
                if (showMode == ShowMode.Always || prevDebugColShapes) { QueueRedraw(); }
                prevDebugColShapes = Debug.DebugCollision.showCollisionShapes;
                colliderInfo->transform = GlobalTransform;
                while (collisions->count > 0)
                {
                    Collision c = collisions->RemoveHead();
                    EmitSignal(SignalName.Collision, this, c.bNodeIndex);
                }
            }
        }

        public override void _Draw()
        {
            base._Draw();
            if (showMode == ShowMode.Never && !Debug.DebugCollision.showCollisionShapes) { return; }
            if (!Engine.IsEditorHint() && showMode == ShowMode.Editor && !Debug.DebugCollision.showCollisionShapes) { return; }
            Color c = Colors.White;
            if (Debug.DebugCollision.showCollisionShapes) { c = Debug.DebugCollision.collisionShapesColor; }
            switch (shape)
            {
                case Shape.Circle:
                default:
                    DrawCircle(Vector2.Zero, size.X, c);
                    break;
                case Shape.None:
                    break;
            }
        }
    }
}

