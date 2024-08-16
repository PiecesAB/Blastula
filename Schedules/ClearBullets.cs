using Blastula.Collision;
using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Clear bullets in various ways.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/deletion.png")]
    public partial class ClearBullets : BaseSchedule
	{
        public enum SelectMode
        {
            /// <summary>
            /// elementDirect is a Blastodisc; elementID is a Blastodisc ID (bullets for all Blastodiscs with the ID are deleted).
            /// </summary>
            Blastodisc,
            /// <summary>
            /// elementDirect is a Node; elementID is a Godot group name.
            /// We delete the bullets for all Blastodiscs which are descendents of the Node(s).
            /// (This is useful for players, enemies, etc.)
            /// </summary>
            NodeContainer,
            /// <summary>
            /// elementDirect is meaningless. elementID is a collision layer name.
            /// </summary>
            CollisionLayer,
            /// <summary>
            /// Delete every bullet everywhere. elementDirect and elementID are meaningless.
            /// </summary>
            All
        }

        public enum DeletionMode
        {
            DeleteNoEffect,
            Delete,
            Cancel
        }

        [Export] public SelectMode selectMode = SelectMode.Blastodisc;
        [Export] public Node elementDirect;
        [Export] public string elementID;
        [Export] public DeletionMode deletionMode = DeletionMode.Delete;

        private unsafe void Delete(int bNodeIndex)
        {
            if (bNodeIndex < 0 || !masterQueue[bNodeIndex].initialized) { return; }
            if (Blastodisc.primordial == null) { MasterQueuePushTree(bNodeIndex); return; }

            switch (deletionMode)
            {
                case DeletionMode.DeleteNoEffect:
                    {
                        MasterQueuePushTree(bNodeIndex);
                    }
                    break;
                case DeletionMode.Delete:
                    {
                        BulletRenderer.ConvertToDeletionEffects(bNodeIndex);
                        Blastodisc.primordial.Inherit(bNodeIndex);
                    }
                    break;
                case DeletionMode.Cancel:
                    {
                        CollectibleManager.Cancel(bNodeIndex);
                        BulletRenderer.ConvertToDeletionEffects(bNodeIndex);
                        Blastodisc.primordial.Inherit(bNodeIndex);
                    }
                    break;
            }
        }

        private unsafe int FindAndGroup(string collisionLayerName = null)
        {
            int collisionLayerID = CollisionManager.GetBulletLayerIDFromName(collisionLayerName ?? "");
            int bulletSpaceCount = MasterQueueCount();
            int newParent = MasterQueuePopOne();
            if (newParent < 0) { return -1; }
            BNode* parentPtr = masterQueue + newParent;
            int addedCount = 0;
            for (int i = 0; i < bulletSpaceCount; ++i)
            {
                int bNodeIndex = (mqTail + i) % mqSize;
                BNode* bNodePtr = masterQueue + bNodeIndex;
                if (!bNodePtr->initialized) { continue; }
                if (collisionLayerName != null && bNodePtr->collisionLayer != collisionLayerID) { continue; }
                // Add bullet of matched layer
                if (addedCount == 0) { MakeSpaceForChildren(newParent, 64); }
                else if (addedCount == parentPtr->children.count) { MakeSpaceForChildren(newParent, 2 * parentPtr->children.count); }
                SetChild(newParent, addedCount, bNodeIndex);
                ++addedCount;
            }
            parentPtr->children.Truncate(addedCount);
            return newParent;
        }

        private unsafe void InternalExecute()
        {
            switch (selectMode)
            {
                case SelectMode.Blastodisc:
                    {
                        if (elementDirect != null)
                        {
                            if (elementDirect is Blastodisc)
                            {
                                Delete(((Blastodisc)elementDirect).masterStructure);
                                ((Blastodisc)elementDirect).masterStructure = -1;
                            }
                        }
                        else if (elementID != null)
                        {
                            if (Blastodisc.allByID.ContainsKey(elementID))
                            {
                                foreach (Blastodisc bd in Blastodisc.allByID[elementID])
                                {
                                    Delete(bd.masterStructure);
                                    bd.masterStructure = -1;
                                }
                            }
                        }
                    }
                    break;
                case SelectMode.NodeContainer:
                    {
                        if (elementDirect != null)
                        {
                            foreach (Node bd in elementDirect.GetDescendants())
                            {
                                if (bd is Blastodisc)
                                {
                                    Delete(((Blastodisc)bd).masterStructure);
                                    ((Blastodisc)bd).masterStructure = -1;
                                }
                            }
                        }
                        else if (elementID != null)
                        {
                            foreach (Node bd in Persistent.GetMainScene().GetTree().GetNodesInGroup(elementID))
                            {
                                if (bd is Blastodisc)
                                {
                                    Delete(((Blastodisc)bd).masterStructure);
                                    ((Blastodisc)bd).masterStructure = -1;
                                }
                            }
                        }
                    }
                    break;
                case SelectMode.CollisionLayer:
                    {
                        // Find and group all such bullets, then delete them.
                        // The group is to reduce the amount of Inherit calls for the primordial Blastodisc.
                        Delete(FindAndGroup(elementID));
                    }
                    break;
                case SelectMode.All:
                    {
                        foreach (Blastodisc bd in Blastodisc.all)
                        {
                            Delete(bd.masterStructure);
                            bd.masterStructure = -1;
                        }
                    }
                    break;
            }
        }

        public override IEnumerator Execute(IVariableContainer source)
        {
            InternalExecute();
            yield break;
        }
    }
}
