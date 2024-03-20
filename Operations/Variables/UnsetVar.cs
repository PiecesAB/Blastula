using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Remove a temporary variable.
    /// Not necessary, but gives you the peace of mind that memory won't be occupied by unused variables.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/unsetVar.png")]
    public partial class UnsetVar : Discrete
    {
        /// <summary>
        /// The variable's name.
        /// </summary>
        [Export] public string varName = "temp";
        /// <summary>
        /// The scope where you are setting this variable.
        /// </summary>
        [Export] public SetVar.Environment environment = SetVar.Environment.Local;
        /// <summary>
        /// Used when environment is Custom. Sets virtual variable if it's a BaseVariableContainer,
        /// otherwise sets a Godot variable.
        /// </summary>
        [Export] public Node customEnvironment = null;

        public override void Run()
        {
            if (environment == SetVar.Environment.Local)
            {
                if (ExpressionSolver.currentLocalContainer != null)
                {
                    ExpressionSolver.currentLocalContainer.ClearVar(varName);
                }
                else
                {
                    GD.PushWarning("No blastodisc to unset variables. Use another scope.");
                }
            }
            else if (environment == SetVar.Environment.Session)
            {
                ((IVariableContainer)Session.main).ClearVar(varName);
            }
            else if (environment == SetVar.Environment.Custom && customEnvironment != null)
            {
                if (customEnvironment is IVariableContainer)
                {
                    ((IVariableContainer)customEnvironment).ClearVar(varName);
                }
            }
        }
    }
}

