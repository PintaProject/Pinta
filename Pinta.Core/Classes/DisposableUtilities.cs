using System;
using System.Threading;

namespace Pinta.Core;

internal static class DisposableUtilities
{
	internal static IDisposable FromAction (Action action)
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
