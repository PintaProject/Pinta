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
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Debug = System.Diagnostics.Debug;

namespace Pinta.Core;


// Only call methods on this class from a single thread (The UI thread).
internal abstract class AsyncEffectRenderer
{
	private readonly Settings settings;

	internal sealed class Settings
	{
		internal int ThreadCount { get; }
		internal RectangleI RenderBounds { get; }
		internal bool EffectIsTileable { get; }
		internal int UpdateMillis { get; }
		internal ThreadPriority ThreadPriority { get; }

		internal Settings (
			int threadCount,
			RectangleI renderBounds,
			bool effectIsTileable,
			int updateMilliseconds,
			ThreadPriority threadPriority)
		{
			if (renderBounds.Width < 0) throw new ArgumentException ("Width cannot be negative", nameof (renderBounds));
			if (renderBounds.Height < 0) throw new ArgumentException ("Height cannot be negative", nameof (renderBounds));
			if (updateMilliseconds <= 0) throw new ArgumentOutOfRangeException (nameof (updateMilliseconds), "Strictly positive value expected");
			if (threadCount < 1) throw new ArgumentOutOfRangeException (nameof (threadCount), "Invalid number of threads");
			RenderBounds = renderBounds;
			EffectIsTileable = effectIsTileable;
			ThreadCount = threadCount;
			UpdateMillis = updateMilliseconds;
			ThreadPriority = threadPriority;
		}
	}

	BaseEffect? effect;
	Cairo.ImageSurface? source_surface;
	Cairo.ImageSurface? dest_surface;

	bool is_rendering;
	bool restart_render_flag;
	CancellationTokenSource cancellation_source;
	ConcurrentQueue<RectangleI> queued_tiles;
	int tiles_count = 0;
	readonly ConcurrentQueue<Exception> render_exceptions;

	uint timer_tick_id;

	readonly object updated_lock;
	bool is_updated;

	RectangleI updated_area;

	internal AsyncEffectRenderer (Settings settings)
	{
		effect = null;
		source_surface = null;
		dest_surface = null;

		is_rendering = false;
		cancellation_source = new ();
		updated_lock = new object ();
		is_updated = false;
		queued_tiles = new ConcurrentQueue<RectangleI> ();
		render_exceptions = new ConcurrentQueue<Exception> ();

		timer_tick_id = 0;

		this.settings = settings;
	}

	internal bool IsRendering => is_rendering;

	internal double Progress {
		get {
			if (tiles_count == 0) return 0;
			int dequeued = tiles_count - queued_tiles.Count;
			return dequeued / (double) tiles_count;
		}
	}

	internal void Start (
		BaseEffect effect,
		Cairo.ImageSurface source,
		Cairo.ImageSurface dest)
	{
		Debug.WriteLine ("AsyncEffectRenderer.Start ()");

		// It is important the effect's properties don't change during rendering.
		// So a copy is made for the render.
		this.effect = effect.Clone ();

		source_surface = source;
		dest_surface = dest;

		// If a render is already in progress, then cancel it,
		// and start a new render.
		if (IsRendering) {
			cancellation_source.Cancel ();
			restart_render_flag = true;
			return;
		}

		StartRender ();
	}

	internal void Cancel ()
	{
		Debug.WriteLine ("AsyncEffectRenderer.Cancel ()");

		CancellationToken cancellationToken = cancellation_source.Token;
		cancellation_source.Cancel ();
		restart_render_flag = false;

		if (!IsRendering)
			HandleRenderCompletion (cancellationToken);
	}

	protected abstract void OnUpdate (
		double progress,
		RectangleI updatedBounds);

	protected abstract void OnCompletion (
		IReadOnlyList<Exception> exceptions,
		CancellationToken cancellationToken);

	internal void Dispose ()
	{
		if (timer_tick_id > 0)
			GLib.Source.Remove (timer_tick_id);

		timer_tick_id = 0;
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

	void StartRender ()
	{
		// ------------
		// === Body ===
		// ------------

		is_rendering = true;
		restart_render_flag = false;
		is_updated = false;

		render_exceptions.Clear ();

		ConcurrentQueue<RectangleI> targetTiles = new (
			settings.EffectIsTileable
			? settings.RenderBounds.ToRows () // If effect is tileable, render each row in parallel.
			: new[] { settings.RenderBounds }); // If the effect isn't tileable, there is a single tile for the entire render bounds

		queued_tiles = targetTiles;
		tiles_count = targetTiles.Count;

		CancellationToken cancellationToken = ReplaceCancellationSource ();

		Debug.WriteLine ("AsyncEffectRenderer.Start () Render starting."); // TODO: Show some kind of ID, perhaps the address

		// Start slave render threads.
		var slaves =
			Enumerable.Range (0, settings.ThreadCount - 1)
			.Select (_ => StartSlaveThread (cancellationToken))
			.ToImmutableArray ();

		// Start the master render thread.
		Thread master = StartMasterThread (cancellationToken, slaves);

		// Start timer used to periodically fire update events on the UI thread.
		timer_tick_id = GLib.Functions.TimeoutAdd (
			0,
			(uint) settings.UpdateMillis,
			() => HandleTimerTick (cancellationToken));

		// ---------------
		// === Methods ===
		// ---------------

		Thread StartSlaveThread (CancellationToken cancellationToken)
		{
			return StartRenderThread (() => RenderNextTile (cancellationToken));
		}

		Thread StartRenderThread (ThreadStart callback)
		{
			Thread result = new (callback) { Priority = settings.ThreadPriority };
			result.Start ();
			return result;
		}

		Thread StartMasterThread (CancellationToken cancellationToken, ImmutableArray<Thread> slaves)
		{
			return StartRenderThread (() => {

				// Do part of the rendering on the master thread.
				RenderNextTile (cancellationToken);

				// Wait for slave threads to complete.
				foreach (var slave in slaves)
					slave.Join ();

				// Change back to the UI thread to notify of completion.
				GLib.Functions.TimeoutAdd (
					0,
					0,
					() => {
						HandleRenderCompletion (cancellationToken);
						return false; // don't call the timer again
					}
				);

			});
		}
	}

	// Runs on a background thread.
	void RenderNextTile (CancellationToken cancellationToken)
	{
		// Fetch the next tile index and render it.
		while (true) {

			if (cancellationToken.IsCancellationRequested) return;
			if (!queued_tiles.TryDequeue (out RectangleI tileBounds)) return;

			Exception? exception = null;

			try {
				// NRT - These are set in Start () before getting here
				if (!cancellationToken.IsCancellationRequested) {
					dest_surface!.Flush ();
					effect!.Render (source_surface!, dest_surface, stackalloc[] { tileBounds });
					dest_surface.MarkDirty (tileBounds);
				}

			} catch (Exception ex) {
				exception = ex;
				Debug.WriteLine ("AsyncEffectRenderer Error while rendering effect: " + effect!.Name + " exception: " + ex.Message + "\n" + ex.StackTrace);
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

			if (exception == null)
				continue;

			render_exceptions.Enqueue (exception);
		}
	}

	// Called on the UI thread.
	bool HandleTimerTick (CancellationToken cancellationToken)
	{
		Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " Timer tick.");

		RectangleI bounds;

		lock (updated_lock) {

			if (!is_updated)
				return true;

			is_updated = false;

			bounds = updated_area;
		}

		if (IsRendering && !cancellationToken.IsCancellationRequested)
			OnUpdate (Progress, bounds);

		return true;
	}

	void HandleRenderCompletion (CancellationToken cancellationToken)
	{
		var exceptions =
			render_exceptions.IsEmpty
			? Array.Empty<Exception> ()
			: render_exceptions.ToArray ();

		HandleTimerTick (cancellationToken);

		if (timer_tick_id > 0)
			GLib.Source.Remove (timer_tick_id);

		timer_tick_id = 0;

		OnCompletion (exceptions, cancellationToken);

		if (restart_render_flag)
			StartRender ();
		else
			is_rendering = false;
	}
}
