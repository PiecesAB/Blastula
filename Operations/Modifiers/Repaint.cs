using Blastula.Graphics;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Applies graphics automatically to children in patterns.
    /// Of course nothing's stopping you from using only one graphic,
    /// if you just want to replace them.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/paint.png")]
    public unsafe partial class Repaint : Modifier
    {
        public enum ReplaceMode
        {
            /// <summary>
            /// Set the graphic of this parent of the structure.
            /// </summary>
            Self,
            /// <summary>
            /// Directly sets the graphic of the children.
            /// </summary>
            Direct,
            /// <summary>
            /// Looks deeply into each child for all bullets within that are rendered,
            /// and replaces those graphics.
            /// </summary>
            DeepReplace
        }

        public enum PatternMode
        {
            Clamp,
            Loop,
            Random
        }

        [Export] public ReplaceMode replaceMode = ReplaceMode.DeepReplace;
        [Export] public PatternMode repeatMode = PatternMode.Loop;
        [Export] public string[] graphicsList;
        [Export] public string startOffset = "0";

        private int[] renderIDTabulation;

        public static int SolvePatternIndex(int index, int listLength, PatternMode mode)
        {
            if (listLength <= 0) { return -1; }
            switch (mode)
            {
                case PatternMode.Clamp: return Mathf.Clamp(index, 0, listLength - 1);
                case PatternMode.Loop: default: return MoreMath.RDMod(index, listLength);
                case PatternMode.Random: return RNG.Int(0, listLength);
            }
        }

        private int RenderIDFromChildPos(int childPos)
        {
            int graphicsListIndex = childPos + Solve("startOffset").AsInt32();
            graphicsListIndex = SolvePatternIndex(graphicsListIndex, graphicsList.Length, repeatMode);
            return RenderIDFromName(graphicsListIndex);
        }

        private int RenderIDFromName(int graphicsListIndex)
        {
            if (renderIDTabulation[graphicsListIndex] != -123) { return renderIDTabulation[graphicsListIndex]; }
            return renderIDTabulation[graphicsListIndex] = BulletRendererManager.GetIDFromName(graphicsList[graphicsListIndex]);
        }

        private void DeepReplace(int currStructure, int renderID)
        {
            if (currStructure < 0 || currStructure >= mqSize) { return; }
            if (masterQueue[currStructure].bulletRenderID >= 0)
            {
                BulletRenderer.SetRenderID(currStructure, renderID);
            }
            for (int j = 0; j < masterQueue[currStructure].children.count; ++j)
            {
                DeepReplace(masterQueue[currStructure].children[j], renderID);
            }
        }

        public override void ModifyStructure(int inStructure)
        {
            if (graphicsList.Length == 0) { return; }
            if (inStructure < 0 || inStructure >= mqSize) { return; }
            if (masterQueue[inStructure].children.count == 0 && replaceMode != ReplaceMode.Self) { return; }

            renderIDTabulation = new int[graphicsList.Length];
            for (int i = 0; i < graphicsList.Length; ++i) { renderIDTabulation[i] = -123; }

            if (replaceMode == ReplaceMode.Self)
            {
                BulletRenderer.SetRenderID(inStructure, RenderIDFromChildPos(0));
                return;
            }

            for (int j = 0; j < masterQueue[inStructure].children.count; ++j)
            {
                int childStructure = masterQueue[inStructure].children[j];
                if (childStructure < 0 || childStructure >= mqSize) { continue; }
                switch (replaceMode)
                {
                    case ReplaceMode.Direct:
                        BulletRenderer.SetRenderID(childStructure, RenderIDFromChildPos(j));
                        break;
                    case ReplaceMode.DeepReplace:
                    default:
                        DeepReplace(childStructure, RenderIDFromChildPos(j));
                        break;
                }
            }
        }
    }
}

