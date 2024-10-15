using Godot;
using System.Collections;
using Blastula.VirtualVariables;
using System.Numerics;

namespace Blastula.Schedules;

/// <remarks>
/// Set the player (assuming a single-player game) to get extra lives when they reach score milestones.
/// </remarks>
[GlobalClass]
[Icon(Persistent.NODE_ICON_PATH + "/extend.png")]
public partial class SetScoreExtends : BaseSchedule
{
	[Export] public double[] extendScores = new double[] {
	    1e6, 2e6, 3e6, 5e6, 8e6, 13e6, 21e6
	};

    private static SetScoreExtends current = null;
    private int currentIndex = 0;
    private BigInteger? nextScoreStored;

    public static BigInteger? GetNextExtendScore()
    {
        if (current == null || current.currentIndex >= current.extendScores.Length) { return null; }
        return current.nextScoreStored.HasValue 
            ? current.nextScoreStored 
            : current.nextScoreStored = (BigInteger)current.extendScores[current.currentIndex];
    }

    public static void Check(BigInteger oldScore, BigInteger newScore)
    {
        if (current == null || current.currentIndex >= current.extendScores.Length) { return; }
        var currentScore = (BigInteger)current.extendScores[current.currentIndex];
        if (oldScore >= currentScore) {
            current.nextScoreStored = null;
            current.currentIndex++; 
            Check(oldScore, newScore); 
            return; 
        }
        if (newScore < currentScore) { return; }
        if (Player.playersByControl.ContainsKey(Player.Role.SinglePlayer))
        {
            Player.playersByControl[Player.Role.SinglePlayer]?.AddLives(1);
            SpecialGameEventNotifier.Trigger(SpecialGameEventNotifier.EventType.Extend);
        }
        current.nextScoreStored = null;
        current.currentIndex++;
    }

    public static void Reset()
    {
        current = null;
    }

    public override IEnumerator Execute(IVariableContainer source)
    {
        currentIndex = 0;
        while (currentIndex < extendScores.Length && Session.main.score >= (BigInteger)extendScores[currentIndex])
        {
            currentIndex++;
        }
        current = this;
        yield break;
    }
}

