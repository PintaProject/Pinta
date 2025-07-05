using System;

namespace Pinta.Core;

public readonly struct GLibTimerWrapper : IDisposable
{
	public uint TimerID { get; }
	private GLibTimerWrapper (uint timerId)
	{
		TimerID = timerId;
	}

	public void Dispose ()
	{
		if (TimerID == 0) return;
		GLib.Source.Remove (TimerID);
	}

	public static implicit operator GLibTimerWrapper (uint timerId)
		=> new (timerId);
}
