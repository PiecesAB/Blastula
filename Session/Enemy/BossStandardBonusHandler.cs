using Godot;
using System.Collections;
using Blastula.Coroutine;
using System.Numerics;
using Blastula.Schedules;
using Blastula.VirtualVariables;

namespace Blastula
{
    public partial class BossStandardBonusHandler : Node
    {
        [Export] public bool enabled = true;

        public static BossStandardBonusHandler main;

        public bool IsEnabled() { return enabled; }

        private Callable mainPlayerBombConnection;
        private Callable mainPlayerStruckConnection;

        private StageSector bonusSector;
        private double baseBonus;
        private double bonusPerExtraSecond;
        private Player mainPlayer;
        private bool withinAttack = false;
        private BigInteger currentBonus = 0;
        private bool bonusFailed = false;

        public BigInteger GetCurrentBonus()
        {
            return currentBonus;
        }

        public void OnMainPlayerBomb()
        {
            bonusFailed = true;
        }

        public void OnMainPlayerStruck()
        {
            bonusFailed = true;
        }

        public IEnumerator Calculate()
        {
            if (withinAttack) 
            {
                GD.PushWarning(
                    "Tried to enter another bonus calculation while calculating another; not re-entering. " +
                    "This is likely a symptom of mishandling/nesting boss attacks."
                );
                yield break;
            }
            withinAttack = true;
            bonusFailed = false;

            mainPlayer.Connect(
                Player.SignalName.OnBombBegan,
                mainPlayerBombConnection = new Callable(this, MethodName.OnMainPlayerBomb)
            );

            mainPlayer.Connect(
                Player.SignalName.OnStruck,
                mainPlayerStruckConnection = new Callable(this, MethodName.OnMainPlayerStruck)
            );

            while (bonusSector.ShouldBeExecuting() && !bonusFailed)
            {
                currentBonus = (BigInteger)(baseBonus + Mathf.Round(bonusPerExtraSecond * StageSector.GetTimeRemaining()));
                currentBonus = (currentBonus / 10) * 10;
                yield return new WaitOneFrame();
            }

            if (bonusSector.HasBeenTimedOut())
            {
                bonusFailed = true;
            }

            if (bonusFailed)
            {
                currentBonus = 0;
            }
            else
            {
                Session.main.AddScore(currentBonus);
            }

            mainPlayer.Disconnect(Player.SignalName.OnBombBegan, mainPlayerBombConnection);
            mainPlayer.Disconnect(Player.SignalName.OnStruck, mainPlayerStruckConnection);

            withinAttack = false;
        }

        public void StartCalculation(double baseBonus, double bonusPerExtraSecond)
        {
            if (!enabled)
            {
                GD.Print("Standard boss attack bonus is not enabled.");
                return;
            }
            if (baseBonus == 0 && bonusPerExtraSecond == 0)
            {
                return;
            }
            if (!Player.playersByControl.ContainsKey(Player.Role.SinglePlayer))
            {
                GD.PushError("This action currently makes no sense; I'm looking for a SinglePlayer to detect bonus condition.");
                return;
            }
            mainPlayer = Player.playersByControl[Player.Role.SinglePlayer];
            this.baseBonus = baseBonus;
            this.bonusPerExtraSecond = bonusPerExtraSecond;
            bonusSector = StageSector.GetCurrentSector();
            this.StartCoroutine(Calculate());
        }

        public override void _Ready()
        {
            base._Ready();
            ProcessPriority = Persistent.Priorities.BONUS_TICK;
            main = this;
        }
    }
}
