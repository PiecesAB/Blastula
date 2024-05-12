using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

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

        /// <summary>
        /// Makes a string backwards!
        /// </summary>
        /// <param name="nullReplacement">If the string is null, the nullReplacement is used as alternate input.</param>
        public static string Reverse(this string s, string nullReplacement = null)
        {
            StringBuilder reversed = new StringBuilder();
            if (s == null) { s = nullReplacement; }
            if (s == null) { return null; }
            for (int i = 0; i < s.Length; ++i)
            {
                reversed.Append(s[s.Length - 1 - i]);
            }
            return reversed.ToString();
        }

        /// <summary>
        /// Gets a list of all descendants of the node in tree order.
        /// </summary>
        public static List<Node> GetDescendants(this Node n, bool includeInternal = false)
        {
            if (n.GetChildCount() == 0) { return new List<Node>(); }
            List<Node> total = new List<Node>();
            foreach (Node child in n.GetChildren(includeInternal))
            {
                total.Add(child);
                total.AddRange(GetDescendants(child, includeInternal));
            }
            return total;
        }
    }
}

