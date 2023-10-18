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
	Settings settings;

	internal struct Settings
	{
		internal int ThreadCount { get; set; }
		internal int TileWidth { get; set; }
		internal int TileHeight { get; set; }
		internal int UpdateMillis { get; set; }
		internal ThreadPriority ThreadPriority { get; set; }
	}

	BaseEffect? effect;

	bool is_rendering;
	bool cancel_render_flag;
	bool restart_render_flag;
	int render_id;
	int current_tile;

	readonly List<Exception> render_exceptions;

	uint timer_tick_id;

	readonly object updated_lock;
	bool is_updated;

	RectangleI updated_area;

	internal AsyncEffectRenderer (Settings settings)
	{
		if (settings.ThreadCount < 1)
			settings.ThreadCount = 1;

		if (settings.TileWidth < 0)
			throw new ArgumentException ("EffectRenderSettings.TileWidth");

		if (settings.TileHeight < 0)
			throw new ArgumentException ("EffectRenderSettings.TileHeight");

		if (settings.UpdateMillis <= 0)
			settings.UpdateMillis = 100;

		effect = null;
		this.settings = settings;

		is_rendering = false;
		render_id = 0;
		updated_lock = new object ();
		is_updated = false;
		render_exceptions = new List<Exception> ();

		timer_tick_id = 0;
	}

	internal bool IsRendering => is_rendering;

	internal double Progress => report_progress ();

	private Func<double> report_progress = () => 0;

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

		// If a render is already in progress, then cancel it,
		// and start a new render.
		if (IsRendering) {
			cancel_render_flag = true;
			restart_render_flag = true;
			return;
		}

		StartRender (source, dest, renderBounds);
	}

	internal void Cancel (
		Cairo.ImageSurface sourceSurface,
		Cairo.ImageSurface destSurface,
		RectangleI renderBounds)
	{
		Debug.WriteLine ("AsyncEffectRenderer.Cancel ()");
		cancel_render_flag = true;
		restart_render_flag = false;

		if (!IsRendering)
			HandleRenderCompletion (sourceSurface, destSurface, renderBounds);
	}

	protected abstract void OnUpdate (double progress, RectangleI updatedBounds);

	protected abstract void OnCompletion (bool canceled, Exception[] exceptions);

	internal void Dispose ()
	{
		if (timer_tick_id > 0)
			GLib.Source.Remove (timer_tick_id);

		timer_tick_id = 0;
	}

	void StartRender (
		Cairo.ImageSurface sourceSurface,
		Cairo.ImageSurface destSurface,
		RectangleI renderBounds)
	{
		is_rendering = true;
		cancel_render_flag = false;
		restart_render_flag = false;
		is_updated = false;

		render_id++;
		render_exceptions.Clear ();

		current_tile = -1;

		int totalTiles = CalculateTotalTiles (renderBounds);

		report_progress = () => {
			if (totalTiles == 0 || current_tile < 0)
				return 0;
			else if (current_tile < totalTiles)
				return (double) current_tile / (double) totalTiles;
			else
				return 1;
		};

		Debug.WriteLine ("AsyncEffectRenderer.Start () Render " + render_id + " starting.");

		// Copy the current render id.
		int renderId = render_id;

		// Start slave render threads.
		int threadCount = settings.ThreadCount;
		var slaves = new Thread[threadCount - 1];
		for (int threadId = 1; threadId < threadCount; threadId++)
			slaves[threadId - 1] = StartSlaveThread (sourceSurface, destSurface, renderBounds, totalTiles, renderId, threadId);

		// Start the master render thread.
		var master = new Thread (() => {

			// Do part of the rendering on the master thread.
			Render (sourceSurface, destSurface, renderBounds, totalTiles, renderId, 0);

			// Wait for slave threads to complete.
			foreach (var slave in slaves)
				slave.Join ();

			// Change back to the UI thread to notify of completion.
			GLib.Functions.TimeoutAdd (0, 0, () => {
				HandleRenderCompletion (sourceSurface, destSurface, renderBounds);
				return false; // don't call the timer again
			});
		}) {
			Priority = settings.ThreadPriority
		};
		master.Start ();

		// Start timer used to periodically fire update events on the UI thread.
		timer_tick_id = GLib.Functions.TimeoutAdd (0, (uint) settings.UpdateMillis, () => HandleTimerTick ());
	}

	Thread StartSlaveThread (
		Cairo.ImageSurface sourceSurface,
		Cairo.ImageSurface destSurface,
		RectangleI renderBounds,
		int totalTiles,
		int renderId,
		int threadId)
	{
		var slave = new Thread (() => {
			Render (sourceSurface, destSurface, renderBounds, totalTiles, renderId, threadId);
		}) {
			Priority = settings.ThreadPriority
		};
		slave.Start ();

		return slave;
	}

	// Runs on a background thread.
	void Render (
		Cairo.ImageSurface sourceSurface,
		Cairo.ImageSurface destSurface,
		RectangleI renderBounds,
		int totalTiles,
		int renderId,
		int threadId)
	{
		// Fetch the next tile index and render it.
		for (; ; ) {
			int tileIndex = Interlocked.Increment (ref current_tile);
			if (tileIndex >= totalTiles || cancel_render_flag)
				return;
			RenderTile (sourceSurface, destSurface, renderBounds, renderId, threadId, tileIndex);
		}
	}

	// Runs on a background thread.
	void RenderTile (
		Cairo.ImageSurface sourceSurface,
		Cairo.ImageSurface destSurface,
		RectangleI renderBounds,
		int renderId,
		int threadId,
		int tileIndex)
	{
		Exception? exception = null;
		var bounds = new RectangleI ();

		try {

			bounds = GetTileBounds (renderBounds, tileIndex);

			// NRT - These are set in Start () before getting here
			if (!cancel_render_flag) {
				destSurface.Flush ();
				effect!.Render (sourceSurface!, destSurface, stackalloc[] { bounds });
				destSurface.MarkDirty (bounds);
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
	RectangleI GetTileBounds (RectangleI renderBounds, int tileIndex)
	{
		int horizTileCount = (int) Math.Ceiling ((float) renderBounds.Width
						       / (float) settings.TileWidth);

		int x = ((tileIndex % horizTileCount) * settings.TileWidth) + renderBounds.X;
		int y = ((tileIndex / horizTileCount) * settings.TileHeight) + renderBounds.Y;
		int w = Math.Min (settings.TileWidth, renderBounds.Right + 1 - x);
		int h = Math.Min (settings.TileHeight, renderBounds.Bottom + 1 - y);

		return new RectangleI (x, y, w, h);
	}

	int CalculateTotalTiles (RectangleI renderBounds)
	{
		return (int) (Math.Ceiling ((float) renderBounds.Width / (float) settings.TileWidth)
			* Math.Ceiling ((float) renderBounds.Height / (float) settings.TileHeight));
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

	void HandleRenderCompletion (
		Cairo.ImageSurface sourceSurface,
		Cairo.ImageSurface destSurface,
		RectangleI renderBounds)
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
			StartRender (sourceSurface, destSurface, renderBounds);
		else
			is_rendering = false;
	}
}
