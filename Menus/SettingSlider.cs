using Blastula.Input;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Menus
{
    /// <summary>
    /// A list node such that when Menu/Left or Menu/Right are pressed, the setting is changed.
    /// </summary>
    public partial class SettingSlider : ListNode
    {
        /// <summary>
        /// Plays "Left" and "Right" animations when the selection is changed.
        /// </summary>
        [Export] AnimationPlayer selectorAnimPlayer;
        [Export] public Label selectorText;
        [Export] public TextureRect selectorLeftArrow;
        [Export] public TextureRect selectorRightArrow;
        [Export] public string settingName = "unknown";
        [Export] public string[] settingValues = new string[] { "off", "on" };
        public int valueIndex { get; private set; } = 0;

        private void LoadIndex()
        {
            string targetValue = SettingsLoader.Get(settingName);
            for (int i = 0; i < settingValues.Length; ++i)
            {
                if (settingValues[i] == targetValue)
                {
                    valueIndex = i; break;
                }
            }
        }

        private void UpdateGraphic(int direction = 0)
        {
            selectorText.Text = settingValues[valueIndex].ToString();
            switch (direction)
            {
                case -1: selectorAnimPlayer.Stop(); selectorAnimPlayer.Play("Left"); break;
                case 1: selectorAnimPlayer.Stop(); selectorAnimPlayer.Play("Right"); break;
            }
        }

        public override void _Ready()
        {
            base._Ready();
            LoadIndex();
            UpdateGraphic();
        }

        private int holdKeyCooldown = 0;

        public override void _Process(double delta)
        {
            base._Process(delta);

            selectorAnimPlayer.Pause();
            selectorAnimPlayer.Advance(1.0 / Persistent.SIMULATED_FPS);

            if (!currentlyHighlighted) { return; }

            selectorLeftArrow.Modulate = (valueIndex == 0) ? Colors.Black : Colors.White;
            selectorRightArrow.Modulate = (valueIndex == settingValues.Length - 1) ? Colors.Black : Colors.White;

            if (InputManager.ButtonPressedThisFrame("Menu/Left") 
             || InputManager.ButtonPressedThisFrame("Menu/Right"))
            {
                holdKeyCooldown = 0;
            }
            bool leftPressed
                 = InputManager.ButtonPressedThisFrame("Menu/Left")
                || InputManager.ButtonIsHeldLongEnough("Menu/Left", 24);
            leftPressed &= !InputManager.ButtonIsDown("Menu/Right");
            leftPressed &= holdKeyCooldown == 0;
            bool rightPressed
                 = InputManager.ButtonPressedThisFrame("Menu/Right")
                || InputManager.ButtonIsHeldLongEnough("Menu/Right", 24);
            rightPressed &= !InputManager.ButtonIsDown("Menu/Left");
            rightPressed &= holdKeyCooldown == 0;
            
            if (leftPressed)
            {
                valueIndex -= 1;
                if (valueIndex < 0) { valueIndex = 0; }
                holdKeyCooldown = 8;
                UpdateGraphic(-1);
            }
            else if (rightPressed)
            {
                valueIndex += 1;
                if (valueIndex >= settingValues.Length) { valueIndex = settingValues.Length - 1; }
                holdKeyCooldown = 8;
                UpdateGraphic(1);
            }
            
            if (holdKeyCooldown > 0) { --holdKeyCooldown; }

        }
    }
}

