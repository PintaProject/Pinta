using System;

namespace Pinta.Core;

internal sealed class LockProvider
{
	public bool LockActive { get; private set; } = false;
	public IDisposable ProvideLock ()
	{
		LockActive = true;
		return Utility.CreateDisposable (() => LockActive = false);
	}
}
