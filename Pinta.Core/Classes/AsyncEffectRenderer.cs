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
using System.Collections.Generic;
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
		internal int TileWidth { get; }
		internal int TileHeight { get; }
		internal int UpdateMillis { get; }
		internal ThreadPriority ThreadPriority { get; }

		internal Settings (
			int threadCount,
			int tileWidth,
			int tileHeight,
			int updateMilliseconds,
			ThreadPriority threadPriority)
		{
			if (tileWidth < 0) throw new ArgumentOutOfRangeException (nameof (tileWidth), "Cannot be negative");
			if (tileHeight < 0) throw new ArgumentOutOfRangeException (nameof (tileHeight), "Cannot be negative");
			if (updateMilliseconds <= 0) throw new ArgumentOutOfRangeException (nameof (updateMilliseconds), "Strictly positive value expected");
			if (threadCount < 1) throw new ArgumentOutOfRangeException (nameof (threadCount), "Invalid number of threads");
			TileWidth = tileWidth;
			TileHeight = tileHeight;
			ThreadCount = threadCount;
			UpdateMillis = updateMilliseconds;
			ThreadPriority = threadPriority;
		}
	}

	BaseEffect? effect;
	Cairo.ImageSurface? source_surface;
	Cairo.ImageSurface? dest_surface;
	RectangleI render_bounds;

	bool is_rendering;
	bool cancel_render_flag;
	bool restart_render_flag;
	int render_id;
	int current_tile;
	int total_tiles;
	readonly List<Exception> render_exceptions;

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
		render_id = 0;
		updated_lock = new object ();
		is_updated = false;
		render_exceptions = new List<Exception> ();

		timer_tick_id = 0;

		this.settings = settings;
	}

	internal bool IsRendering => is_rendering;

	internal double Progress {
		get {
			if (total_tiles == 0 || current_tile < 0)
				return 0;
			else if (current_tile < total_tiles)
				return current_tile / (double) total_tiles;
			else
				return 1;
		}
	}

	internal void Start (
		BaseEffect effect,
		Cairo.ImageSurface source,
		Cairo.ImageSurface dest,
		RectangleI renderBounds)
	{
		Debug.WriteLine ("AsyncEffectRenderer.Start ()");

		// It is important the effect's properties don't change during rendering.
		// So a copy is made for the render.
		this.effect = effect.Clone ();

		source_surface = source;
		dest_surface = dest;
		render_bounds = renderBounds;

		// If a render is already in progress, then cancel it,
		// and start a new render.
		if (IsRendering) {
			cancel_render_flag = true;
			restart_render_flag = true;
			return;
		}

		StartRender ();
	}

	internal void Cancel ()
	{
		Debug.WriteLine ("AsyncEffectRenderer.Cancel ()");
		cancel_render_flag = true;
		restart_render_flag = false;

		if (!IsRendering)
			HandleRenderCompletion ();
	}

	protected abstract void OnUpdate (double progress, RectangleI updatedBounds);

	protected abstract void OnCompletion (bool canceled, Exception[] exceptions);

	internal void Dispose ()
	{
		if (timer_tick_id > 0)
			GLib.Source.Remove (timer_tick_id);

		timer_tick_id = 0;
	}

	void StartRender ()
	{
		is_rendering = true;
		cancel_render_flag = false;
		restart_render_flag = false;
		is_updated = false;

		render_id++;
		render_exceptions.Clear ();

		current_tile = -1;

		total_tiles = CalculateTotalTiles ();

		Debug.WriteLine ("AsyncEffectRenderer.Start () Render " + render_id + " starting.");

		// Copy the current render id.
		int renderId = render_id;

		// Start slave render threads.
		int threadCount = settings.ThreadCount;
		var slaves = new Thread[threadCount - 1];
		for (int threadId = 1; threadId < threadCount; threadId++)
			slaves[threadId - 1] = StartSlaveThread (renderId, threadId);

		// Start the master render thread.
		var master = new Thread (() => {

			// Do part of the rendering on the master thread.
			Render (renderId, 0);

			// Wait for slave threads to complete.
			foreach (var slave in slaves)
				slave.Join ();

			// Change back to the UI thread to notify of completion.
			GLib.Functions.TimeoutAdd (0, 0, () => {
				HandleRenderCompletion ();
				return false; // don't call the timer again
			});
		}) {
			Priority = settings.ThreadPriority
		};
		master.Start ();

		// Start timer used to periodically fire update events on the UI thread.
		timer_tick_id = GLib.Functions.TimeoutAdd (0, (uint) settings.UpdateMillis, () => HandleTimerTick ());
	}

	Thread StartSlaveThread (int renderId, int threadId)
	{
		var slave = new Thread (() => {
			Render (renderId, threadId);
		}) {
			Priority = settings.ThreadPriority
		};
		slave.Start ();

		return slave;
	}

	// Runs on a background thread.
	void Render (int renderId, int threadId)
	{
		// Fetch the next tile index and render it.
		for (; ; ) {
			int tileIndex = Interlocked.Increment (ref current_tile);
			if (tileIndex >= total_tiles || cancel_render_flag)
				return;
			RenderTile (renderId, threadId, tileIndex);
		}
	}

	// Runs on a background thread.
	void RenderTile (int renderId, int threadId, int tileIndex)
	{
		Exception? exception = null;
		var bounds = new RectangleI ();

		try {

			bounds = GetTileBounds (tileIndex);

			// NRT - These are set in Start () before getting here
			if (!cancel_render_flag) {
				dest_surface!.Flush ();
				effect!.Render (source_surface!, dest_surface, stackalloc[] { bounds });
				dest_surface.MarkDirty (bounds);
			}

		} catch (Exception ex) {
			exception = ex;
			Debug.WriteLine ("AsyncEffectRenderer Error while rendering effect: " + effect!.Name + " exception: " + ex.Message + "\n" + ex.StackTrace);
		}

		// Ignore completions of tiles after a cancel or from a previous render.
		if (!IsRendering || renderId != render_id)
			return;

		// Update bounds to be shown on next expose.
		lock (updated_lock) {
			if (is_updated) {
				updated_area = RectangleI.Union (bounds, updated_area);
			} else {
				is_updated = true;
				updated_area = bounds;
			}
		}

		if (exception != null) {
			lock (render_exceptions) {
				render_exceptions.Add (exception);
			}
		}
	}

	// Runs on a background thread.
	RectangleI GetTileBounds (int tileIndex)
	{
		int horizTileCount = (int) Math.Ceiling (render_bounds.Width
						       / (float) settings.TileWidth);

		int x = ((tileIndex % horizTileCount) * settings.TileWidth) + render_bounds.X;
		int y = ((tileIndex / horizTileCount) * settings.TileHeight) + render_bounds.Y;
		int w = Math.Min (settings.TileWidth, render_bounds.Right + 1 - x);
		int h = Math.Min (settings.TileHeight, render_bounds.Bottom + 1 - y);

		return new RectangleI (x, y, w, h);
	}

	int CalculateTotalTiles ()
	{
		return (int) (Math.Ceiling (render_bounds.Width / (float) settings.TileWidth)
			* Math.Ceiling (render_bounds.Height / (float) settings.TileHeight));
	}

	// Called on the UI thread.
	bool HandleTimerTick ()
	{
		Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " Timer tick.");

		RectangleI bounds;

		lock (updated_lock) {

			if (!is_updated)
				return true;

			is_updated = false;

			bounds = updated_area;
		}

		if (IsRendering && !cancel_render_flag)
			OnUpdate (Progress, bounds);

		return true;
	}

	void HandleRenderCompletion ()
	{
		var exceptions = (render_exceptions.Count == 0)
				? Array.Empty<Exception> ()
				: render_exceptions.ToArray ();

		HandleTimerTick ();

		if (timer_tick_id > 0)
			GLib.Source.Remove (timer_tick_id);

		timer_tick_id = 0;

		OnCompletion (cancel_render_flag, exceptions);

		if (restart_render_flag)
			StartRender ();
		else
			is_rendering = false;
	}
}
