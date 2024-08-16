using Blastula.Collision;
using Blastula.Graphics;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using Blastula.Sounds;
using System.Diagnostics;
using Blastula.LowLevel;
using Blastula.Coroutine;

namespace Blastula
{
    /// <summary>
    /// Base for all those entities that generally oppose and try to destroy the player, so naturally, the player has to destroy them first.
    /// </summary>
    /// <remarks>
    /// It's an IVariableContainer for movement schedule purposes.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/enemy.png")]
    public partial class Enemy : Node2D, IVariableContainer
    {
        public static HashSet<Enemy> all = new HashSet<Enemy>();

        public enum DefenseMode
        {
            /// <summary>
            /// Only the damage left over after subtracting the defense is counted.
            /// </summary>
            Absorb,
            /// <summary>
            /// The damage is multiplied by (1 - defense).
            /// </summary>
            Scale
        }

        [ExportGroup("Health & Defense")]
        [Export] public float health = 100;
        /// <summary>
        /// References a ParticleEffectPool that activates when damaged by a player's bullet.
        /// </summary>
        [Export] public string damageParticlePool = "EnemyHit";
        /// <summary>
        /// If the enemy is damaged and the health becomes below this cutoff, another sound effect will play.
        /// This lets the player know that their attacks have not been in vain, and it will be over soon.
        /// </summary>
        [Export] public float lowHealthCutoff = -1;
        /// <summary>
        /// Determines how "defense" is used.
        /// </summary>
        [Export] public DefenseMode defenseMode = DefenseMode.Scale;
        [Export] public float defense = 0;
        public bool deflectAllDamage { get; protected set; } = false;

        /// <summary>
        /// Schedule that is executed primarily for the purpose of moving this enemy around.
        /// </summary>
        [ExportGroup("Movement")]
        [Export] public BaseSchedule movementSchedule;

        private Vector2 startPosition;

        /// <summary>
        /// A crude form of enemy-to-player collision, 
        /// because BlastulaCollider only handles collisions with bullets.
        /// If the player is ever closer than this, try to kill them.
        /// </summary>
        [ExportGroup("Collision")]
        [Export] public float playerCollisionRadius = 24;

        /// <summary>
        /// Blastodiscs in this list will recieve variables such as "enemy_count" and "health_frac".
        /// Auto-generated from children recursively.
        /// </summary>
        [ExportGroup("Shooting")]
        private List<Blastodisc> varDiscs = new List<Blastodisc>();

        /// <summary>
        /// Runs on defeat of the enemy. For example, this can drop collectibles using Schedules.DropCollectible.
        /// </summary>
        /// <remarks>
        /// Be careful if the schedule is a child of this Blastodisc and waits, even one frame.
        /// A deleted schedule won't do anything.
        /// </remarks>
        [ExportGroup("Deletion")]
        [Export] public BaseSchedule defeatSchedule;
        /// <summary>
        /// References a ParticleEffectPool that activates at the enemy's position when it is destroyed on-screen.
        /// </summary>
        [Export] public string deletionParticlePool = "ExplodeMedium";
        /// <remarks>
        /// By default, an enemy contains a visibility notifier that deletes the enemy when it goes offscreen.
        /// This minimum lifespan was added in the possibility that even if the enemy goes off the screen for some time,
        /// we might want it to return.<br /><br />
        /// A negative value is interpreted as an infinite duration.
        /// </remarks>
        [Export] public float selfMinLifespan = -1;
        /// <summary>
        /// After this duration, the enemy will destroy itself no matter what.
        /// </summary>
        /// <remarks>
        /// A negative value is interpreted as an infinite duration.
        /// </remarks>
        [Export] public float selfMaxLifespan = -1;
        [Export] public Wait.TimeUnits lifespanUnits = Wait.TimeUnits.Frames;
        [ExportGroup("Collectibles")]
        /// <summary>
        /// The points obtained for dealing one unit of damage to this enemy.
        /// </summary>
        [Export] public double pointsOnDamage = 10;
        /// <summary>
        /// The points obtained for destroying this enemy.
        /// </summary>
        [Export] public double pointsOnDestroy = 20000;

        /// <summary>
        /// The time remaining (in the same units as selfMaxLifespan) until the enemy is destroyed no matter what.
        /// </summary>
        public float lifespanLeft { get; protected set; } = float.PositiveInfinity;
        /// <summary>
        /// The amount of health the enemy spawned in with.
        /// </summary>
        public float maxHealth { get; protected set; }
        public bool defeated { get; protected set; } = false;
        public bool onScreen { get; private set; } = false;

        private EnemyFormation formation;
        private System.Collections.Generic.Dictionary<string, EnemyMover> myMovers = new System.Collections.Generic.Dictionary<string, EnemyMover>();

        /// <summary>
        /// Implemented for IVariableContainer; holds local variables.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, Variant> customData { get; set; } = new System.Collections.Generic.Dictionary<string, Variant>();
        /// <summary>
        /// Implemented for IVariableContainer; holds special variable names.
        /// </summary>
        public HashSet<string> specialNames { get; set; } = new HashSet<string>()
        {
            "pos", "dpos", "start_pos", "enemy_count", "health_frac", "on_screen"
        };
        /// <summary>
        /// Implemented for IVariableContainer; solves special variable names.
        /// </summary>
        public Variant GetSpecial(string varName)
        {
            switch (varName)
            {
                case "pos": return GlobalPosition;
                case "dpos": return GlobalPosition - startPosition;
                case "start_pos": return startPosition;
                case "enemy_count": return (formation == null) ? 1 : formation.enemyCount;
                case "health_frac": return (maxHealth == 0) ? 0 : (health / maxHealth);
                case "on_screen": return onScreen;
            }
            if (varName.StartsWith("vel"))
            {
                return AddOrGetEnemyMover(varName.Substring(3)).GetVelocity();
            }
            return default;
        }
        /// <summary>
        /// Response to a collider being hit by a bullet on the "PlayerShot" collision layer.
        /// </summary>
        public virtual unsafe void OnHit(BlastulaCollider collider, int bNodeIndex)
        {
            BNode* nodePtr = BNodeFunctions.masterQueue + bNodeIndex;
            int collisionLayer = nodePtr->collisionLayer;
            if (collisionLayer == CollisionManager.GetBulletLayerIDFromName("PlayerShot"))
            {
                float damageAmount = nodePtr->power;
                float usedBulletHealth = Mathf.Min(nodePtr->health, 1f);

                float oldBulletHealth = nodePtr->health;
                nodePtr->health = Mathf.Max(0, nodePtr->health - 1f);

                if (oldBulletHealth > 0 && nodePtr->health <= 0)
                {
                    BulletRenderer.SetRenderID(bNodeIndex, -1);
                    LaserRenderer.RemoveLaserEntry(bNodeIndex);
                }

                damageAmount *= usedBulletHealth;
                switch (defenseMode)
                {
                    case DefenseMode.Scale: damageAmount *= 1f - defense; break;
                    case DefenseMode.Absorb: damageAmount = Mathf.Max(0, damageAmount - defense); break;
                }
                if (deflectAllDamage) { damageAmount = 0f; }
                health = Mathf.Max(0, health - damageAmount);

                // Play damaged sound and effects
                if (usedBulletHealth > 0)
                {
                    if (damageAmount == 0)
                    {
                        CommonSFXManager.PlayByName("Enemy/Deflect", 1, 1, GlobalPosition, true);
                    }
                    else
                    {
                        Vector2 bulletWorldPos = BulletWorldTransforms.Get(bNodeIndex).Origin;
                        BulletWorldTransforms.Invalidate(bNodeIndex); // Just in case the caching affects the behavior. Hopefully not too many bullets hit the enemy at once.
                        ParticleEffectPool.PlayEffect(damageParticlePool, bulletWorldPos);
                        CommonSFXManager.PlayByName("Enemy/Damaged", 1, 1, GlobalPosition, true);
                        if (health <= lowHealthCutoff)
                        {
                            CommonSFXManager.PlayByName("Enemy/DamagedDeep", 1, 1, GlobalPosition, true);
                        }
                        Session.main.AddScore(pointsOnDamage * usedBulletHealth);
                    }
                }

                if (damageAmount > 0 && health == 0 && !defeated)
                {
                    Session.main.AddScore(pointsOnDestroy);
                    pointsOnDestroy = 0;
                    BecomeDefeated();
                }
            }
        }

        public virtual void BecomeDefeated()
        {
            if (defeated) { return; }
            defeated = true;
            if (defeatSchedule != null)
            {
                this.StartCoroutine(defeatSchedule.Execute(this));
            }
            if (onScreen)
            {
                CommonSFXManager.PlayByName("Enemy/Explode", 1, 1, GlobalPosition, true);
                ParticleEffectPool.PlayEffect(deletionParticlePool, GlobalPosition);
            }
            QueueFree();
        }

        /// <summary>
        /// Response to a visibility notifier.
        /// </summary>
        public void BecameVisibleFromNotifier()
        {
            onScreen = true;
        }

        /// <summary>
        /// Response to a visibility notifier.
        /// </summary>
        public void NoLongerVisibleFromNotifier()
        {
            onScreen = false;
            if (selfMaxLifespan - lifespanLeft >= selfMinLifespan)
            {
                QueueFree();
            }
            else
            {
                this.StartCoroutine(
                    CoroutineUtility.DelayedQueueFree(this, lifespanLeft, lifespanUnits)
                );
            }
        }

        public EnemyMover AddOrGetEnemyMover(string ID)
        {
            if (myMovers.ContainsKey(ID)) { return myMovers[ID]; }
            EnemyMover newMover = new EnemyMover();
            newMover.enemy = this;
            AddChild(newMover, false, InternalMode.Front);
            specialNames.Add("vel" + ID);
            return myMovers[ID] = newMover;
        }

        private void SetVarsInDiscs()
        {
            if (varDiscs != null && varDiscs.Count > 0)
            {
                foreach (Blastodisc bd in varDiscs)
                {
                    ((IVariableContainer)bd).SetVar("enemy_count", (formation == null) ? 1 : formation.enemyCount);
                    ((IVariableContainer)bd).SetVar("health_frac", (maxHealth == 0) ? 1.0 : (health / maxHealth));
                    ((IVariableContainer)bd).SetVar("on_screen", onScreen);
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
            base._Ready();
            all.Add(this);
            startPosition = GlobalPosition;
            maxHealth = health;
            lifespanLeft = selfMaxLifespan;

            if (movementSchedule != null)
            {
                this.StartCoroutine(movementSchedule.Execute(this));
            }

            formation = StageSector.GetCurrentEnemyFormation();
            if (formation != null)
            {
                formation.IncrementEnemy();
            }

            FindDiscs();
            SetVarsInDiscs();
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (formation != null)
            {
                formation.DecrementEnemy();
                formation = null;
            }
            if (Session.main != null && Session.main.inSession)
            {
                BecomeDefeated();
            }
            all.Remove(this);
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (Session.IsPaused()) { return; }

            // Collide with players.
            foreach (Player player in Player.playersByControl.Values)
            {
                if (player == null) { continue; }
                if ((player.GlobalPosition - GlobalPosition).Length() < playerCollisionRadius)
                {
                    this.StartCoroutine(player.Die());
                }
            }

            if (Debug.GameFlow.frozen) { return; }

            // Count down the life and destroy self when it hits zero.
            if (lifespanLeft > 0.0001)
            {
                lifespanLeft -= 
                    (float)((lifespanUnits == Wait.TimeUnits.Seconds) 
                    ? (Engine.TimeScale / Persistent.SIMULATED_FPS) 
                    : ((Engine.TimeScale > 0) ? 1 : 0));
                if (lifespanLeft <= 0.0001)
                {
                    lifespanLeft = 0;
                    BecomeDefeated();
                }
            }

            SetVarsInDiscs();
        }
    }
}

