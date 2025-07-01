//
// AsyncEffectRenderer.cs
//
// Author:
//       Greg Lowe <greg@vis.net.nz>
//
// Copyright (c) 2010 Greg Lowe
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#if (!LIVE_PREVIEW_DEBUG && DEBUG)
#undef DEBUG
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Debug = System.Diagnostics.Debug;

namespace Pinta.Core;

// Only call methods on this class from a single thread (The UI thread).
internal sealed class AsyncEffectRenderer
{
	internal readonly record struct CompletionInfo (
		bool WasCanceled,
		IReadOnlyList<Exception> Errors);

	internal sealed class Settings
	{
		internal int ThreadCount { get; }
		internal RectangleI RenderBounds { get; }
		internal bool EffectIsTileable { get; }

		internal Settings (
			int threadCount,
			RectangleI renderBounds,
			bool effectIsTileable)
		{
			if (renderBounds.Width < 0) throw new ArgumentException ("Width cannot be negative", nameof (renderBounds));
			if (renderBounds.Height < 0) throw new ArgumentException ("Height cannot be negative", nameof (renderBounds));
			if (threadCount < 1) throw new ArgumentOutOfRangeException (nameof (threadCount), "Invalid number of threads");
			RenderBounds = renderBounds;
			EffectIsTileable = effectIsTileable;
			ThreadCount = threadCount;
		}
	}

	TaskCompletionSource<CompletionInfo> completion_source;
	CancellationTokenSource cancellation_source;
	ConcurrentQueue<RectangleI> queued_tiles;
	int tiles_count = 0;

	private readonly object updated_lock = new ();
	private bool is_updated = false;
	private RectangleI updated_area;

	private readonly Settings settings;

	internal AsyncEffectRenderer (Settings settings)
	{
		TaskCompletionSource<CompletionInfo> initialCompletionSource = new ();
		initialCompletionSource.SetResult (
			new (
				WasCanceled: false,
				Errors: []
			)
		);

		completion_source = initialCompletionSource;
		cancellation_source = new ();
		queued_tiles = new ConcurrentQueue<RectangleI> ();

		this.settings = settings;
	}

	internal bool IsRendering => !completion_source.Task.IsCompleted;

	internal double Progress {
		get {
			if (tiles_count == 0) return 0;
			int dequeued = tiles_count - queued_tiles.Count;
			return dequeued / (double) tiles_count;
		}
	}

	/// <summary>
	/// Retrieves the union of all tiles that have finished rendering since the
	/// last time this method was called, and resets the updated area.
	/// </summary>
	/// <returns>
	/// True if there was an updated area to retrieve, otherwise false.
	/// </returns>
	internal bool TryConsumeBounds (out RectangleI bounds)
	{
		lock (updated_lock) {
			if (!is_updated) {
				bounds = default;
				return false;
			}

			bounds = updated_area;
			is_updated = false;
			return true;
		}
	}

	internal async void Start (
		BaseEffect effect,
		Cairo.ImageSurface source,
		Cairo.ImageSurface dest)
	{
		if (IsRendering) throw new InvalidOperationException ("Render is in progress");

		TaskCompletionSource<CompletionInfo> newCompletionSource = new ();
		completion_source = newCompletionSource;

		Debug.WriteLine ("AsyncEffectRenderer.Start ()");

		// It is important the effect's properties don't change during rendering.
		// So a copy is made for the render.
		BaseEffect effectClone = effect.Clone ();

		ConcurrentQueue<Exception> renderExceptions = new ();

		ConcurrentQueue<RectangleI> targetTiles = new (
			settings.EffectIsTileable
			? settings.RenderBounds.ToRows () // If effect is tileable, render each row in parallel.
			: [settings.RenderBounds]); // If the effect isn't tileable, there is a single tile for the entire render bounds

		queued_tiles = targetTiles;
		tiles_count = targetTiles.Count;

		CancellationToken cancellationToken = ReplaceCancellationSource ();

		Debug.WriteLine ("AsyncEffectRenderer.Start () Render starting.");

		var tasks =
			Enumerable.Range (0, settings.ThreadCount)
			.Select (_ => Task.Run (RenderNextTile));

		await Task.WhenAll (tasks);

		// Change back to the UI thread to notify of completion.
		GLib.Functions.TimeoutAdd (
			0,
			0,
			() => {

				CompletionInfo completion = new (
					WasCanceled: cancellationToken.IsCancellationRequested,
					Errors: [.. renderExceptions]);

				newCompletionSource.SetResult (completion);

				return false; // don't call the timer again
			}
		);

		// ---------------
		// === Methods ===
		// ---------------

		// Runs on a background thread.
		void RenderNextTile ()
		{
			// Fetch the next tile index and render it.
			while (true) {

				if (cancellationToken.IsCancellationRequested) return;
				if (!queued_tiles.TryDequeue (out RectangleI tileBounds)) return;
				try {
					// NRT - These are set in Start () before getting here
					if (!cancellationToken.IsCancellationRequested) {
						dest.Flush ();
						effectClone.Render (source, dest, [tileBounds]);
						dest.MarkDirty (tileBounds);
					}
				} catch (Exception ex) {
					renderExceptions.Enqueue (ex);
				}

				// Ignore completions of tiles after a cancel or from a previous render.
				if (!IsRendering || cancellationToken.IsCancellationRequested)
					return;

				// Update bounds to be shown on next expose.
				lock (updated_lock) {
					if (is_updated) {
						updated_area = RectangleI.Union (tileBounds, updated_area);
					} else {
						is_updated = true;
						updated_area = tileBounds;
					}
				}
			}
		}
	}

	internal Task<CompletionInfo> Finish (bool cancel)
	{
		if (cancel) {
			Debug.WriteLine ("AsyncEffectRenderer.Cancel ()");
			cancellation_source.Cancel ();
		}
		return completion_source.Task;
	}

	CancellationToken ReplaceCancellationSource ()
	{
		CancellationTokenSource newSource = new ();
		CancellationTokenSource oldSource = cancellation_source;
		oldSource.Cancel (); // Safe to call multiple times
		oldSource.Dispose (); // Not safe to call multiple times, so this is the only place it should be called
		cancellation_source = newSource;
		return newSource.Token;
	}
}
