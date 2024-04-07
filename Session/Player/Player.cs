using Blastula.Collision;
using Blastula.Graphics;
using Blastula.Input;
using Blastula.LowLevel;
using Blastula.Operations;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;

namespace Blastula
{
    /// <summary>
    /// Do I really need to explain what a player is? Fine.
    /// This is an entity driven by the user's input, and the user is supposed to help them survive.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/player.png")]
    public partial class Player : Node2D
    {
        public enum Role
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
        [Export] public Role role = Role.SinglePlayer;
        /// <summary>
        /// Player's normal speed.
        /// </summary>
        [ExportGroup("Mobility")]
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
        /// <summary>
        /// Amount of lives that the player currently has. 
        /// In a single-player context, if 0 lives are left, the player will lose next time they are hit.
        /// </summary>
        [Export] public float lives = 2;
        [ExportGroup("Shot Power")]
        [Export] public int shotPower = 100;
        /// <summary>
        /// The shotPower is bounded between X and Y.
        /// </summary>
        [Export] public Vector2I shotPowerRange = new Vector2I(100, 400);
        /// <summary>
        /// Amount of bombs the player currently has.
        /// </summary>
        [ExportGroup("Bomb")]
        [Export] public float bombs = 3;
        /// <summary>
        /// When the player resurrects and they have extra lives, the bombs will be refilled to this amount.
        /// </summary>
        [Export] public float bombRefillOnDeath = 3;
        /// <summary>
        /// The number of frames early the player can press the Bomb input before they are able to use it.
        /// </summary>
        [Export] public int bombStartBufferFrames = 6;
        /// <summary>
        /// The number of leniency frames where the player can bomb after getting hit, cheating death.
        /// </summary>
        [Export] public int deathbombFrames = 8;
        [ExportGroup("Graze")]
        [Export] public BlastulaCollider grazebox;
        [Export] public float framesBetweenLaserGraze = 8;
        [ExportGroup("Collectibles")]
        [Export] public BlastulaCollider attractbox;
        /// <summary>
        /// Above this Y position, the player will attract all collectibles by making the attractbox extremely large.
        /// Point items will also be worth their full value.
        /// </summary>
        [Export] public float itemGetHeight = -150;
        /// <summary>
        /// The fraction of point item value which is lost when items are collected immediately below the item get height.
        /// </summary
        /// <example>
        /// If this is 0.3, then 30% of value is lost immediately below the item get height.
        /// </example>
        [Export] public float pointItemValueCut = 0.3f;
        /// <summary>
        /// The fraction of point item value which is lost exponentially, for every 100 units below the item get height.
        /// </summary>
        /// <example>
        /// If pointItemValueCut is 0.3, and this is 0.1, then if the player collects a point item 200 units below the item get height,
        /// It will only be worth 70% * 90% * 90% = 56.7% of full value.
        /// </example>
        [Export] public float pointItemValueRolloff = 0.1f;
        private Vector2 attractboxOriginalSize;
        private string COLLECTIBLE_ATTRACT_SEQUENCE_NAME = "CollectibleAttractPhase";

        public enum LifeState
        {
            Normal, 
            Dying, 
            Recovering, 
            Invulnerable
        }
        public LifeState lifeState = LifeState.Normal;
        /// <summary>
        /// The number of invulnerability frames remaining now; relevant in the Recovering/Invulnerable life state.
        /// </summary>
        public int invulnerabilityFrames = 0;
        public bool debugInvulnerable = false;

        private string leftName = "Left";
        private string rightName = "Right";
        private string upName = "Up";
        private string downName = "Down";
        private string shootName = "Shoot";
        private string focusName = "Focus";
        private string bombName = "Bomb";
        private string specialName = "Special";

        private MainBoundary mainBoundary = null;
        private int grazeGetThisFrame = 0;
        private FrameCounter.Buffer bombStartBuffer = new FrameCounter.Buffer(0);
        private FrameCounter.Buffer deathbombBuffer = new FrameCounter.Buffer(0);

        /// <summary>
        /// Blastodiscs in this list will recieve important variables such as "shoot" and "focus".
        /// These variables are important to make player shots function correctly.
        /// </summary>
        public List<Blastodisc> varDiscs = new List<Blastodisc>();
        public static System.Collections.Generic.Dictionary<Role, Player> playersByControl = new System.Collections.Generic.Dictionary<Role, Player>();

        private void FindMainBoundary()
        {
            MainBoundary.MainType m = MainBoundary.MainType.Single;
            switch (role)
            {
                case Role.SinglePlayer:
                default:
                    break;
                case Role.LeftPlayer:
                    m = MainBoundary.MainType.Left;
                    break;
                case Role.RightPlayer:
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
                    ((IVariableContainer)bd).SetVar("power", shotPower);
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

        /// <summary>
        /// Returns true when above the item get line.
        /// </summary>
        private bool IsInItemGetMode()
        {
            return GlobalPosition.Y <= itemGetHeight;
        }

        public override void _Ready()
        {
            switch (role)
            {
                case Role.SinglePlayer:
                default:
                    break;
                case Role.LeftPlayer:
                    leftName = "LP/" + leftName;
                    rightName = "LP/" + rightName;
                    upName = "LP/" + upName;
                    downName = "LP/" + downName;
                    shootName = "LP/" + shootName;
                    focusName = "LP/" + focusName;
                    bombName = "LP/" + bombName;
                    specialName = "LP/" + specialName;
                    break;
                case Role.RightPlayer:
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
            if (!playersByControl.ContainsKey(role)) { playersByControl[role] = this; }
            else { GD.PushWarning("Two or more players exist with the same role. This is not expected."); }
            FindDiscs();
            SetVarsInDiscs();
            attractboxOriginalSize = attractbox.size;
        }

        private unsafe void OnHitHurtIntent(BlastulaCollider collider, int bNodeIndex)
        {
            BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;

            // Because of the way lasers are rendered, the head and tail collisions could be unfair
            // (possible to occur outside the graphic)
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
                if (bNodePtr->laserRenderID < 0)
                {
                    bNodePtr->transform = BulletWorldTransforms.Get(bNodeIndex);
                    bNodePtr->worldTransformMode = true;
                    PostExecute.ScheduleDeletion(bNodeIndex, true);
                }
                else
                {
                    BulletRenderer.SetRenderID(bNodeIndex, -1);
                    LaserRenderer.RemoveLaserEntry(bNodeIndex);
                }
            }

            if (debugInvulnerable || lifeState != LifeState.Normal) { return; }
            // At this point the hurting actually occurs
            lifeState = LifeState.Dying;
            deathbombBuffer.Replenish((ulong)deathbombFrames);
        }

        private unsafe void OnHitGrazeIntent(BlastulaCollider collider, int bNodeIndex)
        {
            BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
            if (bNodePtr->graze >= 0)
            {
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
                    ++grazeGetThisFrame;
                    // TODO: implement and increment graze counter
                    if (grazeGetThisFrame < 5)
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

        private unsafe void OnHitCollectIntent(BlastulaCollider collider, int bNodeIndex)
        {
            BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
            bool itemGetLineActivated = (bNodePtr->phase == 3);
            Vector2 bulletWorldPos = BulletWorldTransforms.Get(bNodeIndex).Origin;

            if (CollectibleManager.IsPointItem(bNodeIndex))
            {
                double fullValue = CollectibleManager.GetPointItemFullValue(bNodeIndex);

                if (itemGetLineActivated)
                {
                    var actualAdded = Session.main.AddScore(fullValue);
                    ScorePopupPool.Play(bulletWorldPos, actualAdded, Colors.Cyan);
                }
                else
                {
                    double cutValue = fullValue
                        * (1.0 - pointItemValueCut)
                        * System.Math.Pow(1.0 - pointItemValueRolloff, (GlobalPosition.Y - itemGetHeight) / 100.0);
                    var actualAdded = Session.main.AddScore(cutValue);
                    ScorePopupPool.Play(bulletWorldPos, actualAdded, Colors.White);
                }

                if (StageManager.main != null) { StageManager.main.AddPointItem(1); }
                if (Session.main != null) { Session.main.AddPointItem(1); }
            }
            else if (CollectibleManager.IsPowerItem(bNodeIndex))
            {
                shotPower += Mathf.RoundToInt(bNodePtr->power);
                if (shotPower > shotPowerRange.Y) { shotPower = shotPowerRange.Y; }

                ScorePopupPool.Play(bulletWorldPos, 10, itemGetLineActivated ? Colors.Cyan : Colors.White);
            }

            PostExecute.ScheduleDeletion(bNodeIndex, false);
            CommonSFXManager.PlayByName("Player/Vacuum", 1, 1f, GlobalPosition, true);
        }

        private unsafe void OnHitAttractCollectIntent(BlastulaCollider collider, int bNodeIndex)
        {
            BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
            short phase = bNodePtr->phase;
            if (phase == 1 && Sequence.referencesByID.ContainsKey(COLLECTIBLE_ATTRACT_SEQUENCE_NAME))
            {
                PostExecute.ScheduleOperation(
                    bNodeIndex,
                    Sequence.referencesByID[COLLECTIBLE_ATTRACT_SEQUENCE_NAME]?.GetOperationID() ?? -1
                );

                if (IsInItemGetMode())
                {
                    // Tint the items so we know which ones have the full value later on
                    if (bNodePtr->multimeshExtras == null)
                    {
                        bNodePtr->multimeshExtras = SetMultimeshExtraData.NewPointer();
                    }
                    bNodePtr->multimeshExtras->color = 1.2f * Colors.LightBlue;
                    bNodePtr->phase++;
                }
            }
        }

        /// <summary>
        /// Response to a collider being hit by a bullet on the "EnemyShot" collision layer.
        /// Also handles grazing, naturally.
        /// </summary>
        public unsafe void OnHit(BlastulaCollider collider, int bNodeIndex)
        {
            if (lifeState == LifeState.Dying) { return; }
            // bNodeIndex is always >= 0, how could we get here otherwise???
            BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
            int collisionLayer = bNodePtr->collisionLayer;
            int enemyShotBulletLayer = CollisionManager.GetBulletLayerIDFromName("EnemyShot");
            int collectibleBulletLayer = CollisionManager.GetBulletLayerIDFromName("Collectible");
            if (Engine.TimeScale > 0 && collisionLayer == enemyShotBulletLayer)
            {
                if (collider == hurtbox) { OnHitHurtIntent(collider, bNodeIndex); }
                else if (collider == grazebox) { OnHitGrazeIntent(collider, bNodeIndex); }
            }
            else if (Engine.TimeScale > 0 && collisionLayer == collectibleBulletLayer)
            {
                if (collider == hurtbox) { OnHitCollectIntent(collider, bNodeIndex); }
                else if (collider == attractbox) { OnHitAttractCollectIntent(collider, bNodeIndex); }
            }
        }

        public bool IsShooting() 
        { 
            if (lifeState == LifeState.Dying) { return false; }
            return InputManager.ButtonIsDown(shootName); 
        }

        public bool IsFocused()
        { 
            return InputManager.ButtonIsDown(focusName);
        }

        private FrameCounter.Cache<Vector2> mvtDirCache = new FrameCounter.Cache<Vector2>();

        /// <summary>
        /// Calculates this frame's movement direction vector, which has length 1 when moving, and is the zero vector when not moving.
        /// </summary>
        public Vector2 GetMovementDirection()
        {
            if (mvtDirCache.IsValid()) { return mvtDirCache.data; }
            Vector2 v = Vector2.Zero;
            if (InputManager.ButtonIsDown(leftName)) { v += Vector2.Left; }
            if (InputManager.ButtonIsDown(rightName)) { v += Vector2.Right; }
            if (InputManager.ButtonIsDown(upName)) { v += Vector2.Up; }
            if (InputManager.ButtonIsDown(downName)) { v += Vector2.Down; }
            Vector2 result = (v != Vector2.Zero) ? v.Normalized() : v;
            mvtDirCache.Update(result);
            return result;
        }

        private unsafe void PerformMovement()
        {
            if (lifeState == LifeState.Dying) { return; }
            float speed = IsFocused() ? focusedSpeed : normalSpeed;
            speed *= (float)Engine.TimeScale;
            speed /= Persistent.SIMULATED_FPS;
            GlobalPosition += speed * GetMovementDirection();
            if (mainBoundary == null) { FindMainBoundary(); }
            if (mainBoundary != null)
            {
                GlobalPosition = Boundary.Clamp(mainBoundary.lowLevelInfo, GlobalPosition, boundaryShrink);
            }
        }

        private void CountGrazeGetThisFrame()
        {
            if (StageManager.main != null) { StageManager.main.AddGraze(grazeGetThisFrame); }
            if (Session.main != null) { Session.main.AddGraze(grazeGetThisFrame); }
            grazeGetThisFrame = 0;
        }

        public override void _Process(double delta)
        {
            if (Session.IsPaused()) { return; }
            PerformMovement();
            SetVarsInDiscs();
            CountGrazeGetThisFrame();
            if (IsInItemGetMode()) { attractbox.size = new Vector2(2000, 2000); }
            else { attractbox.size = attractboxOriginalSize; }
        }
    }
}

