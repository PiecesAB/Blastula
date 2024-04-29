using Blastula.Input;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Menus
{
    /// <summary>
    /// Handles menus in which children can be highlighted using up/down or left/right buttons,
    /// and selected using the select button.
    /// </summary>
    public partial class ListMenu : BaseMenu
    {
        public enum Direction
        {
            Vertical, Horizontal
        }
        /// <summary>
        /// Whether the up/down or left/right buttons are used to navigate the menu.
        /// </summary>
        [Export] public Direction direction = Direction.Vertical;
        /// <summary>
        /// If it exists, we expect it to start in the "Closed" state.
        /// When the menu opens, it is put in the "Open" state.
        /// When the menu closes, it is put in the "Closed" state.
        /// </summary>
        [Export] public AnimationPlayer animationPlayer;
        /// <summary>
        /// If true, the back button can be pressed to leave the menu.
        /// </summary>
        [Export] public bool cancelable = true;
        /// <summary>
        /// The index of the initial selected item.
        /// </summary>
        [Export] public int initialSelection = 0;
        /// <summary>
        /// If true, going up past the first element will select the final element.
        /// </summary>
        [Export] public bool wrap = true;
        /// <summary>
        /// Time in frames between the player pressing select button and menu performing its action
        /// (so that the player sees the button selection animation).
        /// </summary>
        [Export] public int waitAfterSelectFrames = 20;

        private List<ListNode> menuNodes = new List<ListNode>();


        private int selectFramesRemaining = -1;

        public int selection { get; private set; } = 0;

        public override void _Ready()
        {
            base._Ready();
            foreach (Node child in GetChildren())
            {
                if (child is ListNode)
                {
                    menuNodes.Add((ListNode)child);
                    AnimationPlayer ap = ((ListNode)child).animationPlayer;
                    if (ap != null) { ap.Active = false; }
                }
            }
        }

        private void EnsureSelectionIsValid()
        {
            if (menuNodes.Count == 0) { selection = -1; return; }

            if (selection >= menuNodes.Count || selection < 0) { selection = 0; }

            // This tries to select subsequent nodes if the intended one is disabled.
            int intendedSelection = selection;
            int nextPossibleSelection = (selection + 1) % menuNodes.Count;
            while (!menuNodes[selection].selectable && nextPossibleSelection != intendedSelection)
            {
                selection = nextPossibleSelection;
                nextPossibleSelection = (selection + 1) % menuNodes.Count;
            }
            if (!menuNodes[selection].selectable) { selection = -1; return; }
        }

        public override void Open()
        {
            base.Open();
            if (animationPlayer != null)
            {
                animationPlayer.Play("Open");
                animationPlayer.Pause();
            }
            controlStunned = false;
            selection = initialSelection;
            selectFramesRemaining = -1;
            EnsureSelectionIsValid();
            for (int i = 0; i < menuNodes.Count; ++i)
            {
                if (menuNodes[i].animationPlayer != null) 
                { 
                    menuNodes[i].animationPlayer.Active = true; 
                }
                menuNodes[i].Normal();
                if (menuNodes[i].animationPlayer != null)
                {
                    // Fast forward to make it appear as if the animation has
                    // been playing since before the menu opened.
                    menuNodes[i].animationPlayer.Advance(1000f);
                }
            }
            menuNodes[selection].Highlight();
        }

        public override void Close()
        {
            base.Close();
            if (animationPlayer != null)
            {
                animationPlayer.Play("Closed");
                animationPlayer.Pause();
            }
            foreach (ListNode ln in menuNodes)
            {
                if (ln.animationPlayer != null) { ln.animationPlayer.Active = false; }
            }
        }

        public override void ReturnControl()
        {
            base.ReturnControl();
            controlStunned = false;
            EnsureSelectionIsValid();
        }

        public void PlayCommonSFX(string sfxName)
        {
            CommonSFXManager.PlayByName(sfxName);
        }

        public void PlayBackSFX()
        {
            CommonSFXManager.PlayByName("Menu/Back");
        }

        private bool HitPrevButton()
        {
            switch (direction)
            {
                case Direction.Vertical: default:
                    return InputManager.ButtonPressedThisFrame("Menu/Up");
                case Direction.Horizontal:
                    return InputManager.ButtonPressedThisFrame("Menu/Left");
            }
        }

        private bool HitNextButton()
        {
            switch (direction)
            {
                case Direction.Vertical: default:
                    return InputManager.ButtonPressedThisFrame("Menu/Down");
                case Direction.Horizontal:
                    return InputManager.ButtonPressedThisFrame("Menu/Right");
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (animationPlayer != null && IsVisibleInTree())
            {
                animationPlayer.Pause();
                animationPlayer.Advance(1.0 / Persistent.SIMULATED_FPS);
            }
            if (!IsActive()) { return; }
            if (controlStunned)
            {
                if (selectFramesRemaining > 0)
                {
                    --selectFramesRemaining;
                    if (selectFramesRemaining == 0)
                    {
                        menuNodes[selection].EmitSignal(ListNode.SignalName.SelectAction);
                    }
                }
            }
            if (menuNodes.Count == 0 || selection < 0 || controlStunned) { return; }

            if (cancelable && InputManager.ButtonPressedThisFrame("Menu/Back"))
            {
                PlayCommonSFX("Menu/Back");
                Close(); return;
            }

            if (menuNodes[selection].performsActionOnSelect && InputManager.ButtonPressedThisFrame("Menu/Select"))
            {
                selectFramesRemaining = waitAfterSelectFrames;
                controlStunned = true;
                menuNodes[selection].Select();
            }

            int oldSelection = selection;
            bool hitNext = HitNextButton();
            bool hitPrev = HitPrevButton();
            if (hitNext && !hitPrev)
            {
                ++selection;
                if (selection >= menuNodes.Count)
                {
                    if (wrap) { selection = 0; }
                    else { selection = menuNodes.Count - 1; }
                }

                while (!menuNodes[selection].selectable)
                {
                    ++selection;
                    if (wrap) { if (selection >= menuNodes.Count) { selection = 0; } }
                    else if (selection == menuNodes.Count) { selection = oldSelection; break; }
                }
            }
            else if (hitPrev && !hitNext)
            {
                --selection;
                if (selection < 0)
                {
                    if (wrap) { selection = menuNodes.Count - 1; }
                    else { selection = 0; }
                }

                while (!menuNodes[selection].selectable)
                {
                    --selection;
                    if (wrap) { if (selection < 0) { selection = menuNodes.Count - 1; } }
                    else if (selection == 0) { selection = oldSelection; break; }
                }
            }
            EnsureSelectionIsValid();

            if (oldSelection != selection)
            {
                menuNodes[oldSelection].Normal();
                menuNodes[selection].Highlight();
            }
        }
    }
}

