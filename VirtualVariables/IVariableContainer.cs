using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.VirtualVariables
{
    /// <summary>
    /// Implemented by classes that can store variables as used in expressions, including custom user-defined variables.
    /// </summary>
    public interface IVariableContainer
    {
        public Dictionary<string, Variant> customData { get; set; }
        public HashSet<string> specialNames { get; set; }

        public void Reset() 
        { 

        }

        public Variant GetSpecial(string varName)
        {
            return default;
        }

        public Variant GetVar(string varName)
        {
            if (specialNames.Contains(varName))
            {
                Variant s = GetSpecial(varName);
                if (s.VariantType != Variant.Type.Nil) { return s; }
            }
            if (!customData.ContainsKey(varName)) { return default; }
            return customData[varName];
        }

        public virtual bool SetVar(string varName, Variant newData)
        {
            if (specialNames.Contains(varName)) { return false; }
            customData[varName] = newData;
            return true;
        }

        public virtual void ClearAllVars()
        {
            customData.Clear();
        }

        public virtual void ClearVar(string varName)
        {
            if (!customData.ContainsKey(varName)) { return; }
            customData.Remove(varName);
        }
    }
}
