using Blastula.Input;
using Blastula.Sounds;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Blastula.Menus
{
    /// <summary>
    /// Generates a grid of keys, allows selection between them by a crosshair, and broadcasts their selection.
    /// </summary>
    /// <remarks>
    /// It assumes several existences of menu buttons.
    /// </remarks>
    public partial class KeyboardMenu : BaseMenu
    {
        /// <summary>
        /// Sample's text within a first child Label will become the key's letter.
        /// The second child is an animator,
        /// </summary>
        [Export] Control sample;
        [Export] public Vector2 gridItemSize;
        [Export] public int columnCount;
        [Export] public string[] keyLetters;
        [Export] public int[] keyColSpans;
        [Export] public Vector2I crosshairGridPos = Vector2I.Zero;
        [Export] public string backSFXName = "Menu/Back";
        [Export] public string keyChangeSFXName = "Menu/TickRight";
        [Export] public string keyPressSFXName = "Menu/Type";
        [Export] public bool cancelable = true;

        [Signal] public delegate void OnTypeSymbolEventHandler(string symbol);

        private Dictionary<Vector2I, Control> gridPosToControl = new();
        private Dictionary<Vector2I, Vector2I> gridPosToHead = new();
        private Dictionary<Vector2I, string> gridPosToSymbol = new();
        private Dictionary<Vector2I, Vector2I> gridPosToNextRightPos = new();
        private Dictionary<Vector2I, Vector2I> gridPosToNextLeftPos = new();
        private Dictionary<int, int> rowMaxX = new();
        private Dictionary<int, int> columnMaxY = new();

        public override void _Ready()
        {
            base._Ready();
            int currX = 0;
            int currY = 0;
            int i = 0;
            foreach (string letter in keyLetters)
            {
                var colSpan = i < keyColSpans.Length ? keyColSpans[i] : 1;
                Control newNode = (Control)sample.Duplicate(7);
                AddChild(newNode);
                ((Label)newNode.GetChild(0)).Text = letter;
                newNode.Position = sample.Position + new Vector2(currX * gridItemSize.X, currY * gridItemSize.Y);
                newNode.Size = new Vector2(colSpan * gridItemSize.X, gridItemSize.Y);
                for (int j = 0; j < colSpan; j++)
                {
                    var v = new Vector2I(currX + j, currY);
                    gridPosToControl[v] = newNode;
                    gridPosToHead[v] = new Vector2I(currX, currY);
                    gridPosToSymbol[v] = letter;
                    gridPosToNextRightPos[v] = new Vector2I(currX + colSpan, currY);
                    gridPosToNextLeftPos[v] = new Vector2I(currX - 1, currY);
                    
                }
                rowMaxX[currY] = Mathf.Max(rowMaxX.ContainsKey(currY) ? rowMaxX[currY] : 0, currX);
                columnMaxY[currX] = Mathf.Max(columnMaxY.ContainsKey(currX) ? columnMaxY[currX] : 0, currY);
                currX += colSpan;
                if (currX >= columnCount) { currX = 0; currY++; }
                i++;
            }
            int yMax = rowMaxX.Keys.Max();
            rowMaxX[yMax + 1] = rowMaxX[yMax]; rowMaxX[-1] = rowMaxX[0];
            int xMax = columnMaxY.Keys.Max();
            columnMaxY[xMax + 1] = columnMaxY[xMax]; columnMaxY[-1] = columnMaxY[0];
            sample.Visible = false;
        }

        public Vector2 GetCurrentSelectPosition()
        {
            if (!gridPosToControl.ContainsKey(crosshairGridPos)) return Vector2.Zero; // This shouldn't happen
            return gridPosToControl[crosshairGridPos].GlobalPosition + gridPosToControl[crosshairGridPos].Size * 0.5f;
        }

        private bool RegisterPress(string buttonName)
            => InputManager.ButtonPressedThisFrame(buttonName)
            || (InputManager.GetButtonHeldFrames(buttonName) >= 16 && FrameCounter.realGameFrame % 8 == 0)
            || (InputManager.GetButtonHeldFrames(buttonName) >= 32 && FrameCounter.realGameFrame % 4 == 0);

        private bool RegisterPressSubmit(string buttonName)
            => InputManager.ButtonPressedThisFrame(buttonName)
            || (gridPosToSymbol[crosshairGridPos].Length > 1 && InputManager.GetButtonHeldFrames(buttonName) >= 16 && FrameCounter.realGameFrame % 8 == 0)
            || (gridPosToSymbol[crosshairGridPos].Length > 1 && InputManager.GetButtonHeldFrames(buttonName) >= 32 && FrameCounter.realGameFrame % 4 == 0);

        public override void _Process(double _)
        {
            if (!IsActive()) { return; }

            if (cancelable && InputManager.ButtonPressedThisFrame("Menu/Back"))
            {
                CommonSFXManager.PlayByName(backSFXName, 1, 0.5f);
                Close();
                return;
            }

            if (gridPosToSymbol.ContainsKey(crosshairGridPos) && RegisterPressSubmit("Menu/Select"))
            {
                string symbol = gridPosToSymbol[crosshairGridPos];
                CommonSFXManager.PlayByName(keyPressSFXName);
                EmitSignal(SignalName.OnTypeSymbol, symbol);
            }

            Vector2I dir = Vector2I.Zero;
            if (RegisterPress("Menu/Left")) dir += Vector2I.Left;
            if (RegisterPress("Menu/Right")) dir += Vector2I.Right;
            if (RegisterPress("Menu/Up")) dir += Vector2I.Up;
            if (RegisterPress("Menu/Down")) dir += Vector2I.Down;
            if (dir != Vector2I.Zero)
            {
                if (dir.X > 0) crosshairGridPos = gridPosToNextRightPos[crosshairGridPos];
                else if (dir.X < 0) crosshairGridPos = gridPosToNextLeftPos[crosshairGridPos];
                crosshairGridPos += new Vector2I(0, dir.Y);
                if (!gridPosToHead.ContainsKey(crosshairGridPos)) {
                    if (crosshairGridPos.X < 0) crosshairGridPos = new Vector2I(rowMaxX[crosshairGridPos.Y], crosshairGridPos.Y);
                    else if (crosshairGridPos.X > rowMaxX[crosshairGridPos.Y]) crosshairGridPos = new Vector2I(0, crosshairGridPos.Y);
                    if (crosshairGridPos.Y < 0) crosshairGridPos = new Vector2I(crosshairGridPos.X, columnMaxY[crosshairGridPos.X]);
                    else if (crosshairGridPos.Y > columnMaxY[crosshairGridPos.X]) crosshairGridPos = new Vector2I(crosshairGridPos.X, 0);
                }
                crosshairGridPos = gridPosToHead[crosshairGridPos];
                CommonSFXManager.PlayByName(keyChangeSFXName, 1, 0.5f);
            }
        }
    }
}

