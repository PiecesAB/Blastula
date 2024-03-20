using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using System.Text;

namespace Blastula
{
    /// <summary>
    /// Godot already has a powerful expression parser! What luck. But we still need to define some variables.
    /// This class manages variables in order to use expressions in bullet operations.
    /// </summary>
    public static class ExpressionSolver
    {
        public class VarInfo
        {
            /// <summary>
            /// For stored parsing and execution.
            /// </summary>
            public Expression expr = new Expression();
            public bool parseSuccess = false;
            /// <summary>
            /// The variables in the expression, which we need to solve.
            /// </summary>
            public List<string> vars = new List<string>();
            /// <summary>
            /// If the result is constant, store it here.
            /// </summary>
            public Variant constantResult = default;
        }

        public class NodeInfo
        {
            /// <summary>
            /// Key is the name of the variable in Godot.
            /// </summary>
            public Dictionary<string, VarInfo> exprs = new Dictionary<string, VarInfo>();
        }

        /// <summary>
        /// These are all the functions in global scope which (as far as I know) always have the same output per input.
        /// We are concerned to optimize the output of an expression whenever it's constant.
        /// </summary>
        private static HashSet<string> globalDeterminateFunctions = new HashSet<string>()
        {
            "abs", "absf", "absi", "acos", "acosh", "angle_difference", "asin", "asinh", "atan", "atan2", "atanh",
            "bezier_derivative", "bezier_interpolate", "bytes_to_var", "bytes_to_var_with_objects",
            "ceil", "ceilf", "ceili", "clamp", "clampf", "clampi", "cos", "cosh", "cubic_interpolate", "cubic_interpolate_angle", "cubic_interpolate_angle_in_time", "cubic_interpolate_in_time",
            "db_to_linear", "deg_to_rad",
            "ease", "error_string", "exp",
            "floor", "floorf", "floori", "fmod", "fposmod",
            "hash",
            "inverse_lerp", "is_equal_approx", "is_finite", "is_inf", "is_nan", "is_same", "is_zero_approx",
            "lerp", "lerp_angle", "lerpf", "linear_to_db", "log",
            "max", "maxf", "maxi", "min", "minf", "mini", "move_toward",
            "nearest_po2",
            "pingpong", "posmod", "pow", "print", "print_rich", "print_verbose", "printerr", "printraw", "prints", "printt", "push_error", "push_warning",
            "rad_to_deg", "randomize", "remap", "rid_from_int64", "rotate_toward", "round", "roundf", "roundi",
            "sign", "signf", "signi", "sin", "sinh", "smoothstep", "snapped", "snappedf", "snappedi", "sqrt", "step_decimals", "str", "str_to_var",
            "tan", "tanh", "type_convert", "type_string", "typeof",
            "var_to_bytes", "var_to_bytes_with_objects", "var_to_str",
            "weakref", "wrap", "wrapf", "wrapi",
            // Some constructors
            "Vector2", "Vector3", "Vector4", "Color"
        };

        /// <summary>
        /// These are all the functions in global scope which have multiple possible outputs per input.
        /// We are concerned to optimize the output of an expression whenever it's constant.
        /// </summary>
        private static HashSet<string> globalIndeterminateFunctions = new HashSet<string>()
        {
            "instance_from_id", "is_instance_id_valid", "is_instance_valid",
            "rand_from_seed", "randf", "randf_range", "randfn", "randi", "randi_range", "rid_allocate_id",
            "seed",
        };

        /// <summary>
        /// This is the current blastodisc, if it exists. Used to resolve local variables.
        /// </summary>
        public static IVariableContainer currentLocalContainer;

        /// <summary>
        /// Turns instance IDs of Nodes (not BNodes) into their information.
        /// That way we're ready to solve them when the formula is executed.
        /// </summary>
        public static Dictionary<ulong, NodeInfo> nodeToVars = new Dictionary<ulong, NodeInfo>();

        private static bool IsVarStart(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_';
        }

        private static bool IsVarMid(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_' || (c >= '0' && c <= '9');
        }

        public static void PopulateNames(List<string> vars, string s)
        {
            vars.Clear();
            StringBuilder currName = new StringBuilder();
            int currLength = 0;
            foreach (char c in s)
            {
                if (currLength == 0 && IsVarStart(c)) { ++currLength; currName.Append(c); }
                else if (currLength > 0 && IsVarMid(c)) { ++currLength; currName.Append(c); }
                else if (currLength > 0)
                {
                    currLength = 0;
                    string name = currName.ToString();
                    if (!globalDeterminateFunctions.Contains(name))
                    {
                        vars.Add(name);
                    }
                    currName.Clear();
                }
            }
            if (currLength > 0) { vars.Add(currName.ToString()); }
        }

        public static Variant ResolveVariable(string varName)
        {
            if (currentLocalContainer != null)
            {
                Variant tempVarValue = currentLocalContainer.GetVar(varName);
                if (tempVarValue.VariantType != Variant.Type.Nil) { return tempVarValue; }
            }
            if (Session.main != null)
            {
                Variant sessionGet = ((IVariableContainer)Session.main).GetVar(varName);
                if (sessionGet.VariantType != Variant.Type.Nil) { return sessionGet; }
            }
            if (Persistent.main != null)
            {
                Variant persistentGet = ((IVariableContainer)Session.main).GetVar(varName);
                if (persistentGet.VariantType != Variant.Type.Nil) { return persistentGet; }
            }
            return default;
        }

        public static Godot.Collections.Array ResolveVariables(List<string> vars)
        {
            Godot.Collections.Array a = new Godot.Collections.Array();
            a.Resize(vars.Count);
            for (int i = 0; i < vars.Count; ++i) { a[i] = ResolveVariable(vars[i]); }
            return a;
        }

        public enum SolveStatus
        {
            Unsolved, SolvedNormal, SolvedConstant
        }

        public static Variant Solve(Node node, string varName, string varValue, out SolveStatus solveStatus, Variant errorValue = default)
        {
            solveStatus = SolveStatus.Unsolved;
            ulong nodeID = node.GetInstanceId();

            if (!nodeToVars.ContainsKey(nodeID)) { nodeToVars[nodeID] = new NodeInfo(); }
            NodeInfo nodeInfo = nodeToVars[nodeID];

            VarInfo varInfo;
            if (nodeInfo.exprs.ContainsKey(varName))
            {
                varInfo = nodeInfo.exprs[varName];
            }
            else
            {
                varInfo = new VarInfo();
                PopulateNames(varInfo.vars, varValue);
                Error parseError = varInfo.expr.Parse(varValue, varInfo.vars.ToArray());
                if (parseError == Error.Ok)
                {
                    varInfo.parseSuccess = true;
                    nodeInfo.exprs[varName] = varInfo;
                }
            }

            if (varInfo.constantResult.VariantType != Variant.Type.Nil)
            {
                return varInfo.constantResult;
            }

            if (varInfo.parseSuccess)
            {
                Godot.Collections.Array values = ResolveVariables(varInfo.vars);
                Variant result = varInfo.expr.Execute(values, node);
                if (!varInfo.expr.HasExecuteFailed())
                {
                    solveStatus = SolveStatus.SolvedNormal;
                    if (varInfo.vars.Count == 0)
                    {
                        varInfo.constantResult = result;
                        solveStatus = SolveStatus.SolvedConstant;
                    }
                    return result;
                }
            }

            return errorValue;
        }

        public static void Unsolve(Node node, string varName)
        {
            if (nodeToVars.ContainsKey(node.GetInstanceId()))
            {
                NodeInfo nodeInfo = nodeToVars[node.GetInstanceId()];
                if (nodeInfo.exprs.ContainsKey(varName))
                {
                    nodeInfo.exprs.Remove(varName);
                }
            }
        }

        public static void ClearNode(Node node)
        {
            if (nodeToVars.ContainsKey(node.GetInstanceId()))
            {
                nodeToVars.Remove(node.GetInstanceId());
            }
        }
    }
}
