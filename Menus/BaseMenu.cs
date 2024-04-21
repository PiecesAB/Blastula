using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Menus
{
    /// <summary>
    /// Base class to handle menus.
    /// </summary>
    public abstract partial class BaseMenu : Control
    {
        /// <summary>
        /// The top of this stack is the current menu that recieves input.
        /// </summary>
        public static Stack<BaseMenu> activeStack = new Stack<BaseMenu>();

        /// <summary>
        /// True if something has been selected, so that control is forbidden.
        /// </summary>
        protected bool controlStunned = false;

        public bool IsActive()
        {
            return activeStack.Count > 0 
                && this == activeStack.Peek();
        }

        public bool IsInStack()
        {
            return activeStack.Contains(this);
        }

        public virtual void Open()
        {
            if (activeStack.Contains(this))
            {
                if (activeStack.Peek() != this)
                {
                    activeStack.Pop().Close();
                }
                ReturnControl();
            }
            else
            {
                activeStack.Push(this);
            }
        }

        public virtual void Close()
        {
            while (activeStack.Contains(this))
            {
                if (activeStack.Peek() != this) 
                { 
                    activeStack.Pop().Close(); 
                }
                else 
                { 
                    activeStack.Pop(); 
                }
            }

            if (activeStack.Count > 0)
            {
                activeStack.Peek().ReturnControl();
            }
        }

        public virtual void ReturnControl()
        {

        }
    }
}

