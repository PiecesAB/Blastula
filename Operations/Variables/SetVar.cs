using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Set a temporary variable to use in further calculations.
    /// This also works in schedulers, and can change an existing variable.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/setVar.png")]
    public partial class SetVar : Discrete
    {
        public enum Environment
        {
            Local, Session, Custom
        }

        /// <summary>
        /// The variable's name.
        /// </summary>
        [Export] public string varName = "temp";
        /// <summary>
        /// The scope where you are setting this variable.
        /// </summary>
        [Export] public Environment environment = Environment.Local;
        [Export] public string newValue = "sqrt(pow(3, 2) + pow(4, 2))";
        /// <summary>
        /// Used when environment is Custom. Sets virtual variable if it's a BaseVariableContainer,
        /// otherwise sets a Godot variable.
        /// </summary>
        [Export] public Node customEnvironment = null;

        public override void Run()
        {
            Variant result = ExpressionSolver.Solve(this, "newValue", newValue, out ExpressionSolver.SolveStatus solveStatus);
            if (environment == Environment.Local)
            {
                if (ExpressionSolver.currentLocalContainer != null)
                {
                    ExpressionSolver.currentLocalContainer.SetVar(varName, result);
                }
                else
                {
                    GD.PushWarning("No blastodisc to set variables. Use another scope.");
                }
            }
            else if (environment == Environment.Session)
            {
                ((IVariableContainer)Session.main).SetVar(varName, result);
            }
            else if (environment == Environment.Custom && customEnvironment != null)
            {
                if (customEnvironment is IVariableContainer)
                {
                    ((IVariableContainer)customEnvironment).SetVar(varName, result);
                }
                else
                {
                    customEnvironment.Set(varName, result);
                }
            }
        }
    }
}

