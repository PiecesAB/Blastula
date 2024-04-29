using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Menus
{
    /// <summary>
    /// Item that can be highlighted / selected in a list.
    /// </summary>
    public partial class ListNode : BaseMenu
    {
        /// <summary>
        /// This causes the menu item to change appearance when the selection state is changed.
        /// It expects the animations to have particular names:<br/>
        /// "Normal" = Menu item that isn't highlighted. Expected to be the starting state.
        /// "Highlight" = Menu item that is highlighted but not yet selected.
        /// "Select" = Menu item that just got selected.
        /// </summary>
        [Export] public AnimationPlayer animationPlayer;
        /// <summary>
        /// If false, the menu will skip over this node.
        /// </summary>
        [Export] public bool selectable = true;
        /// <summary>
        /// If false, pressing the select button will not do anything (in a ListMenu).
        /// </summary>
        [Export] public bool performsActionOnSelect = true;
        [Signal] public delegate void SelectActionEventHandler();

        public bool currentlyHighlighted { get; private set; } = false;

        public void Highlight()
        {
            currentlyHighlighted = true;
            if (animationPlayer != null) { animationPlayer.Play("Highlight"); }
        }

        public void Normal()
        {
            currentlyHighlighted = false;
            if (animationPlayer != null) { animationPlayer.Play("Normal"); }
        }

        public void Select()
        {
            if (animationPlayer != null) { animationPlayer.Play("Select"); }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            // Play the animation in unscaled time.
            if (animationPlayer != null && animationPlayer.Active)
            {
                animationPlayer.Pause();
                animationPlayer.Advance(1.0 / Persistent.SIMULATED_FPS);
            }
        }
    }
}

