using Godot;
using System;

namespace Blastula
{
    public static class UtilityFunctions
    {
        private static void PathBuilder(Node curr, string currPath, Action<Node, string> Act, bool ignore)
        {
            string nextPath = currPath;
            if (!ignore && !curr.Name.ToString().StartsWith("#"))
            {
                nextPath = currPath + (currPath == "" ? "" : "/") + curr.Name;
            }
            if (nextPath != "")
            {
                Act(curr, nextPath);
            }
            foreach (var child in curr.GetChildren())
            {
                PathBuilder(child, nextPath, Act, false);
            }
        }

        /// <summary>
        /// This is used to convert a node hierarchy into string IDs.<br />
        /// It performs Act function with the node and its new ID, where registering can be handled.<br />
        /// <br />
        /// NodeA                   ID: NodeA<br />
        /// - NodeR                 ID: NodeA/NodeR<br />
        /// - NodeS                 ID: NodeA/NodeS<br />
        /// --- NodeX               ID: NodeA/NodeS/NodeX<br />
        /// --- NodeY               ID: NodeA/NodeS/NodeY<br />
        /// - NodeT                 ID: NodeA/NodeT<br />
        /// --- NodeZ               ID: NodeA/NodeT/NodeZ<br />
        /// - # Holder<br />
        /// --- NodeU               ID: NodeA/NodeU<br />
        /// <br />
        /// If we ignoreRoot, all these IDs would have "NodeA/" omitted, and NodeA itself wouldn't be registered.
        /// </summary>
        public static void PathBuilder(Node root, Action<Node, string> Act, bool ignoreRoot = false)
        {
            PathBuilder(root, "", Act, ignoreRoot);
        }
    }
}

