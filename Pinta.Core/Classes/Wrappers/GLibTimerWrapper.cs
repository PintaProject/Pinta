using System;

namespace Pinta.Core;

public readonly struct GLibTimerWrapper : IDisposable
{
	private readonly uint timer_id;
	private GLibTimerWrapper (uint timerId)
	{
		timer_id = timerId;
	}

	public void Dispose ()
	{
		if (timer_id == 0) return;
		GLib.Source.Remove (timer_id);
	}

	public static implicit operator GLibTimerWrapper (uint timerId)
		=> new (timerId);
}
