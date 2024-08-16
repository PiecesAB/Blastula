using Godot;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Blastula.Coroutine;

public abstract class WaitPauseOption
{
    public bool runsWhenPaused;
}

public class WaitOneFrame : WaitPauseOption
{
        
}

public class WaitFrames : WaitPauseOption
{
    public long frames;

    public WaitFrames(long frames)
    {
        this.frames = frames;
    }
}

public class WaitTime : WaitPauseOption
{
    public double seconds;

    public WaitTime(double seconds)
    {
        this.seconds = seconds;
    }
}

public class WaitCondition
{
    public Func<bool> condition;

    public WaitCondition(Func<bool> condition)
    {
        this.condition = condition;
    }
}

public class SetCancel
{
    public Action<CoroutineUtility.Coroutine> cancel;

    public SetCancel(Action<CoroutineUtility.Coroutine> cancel)
    {
        this.cancel = cancel;
    }
}