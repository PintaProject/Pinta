using System;

namespace Pinta.Core;

internal sealed class LockProvider
{
	private readonly object @lock = new ();
	public bool LockActive { get; private set; } = false;
	public IDisposable ProvideLock ()
	{
		lock (@lock) {
			if (LockActive) throw new InvalidOperationException ("Lock is active");
			LockActive = true;
			return Utility.CreateDisposable (() => LockActive = false);
		}
	}
}
