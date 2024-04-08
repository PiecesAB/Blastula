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
    /// Sort of a bonus class. 
    /// This attaches to the sprite of a player or enemy that animates based on various player states.
    /// </summary>
    public partial class PlayerSpriteAnimator : Sprite2D
    {
        public enum State
        {
            Neutral, LeftEntry, LeftLoop, RightEntry, RightLoop, Dying
        }

        public State state = State.Neutral;

        [Export] public Vector2I neutralSpriteRange = new Vector2I(0, 7);
        [Export] public float neutralFramerate = 12;

        [Export] public Vector2I leftEntrySpriteRange = new Vector2I(8, 11);
        [Export] public Vector2I rightEntrySpriteRange = new Vector2I(16, 19);
        [Export] public float directionEntryFramerate = 30;

        [Export] public Vector2I leftLoopSpriteRange = new Vector2I(12, 15);
        [Export] public Vector2I rightLoopSpriteRange = new Vector2I(20, 23);
        [Export] public float directionLoopFramerate = 12;

        [Export] public Vector2I dyingSpriteRange = new Vector2I(0, 0);
        [Export] public float dyingFramerate = 1;

        private float movementCharge = 0f;
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
            float progress = Mathf.Abs(movementCharge);
            return Mathf.Min(range.Y, range.X + Mathf.FloorToInt(progress * GetSpriteLength(range)));
        }

        private State GetTargetState()
        {
            // Dying?
            if (playerParent.lifeState == Player.LifeState.Dying) 
            { 
                return State.Dying; 
            }

            float moveStep = 0.25f * directionEntryFramerate / Persistent.SIMULATED_FPS;
            // Moving left or left-diagonal?
            if (playerParent.GetMovementDirection().X <= -0.5f)
            {
                movementCharge = Mathf.MoveToward(movementCharge, -1f, 0.1f);
            }
            // Moving right or right-diagonal?
            else if (playerParent.GetMovementDirection().X >= 0.5f)
            {
                movementCharge = Mathf.MoveToward(movementCharge, 1f, 0.1f);
            }
            // Not moving in either direction?
            else
            {
                movementCharge = Mathf.MoveToward(movementCharge, 0f, 0.1f);
            }

            if (movementCharge == -1f) { return State.LeftLoop; }
            if (movementCharge == 1f) { return State.RightLoop; }
            if (movementCharge < 0f) { return State.LeftEntry; }
            if (movementCharge > 0f) { return State.RightEntry; }
            return State.Neutral;
        }

        private void SetSprite()
        {
            int frameNumber = 0;
            switch (state)
            {
                case State.Neutral: default:
                    frameNumber = GetLoopFrameNumber(neutralSpriteRange, neutralFramerate);
                    break;
                case State.LeftEntry:
                    frameNumber = GetMoveEntryFrameNumber(leftEntrySpriteRange);
                    break;
                case State.LeftLoop:
                    frameNumber = GetLoopFrameNumber(leftLoopSpriteRange, directionLoopFramerate);
                    break;
                case State.RightEntry:
                    frameNumber = GetMoveEntryFrameNumber(rightEntrySpriteRange);
                    break;
                case State.RightLoop:
                    frameNumber = GetLoopFrameNumber(rightLoopSpriteRange, directionLoopFramerate);
                    break;
                case State.Dying:
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
            State targetState = GetTargetState();
            if (targetState != state)
            {
                internalTime = 0.0;
            }
            state = targetState;
            SetSprite();
        }
    }
}

