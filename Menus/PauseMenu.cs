using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Menus
{
    /// <summary>
    /// Handles the main pause menu. Contains functionality for pause menu options.
    /// </summary>
    public partial class PauseMenu : ListMenu
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

        private void Retry()
        {
            Close();
            if (StageManager.main != null)
            {
                _ = StageManager.main.RetrySinglePlayerSession();
            }
        }

        public void OpenConfirmRetryMenu()
        {
            if (PauseMenuManager.main != null && PauseMenuManager.main.mode == PauseMenuManager.Mode.GameOverNoContinue)
            {
                // The player probably wants to retry because they can't continue/resume, so no need to confirm.
                Retry();
                return;
            }
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
                case ConfirmType.Retry: Retry(); break;
                case ConfirmType.Quit:
                    {
                        Close();
                        if (StageManager.main != null)
                        {
                            StageManager.main.ForceEndSinglePlayerSession();
                            Loader.LoadExternal(this, Persistent.TITLE_MENU_PATH);
                        }
                    }
                    break;
            }
        }

        // Insert credit
        public void Continue()
        {
            if (Session.main != null && Session.main.canContinue)
            {
                Session.main.SinglePlayerContinue();
                Close();
            }
        }
    }
}

