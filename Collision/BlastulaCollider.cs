using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Runtime.InteropServices;

namespace Blastula.Collision
{
    /// <summary>
    /// A collider that only checks when BNodes have entered.
    /// </summary>
    [GlobalClass]
    [Tool]
    [Icon(Persistent.NODE_ICON_PATH + "/collider.png")]
    public unsafe partial class BlastulaCollider : Node2D
    {
        public enum ShowMode
        {
            Never, Editor, Always
        }

        /// <summary>
        /// The shape of the collider.
        /// </summary>
        [Export] public Shape shape = Shape.Circle;
        /// <summary>
        /// The size of the collider, which is directly related to the shape.
        /// </summary>
        [Export] public Vector2 size = new Vector2(12, 0);
        /// <summary>
        /// The layer name used for detecting collisions.
        /// </summary>
        [Export] public string objectLayer = "None";
        private int objectLayerID = 0;
        /// <summary>
        /// Determines when the collider is visible, for debug purposes.
        /// </summary>
        [Export] public ShowMode showMode = ShowMode.Editor;

        [Signal] public delegate void CollisionEventHandler(int bNodeIndex);

        /// <summary>
        /// Low-level collider info for unmanaged reference in the collision solver.
        /// </summary>
        public ObjectColliderInfo* colliderInfo = null;
        private LinkedList<Collision>* collisions = null;
        private IntPtr deletionPtr = IntPtr.Zero;

        /// <summary>
        /// A simple unique ID number for each collider.
        /// </summary>
        /// <remarks>
        /// At the moment this is only used for a strategy to ensure 
        /// the queue of collision events isn't mangled by multithreaded race conditions.
        /// </remarks>
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
                colliderInfo->shape = shape;
                colliderInfo->size = size;
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
            if (Debug.DebugCollision.showCollisionShapes) 
            { 
                c = Debug.DebugCollision.collisionShapesColor; 
                if (showMode == ShowMode.Never)
                {
                    c = new Color(c.R, c.G, c.B, c.A * 0.3f);
                }
            }
            switch (shape)
            {
                case Shape.Circle:
                default:
                    DrawCircle(Vector2.Zero, size.X * (0.5f * (Scale.X + Scale.Y)), c);
                    break;
                case Shape.None:
                    break;
            }
        }
    }
}

