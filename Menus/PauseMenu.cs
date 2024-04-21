using Blastula.Input;
using Blastula.Sounds;
using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Blastula.Menus
{
    /// <summary>
    /// Handles the main pause menu. Contains functionality for pause menu options.
    /// </summary>
    public partial class PauseMenu : VerticalListMenu
    {
        /// <summary>
        /// The confirmation selection of Yes/No
        /// </summary>
        [Export] public BaseMenu confirmMenu;
        [Export] public Vector2 confirmRetryMenuPosition = Vector2.Zero;
        [Export] public Vector2 confirmQuitMenuPosition = Vector2.Zero;
        private enum ConfirmType { Retry, Quit }
        private ConfirmType confirmType;

        public override void Open()
        {
            base.Open();
            PlayCommonSFX("Menu/Pause");
        }

        /// <summary>
        /// Causes the pause menu to close and the game to continue.
        /// </summary>
        public void Resume()
        {
            Close();
        }

        public void OpenConfirmRetryMenu()
        {
            confirmType = ConfirmType.Retry;
            confirmMenu.Position = confirmRetryMenuPosition;
            confirmMenu.Open();
        }

        public void OpenConfirmQuitMenu()
        {
            confirmType = ConfirmType.Quit;
            confirmMenu.Position = confirmQuitMenuPosition;
            confirmMenu.Open();
        }

        public void CloseConfirmMenu()
        {
            confirmMenu.Close();
        }

        public void Confirm()
        {
            switch (confirmType)
            {
                case ConfirmType.Retry:
                    {
                        // TODO: something less stupid here
                    }
                    break;
                case ConfirmType.Quit:
                    {
                        // TODO: something less stupid here
                        GetTree().Quit();
                    }
                    break;
            }
            Close();
        }
    }
}

