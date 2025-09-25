using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pinta.Core;

internal readonly record struct CompletionInfo (
	bool WasCanceled,
	IReadOnlyList<Exception> Errors);

internal sealed class RenderHandle
{
	internal double Progress
		=> get_progress ();

	/// <summary>
	/// Retrieves the union of all tiles that have finished rendering since the
	/// last time this method was called, and resets the updated area.
	/// </summary>
	/// <returns>
	/// True if there was an updated area to retrieve, otherwise false.
	/// </returns>
	internal bool TryConsumeBounds (out RectangleI bounds)
		=> bounds_consumer (out bounds);

	internal Task<CompletionInfo> Task { get; }
	internal void Cancel ()
	{
		cancellation.Cancel ();
	}

	private readonly CancellationTokenSource cancellation;
	private readonly BoundsConsumer bounds_consumer;
	private readonly Func<double> get_progress;

	internal RenderHandle (
		Task<CompletionInfo> task,
		CancellationTokenSource cts,
		BoundsConsumer boundsConsumer,
		Func<double> getProgress)
	{
		Task = task;
		cancellation = cts;
		bounds_consumer = boundsConsumer;
		get_progress = getProgress;
	}

	internal delegate bool BoundsConsumer (out RectangleI bounds);
}
