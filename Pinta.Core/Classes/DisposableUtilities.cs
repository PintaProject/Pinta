using System;
using System.Threading;

namespace Pinta.Core;

public static class DisposableUtilities
{
	public static IDisposable FromAction (Action action)
	{
		return new ActionDisposable (action);
	}

	private sealed class ActionDisposable (Action action) : IDisposable
	{
		private Action? action = action;
		public void Dispose ()
		{
			Interlocked.Exchange (ref action, null)?.Invoke ();
		}
	}
}
