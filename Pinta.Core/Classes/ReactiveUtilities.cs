using System;
using System.Collections.Generic;

namespace Pinta.Core;

public static class ReactiveUtilities
{
	public static void NotifyAll<TMessage> (this LinkedList<IObserver<TMessage>> listeners, TMessage message)
	{
		foreach (var listener in listeners)
			listener.OnNext (message);
	}

	public static IDisposable Subscribe<TMessage> (this LinkedList<IObserver<TMessage>> listeners, IObserver<TMessage> @new)
	{
		var newNode = listeners.AddLast (@new);
		return DisposableUtilities.FromAction (() => listeners.Remove (newNode));
	}
}
