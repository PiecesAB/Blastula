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

namespace Blastula.Graphics
{
    /// <summary>
    /// This script should belong to the sprite of a player. 
    /// It animates the sprite based on various player states.
    /// </summary>
    public partial class PlayerSpriteAnimator : Sprite2D 
    {
        public enum AnimationStyle
        {
            /// <summary>
            /// Use horizontal and vertical directions for sprite animation. 
            /// The horizontal directions have priority for diagonal movement.
            /// </summary>
            FourDirections,
            /// <summary>
            /// Use only horizontal direction for sprite animation. Vertical movement uses the neutral animation.
            /// </summary>
            TwoDirections
        }

        public enum AnimationState
        {
            Neutral, LeftEntry, LeftLoop, RightEntry, RightLoop, UpEntry, UpLoop, DownEntry, DownLoop, Dying
        }

        public enum ColorScaleState
        {
            Normal, Dying, Recovering
        }

        /// <summary>
        /// Determines the range of sprites to use.
        /// </summary>
        [Export] public AnimationStyle animationStyle = AnimationStyle.FourDirections;
        public AnimationState animationState = AnimationState.Neutral;
        public ColorScaleState colorScaleState = ColorScaleState.Normal;

        
        [ExportGroup("Sprite Ranges")]
        [ExportSubgroup("Entry")]
        [Export] public Vector2I leftEntrySpriteRange = new Vector2I(8, 11);
        [Export] public Vector2I rightEntrySpriteRange = new Vector2I(16, 19);
        [Export] public Vector2I upEntrySpriteRange = new Vector2I(24, 27);
        [Export] public Vector2I downEntrySpriteRange = new Vector2I(32, 35);
        [ExportSubgroup("Loop")]
        [Export] public Vector2I neutralSpriteRange = new Vector2I(0, 7);
        [Export] public Vector2I leftLoopSpriteRange = new Vector2I(12, 15);
        [Export] public Vector2I rightLoopSpriteRange = new Vector2I(20, 23);
        [Export] public Vector2I upLoopSpriteRange = new Vector2I(28, 31);
        [Export] public Vector2I downLoopSpriteRange = new Vector2I(36, 39);
        [Export] public Vector2I dyingSpriteRange = new Vector2I(0, 0);
        [ExportGroup("Framerates")]
        [Export] public float neutralFramerate = 12;
        [Export] public float directionEntryFramerate = 30;
        [Export] public float directionLoopFramerate = 12;
        [Export] public float dyingFramerate = 1;

        private Vector2 movementCharge = Vector2.Zero;
        private double internalTime = 0.0;
        private Player playerParent = null;

        public override void _Ready()
        {
            base._Ready();
            if (GetParent() is Player)
            {
                playerParent = (Player)GetParent();
            }
        }

        private int GetSpriteLength(Vector2I range)
        {
            return range.Y - range.X + 1;
        }

        private int GetLoopFrameNumber(Vector2I range, float frameRate)
        {
            return range.X + (int)((long)Math.Floor(internalTime * frameRate) % GetSpriteLength(range));
        }

        private int GetMoveEntryFrameNumber(Vector2I range)
        {
            float progress = movementCharge.Length();
            return Mathf.Min(range.Y, range.X + Mathf.FloorToInt(progress * GetSpriteLength(range)));
        }

        private void ChargeMoveToward(Vector2 target)
        {
            if (animationStyle == AnimationStyle.TwoDirections)
            {
                target = new Vector2(target.X, 0);
            }

            float moveStep = 0.25f * (float)Engine.TimeScale * directionEntryFramerate / Persistent.SIMULATED_FPS;
            if (target.Normalized() == movementCharge.Normalized() || movementCharge.Length() == 0)
            {
                movementCharge = movementCharge.MoveToward(target, moveStep);
            }
            else
            {
                movementCharge = movementCharge.MoveToward(Vector2.Zero, moveStep);
            }
        }

        private AnimationState GetTargetAnimationState()
        {
            // Dying?
            if (playerParent.lifeState == Player.LifeState.Dying) 
            { 
                return AnimationState.Dying; 
            }

            Vector2 mvt = playerParent.GetMovementDirection();
            
            // Moving left or left-diagonal?
            if (mvt.X <= -0.5f)
            {
                ChargeMoveToward(Vector2.Left);
            }
            // Moving right or right-diagonal?
            else if (mvt.X >= 0.5f)
            {
                ChargeMoveToward(Vector2.Right);
            }
            // Moving up?
            else if (mvt.Y <= -0.5f)
            {
                ChargeMoveToward(Vector2.Up);
            }
            // Moving down?
            else if (mvt.Y >= 0.5f)
            {
                ChargeMoveToward(Vector2.Down);
            }
            // Not moving?
            else
            {
                ChargeMoveToward(Vector2.Zero);
            }

            if (movementCharge == Vector2.Left) { return AnimationState.LeftLoop; }
            if (movementCharge == Vector2.Right) { return AnimationState.RightLoop; }
            if (movementCharge == Vector2.Up) { return AnimationState.UpLoop; }
            if (movementCharge == Vector2.Down) { return AnimationState.DownLoop; }
            Vector2 normalized = movementCharge.Normalized();
            if (normalized == Vector2.Left) { return AnimationState.LeftEntry; }
            if (normalized == Vector2.Right) { return AnimationState.RightEntry; }
            if (normalized == Vector2.Up) { return AnimationState.UpEntry; }
            if (normalized == Vector2.Down) { return AnimationState.DownEntry; }
            return AnimationState.Neutral;
        }

        private void SetSprite()
        {
            int frameNumber = 0;
            switch (animationState)
            {
                case AnimationState.Neutral: default:
                    frameNumber = GetLoopFrameNumber(neutralSpriteRange, neutralFramerate);
                    break;
                case AnimationState.LeftEntry:
                    frameNumber = GetMoveEntryFrameNumber(leftEntrySpriteRange);
                    break;
                case AnimationState.LeftLoop:
                    frameNumber = GetLoopFrameNumber(leftLoopSpriteRange, directionLoopFramerate);
                    break;
                case AnimationState.RightEntry:
                    frameNumber = GetMoveEntryFrameNumber(rightEntrySpriteRange);
                    break;
                case AnimationState.RightLoop:
                    frameNumber = GetLoopFrameNumber(rightLoopSpriteRange, directionLoopFramerate);
                    break;
                case AnimationState.UpEntry:
                    frameNumber = GetMoveEntryFrameNumber(upEntrySpriteRange);
                    break;
                case AnimationState.UpLoop:
                    frameNumber = GetLoopFrameNumber(upLoopSpriteRange, directionLoopFramerate);
                    break;
                case AnimationState.DownEntry:
                    frameNumber = GetMoveEntryFrameNumber(downEntrySpriteRange);
                    break;
                case AnimationState.DownLoop:
                    frameNumber = GetLoopFrameNumber(downLoopSpriteRange, directionLoopFramerate);
                    break;
                case AnimationState.Dying:
                    frameNumber = GetLoopFrameNumber(dyingSpriteRange, dyingFramerate);
                    break;
            }
            Frame = frameNumber;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (Session.main == null || playerParent == null || Session.main.paused) { return; }
            internalTime += Engine.TimeScale / Persistent.SIMULATED_FPS;
            AnimationState targetState = GetTargetAnimationState();
            if (targetState != animationState)
            {
                internalTime = 0.0;
            }
            animationState = targetState;
            SetSprite();
        }
    }
}

