using System;

namespace Pinta.Core;

public static class DisposableHelper
{
	public static IDisposable FromDelegate (Action disposeAction)
		=> new DelegateDisposable (disposeAction);

	private sealed class DelegateDisposable : IDisposable
	{
		private readonly Action dispose_action;
		internal DelegateDisposable (Action disposeAction)
		{
			dispose_action = disposeAction;
		}

		private bool disposed = false;
		public void Dispose ()
		{
			if (disposed) return;
			dispose_action ();
			disposed = true;
		}
	}
}
