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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Debug = System.Diagnostics.Debug;

namespace Pinta.Core;

// Only call methods on this class from a single thread (The UI thread).
internal static class AsyncEffectRenderer
{
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

	internal static RenderHandle Start (
		Settings settings,
		BaseEffect effect,
		Cairo.ImageSurface source,
		Cairo.ImageSurface dest)
	{
		object updatedLock = new ();
		bool isUpdated = false;
		RectangleI updatedArea = RectangleI.Zero;

		CancellationTokenSource cts = new ();

		Debug.WriteLine ("AsyncEffectRenderer.Start ()");

		// It is important the effect's properties don't change during rendering.
		// So a copy is made for the render.
		BaseEffect effectClone = effect.Clone ();

		ConcurrentQueue<Exception> renderExceptions = new ();

		ConcurrentQueue<RectangleI> queuedTiles = new (
			settings.EffectIsTileable
			? settings.RenderBounds.ToRows () // If effect is tileable, render each row in parallel.
			: [settings.RenderBounds]); // If the effect isn't tileable, there is a single tile for the entire render bounds

		int tilesCount = queuedTiles.Count;

		Debug.WriteLine ("AsyncEffectRenderer.Start () Render starting.");

		var tasks =
			Enumerable.Range (0, settings.ThreadCount)
			.Select (_ => Task.Run (RenderNextTile));

		var aggregateTask =
			Task
			.WhenAll (tasks)
			.ContinueWith (
				_ => new CompletionInfo (
					WasCanceled: cts.Token.IsCancellationRequested,
					Errors: [.. renderExceptions]
				)
			);

		return new (
			aggregateTask,
			cts,
			TryConsumeBounds,
			GetProgress);

		// ---------------
		// === Methods ===
		// ---------------


		double GetProgress ()
		{
			if (tilesCount == 0) return 0;
			int dequeued = tilesCount - queuedTiles.Count;
			return dequeued / (double) tilesCount;
		}

		bool TryConsumeBounds (out RectangleI bounds)
		{
			lock (updatedLock) {
				if (!isUpdated) {
					bounds = default;
					return false;
				}

				bounds = updatedArea;
				isUpdated = false;
				return true;
			}
		}

		// Runs on a background thread.
		void RenderNextTile ()
		{
			// Fetch the next tile index and render it.
			while (true) {

				if (cts.Token.IsCancellationRequested) return;
				if (!queuedTiles.TryDequeue (out RectangleI tileBounds)) return;
				try {
					// NRT - These are set in Start () before getting here
					if (!cts.Token.IsCancellationRequested) {
						dest.Flush ();
						effectClone.Render (source, dest, [tileBounds]);
						dest.MarkDirty (tileBounds);
					}
				} catch (Exception ex) {
					renderExceptions.Enqueue (ex);
				}

				// Ignore completions of tiles after a cancel
				if (cts.Token.IsCancellationRequested)
					return;

				// Update bounds to be shown on next expose.
				lock (updatedLock) {
					if (isUpdated) {
						updatedArea = RectangleI.Union (tileBounds, updatedArea);
					} else {
						isUpdated = true;
						updatedArea = tileBounds;
					}
				}
			}
		}
	}
}
