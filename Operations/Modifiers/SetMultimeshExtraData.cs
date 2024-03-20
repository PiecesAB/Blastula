using Blastula.Collision;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Set color or custom data, as used in Multimesh fields. Useful to make bullets fade out or to pass special shader properties.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/nova.png")]
    public unsafe partial class SetMultimeshExtraData : Modifier
    {
        /// <summary>
        /// We expect a Color here. 
        /// </summary>
        [Export] public string color = "";
        /// <summary>
        /// We expect a Vector4 here.
        /// </summary>
        [Export] public string custom = "";

        public static BNodeMultimeshExtras* NewPointer()
        {
            BNodeMultimeshExtras* ret = (BNodeMultimeshExtras*)Marshal.AllocHGlobal(sizeof(BNodeMultimeshExtras));
            ret->color = Colors.White;
            ret->custom = Vector4.Zero;
            return ret;
        }

        public static void Initialize(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return; }
            BNode* bNodePtr = masterQueue + inStructure;
            if (bNodePtr->multimeshExtras == null)
            {
                bNodePtr->multimeshExtras = NewPointer();
            }
        }

        public override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return; }
            Initialize(inStructure);
            BNode* bNodePtr = masterQueue + inStructure;

            if (color != null && color != "")
            {
                bNodePtr->multimeshExtras->color = Solve("color").AsColor();
            }

            if (custom != null && custom != "")
            {
                bNodePtr->multimeshExtras->custom = Solve("custom").AsVector4();
            }
        }
    }
}

