using Blastula.Collision;
using Blastula.Graphics;
using Blastula.Input;
using Blastula.LowLevel;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;
using System.Collections.Generic;

namespace Blastula
{
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/player.png")]
    public partial class Player : Node2D
    {
        public enum Control
        {
            /// <summary>
            /// The only player in a one-player game.
            /// </summary>
            SinglePlayer,
            /// <summary>
            /// The left player in a two-player game.
            /// </summary>
            LeftPlayer,
            /// <summary>
            /// The right player in a two-player game.
            /// </summary>
            RightPlayer
        }
        /// <summary>
        /// Determines the player's role.
        /// </summary>
        [Export] public Control control = Control.SinglePlayer;
        [ExportGroup("Mobility")]
        /// <summary>
        /// Player's normal speed.
        /// </summary>
        [Export] public float normalSpeed = 500;
        /// <summary>
        /// Player's speed during the focus input.
        /// </summary>
        [Export] public float focusedSpeed = 200;
        /// <summary>
        /// Unit count which shrinks the boundary that constrains the player to the screen.
        /// </summary>
        [Export] public float boundaryShrink = 30;
        [ExportGroup("Health")]
        [Export] public BlastulaCollider hurtbox;
        [ExportGroup("Graze")]
        [Export] public BlastulaCollider grazebox;
        [Export] public float framesBetweenLaserGraze = 8;

        public bool debugInvincible = false;

        private string leftName = "Left";
        private string rightName = "Right";
        private string upName = "Up";
        private string downName = "Down";
        private string shootName = "Shoot";
        private string focusName = "Focus";
        private string bombName = "Bomb";
        private string specialName = "Special";
        private MainBoundary mainBoundary = null;

        /// <summary>
        /// Blastodiscs in this list will recieve important variables such as "shoot" and "focus".
        /// These variables are important to make player shots function correctly.
        /// </summary>
        public List<Blastodisc> varDiscs = new List<Blastodisc>();
        public static System.Collections.Generic.Dictionary<Control, Player> playersByControl = new System.Collections.Generic.Dictionary<Control, Player>();

        private void FindMainBoundary()
        {
            MainBoundary.MainType m = MainBoundary.MainType.Single;
            switch (control)
            {
                case Control.SinglePlayer:
                default:
                    break;
                case Control.LeftPlayer:
                    m = MainBoundary.MainType.Left;
                    break;
                case Control.RightPlayer:
                    m = MainBoundary.MainType.Right;
                    break;
            }
            if (MainBoundary.boundPerMode[(int)m] != null)
            {
                mainBoundary = MainBoundary.boundPerMode[(int)m];
            }
        }

        private void SetVarsInDiscs()
        {
            if (varDiscs != null && varDiscs.Count > 0)
            {
                foreach (Blastodisc bd in varDiscs)
                {
                    ((IVariableContainer)bd).SetVar("shoot", IsShooting());
                    ((IVariableContainer)bd).SetVar("focus", IsFocused());
                }
            }
        }

        private void FindDiscs(Node n = null)
        {
            if (n == null) { n = this; }
            foreach (Node child in n.GetChildren())
            {
                if (child is Blastodisc) { varDiscs.Add((Blastodisc)child); }
                FindDiscs(child);
            }
        }

        public override void _Ready()
        {
            switch (control)
            {
                case Control.SinglePlayer:
                default:
                    break;
                case Control.LeftPlayer:
                    leftName = "LP/" + leftName;
                    rightName = "LP/" + rightName;
                    upName = "LP/" + upName;
                    downName = "LP/" + downName;
                    shootName = "LP/" + shootName;
                    focusName = "LP/" + focusName;
                    bombName = "LP/" + bombName;
                    specialName = "LP/" + specialName;
                    break;
                case Control.RightPlayer:
                    leftName = "RP/" + leftName;
                    rightName = "RP/" + rightName;
                    upName = "RP/" + upName;
                    downName = "RP/" + downName;
                    shootName = "RP/" + shootName;
                    focusName = "RP/" + focusName;
                    bombName = "RP/" + bombName;
                    specialName = "RP/" + specialName;
                    break;
            }
            if (!playersByControl.ContainsKey(control)) { playersByControl[control] = this; }
            FindDiscs();
            SetVarsInDiscs();
        }

        public unsafe void OnHit(BlastulaCollider collider, int bNodeIndex)
        {
            // bNodeIndex is always >= 0, how could we get here otherwise???
            BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
            if (collider == hurtbox)
            {
                int collisionLayer = BNodeFunctions.masterQueue[bNodeIndex].collisionLayer;
                if (collisionLayer == CollisionManager.GetBulletLayerIDFromName("EnemyShot"))
                {
                    if (debugInvincible) { return; }
                    
                    if (LaserRenderer.IsBNodeHeadOfLaser(bNodeIndex) || LaserRenderer.IsBNodeTailOfLaser(bNodeIndex))
                    {
                        if (bNodePtr->bulletRenderID < 0) { return; }
                    }

                    if (bNodePtr->health > 1)
                    {
                        bNodePtr->health -= (float)Engine.TimeScale;
                    }
                    else
                    {
                        bNodePtr->health = 0;
                        BulletRenderer.SetRenderID(bNodeIndex, -1);
                        LaserRenderer.RemoveLaserEntry(bNodeIndex);
                    }
                }
            }
            else if (collider == grazebox && Engine.TimeScale > 0)
            {
                if (bNodePtr->graze >= 0) {
                    bool grazeGet = false;

                    if (bNodePtr->bulletRenderID >= 0)
                    {
                        if (bNodePtr->graze == 0) { grazeGet = true; }
                        if (bNodePtr->graze >= 0) { bNodePtr->graze += (float)Engine.TimeScale; }
                    }
                    else if (bNodePtr->laserRenderID >= 0)
                    {
                        bool newGrazeThisFrame = LaserRenderer.NewGrazeThisFrame(bNodeIndex, out int headBNodeIndex);
                        if (newGrazeThisFrame)
                        {
                            BNode* headBNodePtr = BNodeFunctions.masterQueue + headBNodeIndex;
                            float oldGraze = headBNodePtr->graze;
                            float newGraze = oldGraze + (float)Engine.TimeScale;
                            if (oldGraze == 0
                                || oldGraze + framesBetweenLaserGraze <= newGraze
                                || oldGraze % framesBetweenLaserGraze >= newGraze % framesBetweenLaserGraze)
                            {
                                grazeGet = true;
                            }
                            if (oldGraze < 0) { grazeGet = false; }
                            else { headBNodePtr->graze = newGraze; }
                        }
                    }
                    
                    if (grazeGet)
                    {
                        CommonSFXManager.PlayByName("Player/Graze", 1, 1f, GlobalPosition, true);
                        GrazeLines.ShowLine(GlobalPosition, BulletWorldTransforms.Get(bNodeIndex).Origin);
                        // Without this the bullet wiggles because we tried to calculate the position before the movement.
                        // We don't want that to happen, so force recalculate when the time is right.
                        BulletWorldTransforms.Invalidate(bNodeIndex);
                    }
                }
            }
        }

        public bool IsShooting() { return InputManager.ButtonIsDown(shootName); }
        public bool IsFocused() { return InputManager.ButtonIsDown(focusName); }

        private FrameCounter.Cache<Vector2> mvtDirCache = new FrameCounter.Cache<Vector2>();
        public Vector2 GetMovementDirection(bool normalized = false)
        {
            if (mvtDirCache.IsValid()) { return mvtDirCache.data; }
            Vector2 v = Vector2.Zero;
            if (InputManager.ButtonIsDown(leftName)) { v += Vector2.Left; }
            if (InputManager.ButtonIsDown(rightName)) { v += Vector2.Right; }
            if (InputManager.ButtonIsDown(upName)) { v += Vector2.Up; }
            if (InputManager.ButtonIsDown(downName)) { v += Vector2.Down; }
            Vector2 result = (normalized && v != Vector2.Zero) ? v.Normalized() : v;
            mvtDirCache.Update(result);
            return result;
        }

        public unsafe override void _Process(double delta)
        {
            if (Session.IsPaused()) { return; }
            float speed = IsFocused() ? focusedSpeed : normalSpeed;
            speed *= (float)Engine.TimeScale;
            speed /= Persistent.SIMULATED_FPS;
            GlobalPosition += speed * GetMovementDirection(true);
            if (mainBoundary == null) { FindMainBoundary(); }
            if (mainBoundary != null)
            {
                GlobalPosition = Boundary.Clamp(mainBoundary.lowLevelInfo, GlobalPosition, boundaryShrink);
            }
            SetVarsInDiscs();
        }
    }
}
