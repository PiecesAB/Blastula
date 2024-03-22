using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
    /// <summary>
    /// A node used to track a point in 2D space, so that bullet structures can behave in ways relating to these positions.
    /// </summary>
    /// <example>
    /// Aiming and following uses targets.
    /// </example>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/crosshair.png")]
    public unsafe partial class Target : Node2D
    {
        /// <summary>
        /// Globally identify this target. 
        /// </summary>
        /// <remarks>
        /// Multiple targets may have the same ID.
        /// </remarks>
        [Export] public string ID = "Target";
        private int IDNumber = -1;

        private static UnsafeArray<LinkedList<Transform2D>> globalPositions;

        private LinkedList<Transform2D>.Node* myNode = null;

        private static System.Collections.Generic.Dictionary<string, int> IDNumbers = new System.Collections.Generic.Dictionary<string, int>();

        /// <summary>
        /// Gets an internal numerical form of a target ID.
        /// </summary>
        /// <remarks>
        /// Bullet behaviors can't reference strings, because they are managed types.
        /// For optimization purposes (to avoid garbage collection mainly), the data used by a behavior is designed to be unmanaged.
        /// </remarks>
        public static int GetNumberFromID(string ID)
        {
            if (!IDNumbers.ContainsKey(ID)) { return -1; }
            return IDNumbers[ID];
        }

        public static int GetTargetCount(string ID) { return GetTargetCount(GetNumberFromID(ID)); }
        public static int GetTargetCount(int IDNumber)
        {
            if (IDNumber >= globalPositions.count) { return 0; }
            return globalPositions[IDNumber].count;
        }

        /// <summary>
        /// Gets the closest target to pos that has this ID.
        /// </summary>
        public static Transform2D GetClosest(string ID, Vector2 pos) { return GetClosest(GetNumberFromID(ID), pos); }

        /// <summary>
        /// Gets the closest target to pos that has this numerical ID.
        /// </summary>
        public static Transform2D GetClosest(int IDNumber, Vector2 pos)
        {
            Transform2D closest = Transform2D.Identity;
            float distance = float.MaxValue;
            if (IDNumber >= globalPositions.count) { return closest; }
            if (globalPositions[IDNumber].count == 1) { return globalPositions[IDNumber].head->data; }
            LinkedList<Transform2D>.Node* currNode = globalPositions[IDNumber].head;
            while (currNode != null)
            {
                float testDistance = (pos - currNode->data.Origin).Length();
                if (testDistance < distance)
                {
                    closest = currNode->data;
                    distance = testDistance;
                }
                currNode = currNode->next;
            }
            return closest;
        }

        /// <summary>
        /// Gets a pointer to this target's transform, which is updated every frame.
        /// </summary>
        /// <remarks>
        /// Bullet behaviors can't reference classes (such as Node), because they are managed types.
        /// For optimization purposes (to avoid garbage collection mainly), the data used by a behavior is designed to be unmanaged.
        /// </remarks>
        public Transform2D* GetPointerToTransform()
        {
            if (myNode == null) { return null; }
            return &(myNode->data);
        }

        public override void _Ready()
        {
            if (IDNumbers.ContainsKey(ID)) { IDNumber = IDNumbers[ID]; }
            else
            {
                IDNumber = IDNumbers.Count;
                IDNumbers[ID] = IDNumber;
                globalPositions.Expand(IDNumbers.Count, new LinkedList<Transform2D> { count = 0, head = null, tail = null });
            }
            myNode = (globalPositions.array + IDNumber)->AddTail(GlobalTransform);
        }

        public override void _Process(double delta)
        {
            if (myNode != null) { myNode->data = GlobalTransform; }
        }

        public override void _ExitTree()
        {
            if (myNode != null) { (globalPositions.array + IDNumber)->RemoveByNode(myNode); }
            myNode = null;
        }
    }
}
