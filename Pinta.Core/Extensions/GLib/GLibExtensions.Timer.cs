using System;

namespace Pinta.Core;

public readonly struct GLibTimer : IDisposable
{
	public uint TimerID { get; }
	private GLibTimer (uint timerId)
	{
		TimerID = timerId;
	}

	public void Dispose ()
	{
		if (TimerID == 0) return;
		GLib.Source.Remove (TimerID);
	}

	public static implicit operator GLibTimer (uint timerId)
		=> new (timerId);
}
