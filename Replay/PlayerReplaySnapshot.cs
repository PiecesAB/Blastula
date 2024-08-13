using Blastula.Collision;
using Blastula.Graphics;
using Blastula.Input;
using Blastula.LowLevel;
using Blastula.Operations;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula
{
    // I sure hope I don't forget to save state variables that change here.
    public partial class Player : IPersistForReplay
    {
        public Godot.Collections.Dictionary<string, string> CreateReplaySnapshot()
        {
            return new()
            {
                { PropertyName.lives, lives.ToString() },
                { PropertyName.shotPower, shotPower.ToString() },
                { PropertyName.shotPowerIndex, shotPowerIndex.ToString() },
                { PropertyName.bombs, bombs.ToString() },
                { PropertyName.itemGetHeight, itemGetHeight.ToString() },
                { "posX", Position.X.ToString() },
                { "posY", Position.Y.ToString() },
            };
        }

        public void LoadReplaySnapshot(Godot.Collections.Dictionary<string, string> snapshot)
        {
            try
            {
                lives = float.Parse(snapshot[PropertyName.lives]);
                shotPower = int.Parse(snapshot[PropertyName.shotPower]);
                shotPowerIndex = int.Parse(snapshot[PropertyName.shotPowerIndex]);
                bombs = float.Parse(snapshot[PropertyName.bombs]);
                itemGetHeight = float.Parse(snapshot[PropertyName.itemGetHeight]);
                Position = new Vector2(float.Parse(snapshot["posX"]), float.Parse(snapshot["posY"]));
            }
            catch
            {
                throw new System.Exception("Player: Unable to load data from replay file.");
            }
        }
    }
}

