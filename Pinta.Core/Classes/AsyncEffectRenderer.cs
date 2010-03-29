// 
// AsyncEffectRenderer.cs
//  
// Author:
//       greg <${AuthorEmail}>
// 
// Copyright (c) 2010 greg
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

using System;
using System.Collections.Generic;
using System.Threading;
using Debug = System.Diagnostics.Debug;

namespace Pinta.Core
{
	
	// Only call methods on this class from a single thread (The UI thread).
	internal abstract class AsyncEffectRenderer
	{
		Settings settings;
		
		internal struct Settings {
			internal int ThreadCount { get; set; }
			internal int TileWidth { get; set; }
			internal int TileHeight { get; set; }
			internal int UpdateMillis { get; set; }
			internal ThreadPriority ThreadPriority { get; set; }
		}		
		
		BaseEffect effect;
		Cairo.ImageSurface source_surface;
		Cairo.ImageSurface dest_surface;
		Gdk.Rectangle render_bounds;
		
		bool is_rendering;
		bool cancel_render_flag;
		bool restart_render_flag;
		int render_id;
		int rendered_tiles;
		int total_tiles;
		List<Exception> render_exceptions;
		
		bool stop_timer_flag;
		GLib.Timeout timer;
		List<Gdk.Rectangle> updated_rectangles;
		
		internal AsyncEffectRenderer (Settings settings)
		{
			if (settings.ThreadCount < 1)
				settings.ThreadCount = 1;
			
			if (settings.TileWidth < 1)
				throw new ArgumentException ("EffectRenderSettings.TileWidth");
			
			if (settings.TileHeight < 1)
				throw new ArgumentException ("EffectRenderSettings.TileHeight");			
			
			if (settings.UpdateMillis <= 0)
				settings.UpdateMillis = 100;
			
			effect = null;
			source_surface = null;
			dest_surface = null;
			this.settings = settings;
			
			is_rendering = false;
			render_id = 0;
			render_exceptions = new List<Exception> ();		
		}		
		
		internal bool IsRendering {
			get { return is_rendering; }
		}
		
		internal double Progress {
			get { return (total_tiles == 0) ? 0 : rendered_tiles / total_tiles; }
		}
		
		internal void Start (BaseEffect effect,
		                     Cairo.ImageSurface source,
		                     Cairo.ImageSurface dest,
		                     Gdk.Rectangle renderBounds)
		{
			Debug.WriteLine ("AsyncEffectRenderer.Start ()");
			
			if (effect == null)
				throw new ArgumentNullException ("effect");
			
			if (source == null)
				throw new ArgumentNullException ("source");
			
			if (dest == null)
				throw new ArgumentNullException ("dest");
			
			if (renderBounds.IsEmpty)
				throw new ArgumentException ("renderBounds.IsEmpty");
			
			// It is important the effect's properties don't change during rendering.
			// So a copy is made for the render.
			this.effect = effect.Clone();
			
			this.source_surface = source;
			this.dest_surface = dest;
			this.render_bounds = renderBounds;
			
			// If a render is already in progress, then cancel it,
			// and start a new render.
			if (IsRendering) {
				cancel_render_flag = true;
				restart_render_flag = true;
				return;
			}			
			
			GLib.Timeout.Add((uint) settings.UpdateMillis, HandleTimerTick);
			
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
		
		protected abstract void OnUpdate (double progress, Gdk.Rectangle[] updatedBounds);
		
		protected abstract void OnCompletion (bool canceled, Exception[] exceptions);
		
		internal void Dispose ()
		{
			stop_timer_flag = true;
		}
		
		void StartRender ()
		{
			is_rendering = true;
			cancel_render_flag = false;			
			restart_render_flag = false;
			
			render_id++;
			render_exceptions.Clear ();
			
			rendered_tiles = 0;
			
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
				Gtk.Application.Invoke ((o,e) => HandleRenderCompletion ());
			});
			
			master.Priority = settings.ThreadPriority;
			master.Start ();
			
			// Start timer used to throttle update events.
			stop_timer_flag = false;
			updated_rectangles = new List<Gdk.Rectangle> ();			
		}
		
		Thread StartSlaveThread (int renderId, int threadId)
		{
			var slave = new Thread(() => {
				Render (renderId, threadId);
			});
			
			slave.Priority = settings.ThreadPriority;
			slave.Start ();
			
			return slave;
		}
		
		// Runs on a background thread.
		void Render (int renderId, int threadId)
		{			
			for (int tileIndex = threadId; tileIndex < total_tiles; tileIndex += settings.ThreadCount) {
				
				if (cancel_render_flag)
					break;
				
				RenderTile (renderId, threadId, tileIndex);
			}
		}
		
		// Runs on a background thread.
		void RenderTile (int renderId, int threadId, int tileIndex)
		{
			Exception exception = null;
			Gdk.Rectangle bounds = new Gdk.Rectangle ();
			
			try {
				
				bounds = GetTileBounds (tileIndex);
				
				if (!cancel_render_flag)
					effect.RenderEffect (source_surface, dest_surface, new [] { bounds });
				
			} catch (Exception ex) {		
				exception = ex;
				//cancel_render_flag = true; //TODO could cancel render on first error detected.
				Debug.WriteLine ("AsyncEffectRenderer Error while rendering effect: " + effect.Text + " exception: " + ex.Message + "\n" + ex.StackTrace);
			}
			
			// Notify the UI thread that a tile has been rendered (or of a failure/cancellation)/
			bool cancelFlag = cancel_render_flag;
			Gtk.Application.Invoke ((o, e) => {				
				HandleTileRenderCompletion (renderId, threadId, tileIndex, bounds, cancelFlag, exception);
			});			
		}
		
		// Runs on a background thread.
		Gdk.Rectangle GetTileBounds (int tileIndex)
		{
			int horizTileCount = (int)Math.Ceiling((float)render_bounds.Width 
			                                       / (float)settings.TileWidth);
			
            int x = ((tileIndex % horizTileCount) * settings.TileWidth) + render_bounds.X;
            int y = ((tileIndex / horizTileCount) * settings.TileHeight) + render_bounds.Y;
            int w = Math.Min(settings.TileWidth, render_bounds.Right - x);
            int h = Math.Min(settings.TileHeight, render_bounds.Bottom - y);
			
			return new Gdk.Rectangle (x, y, w, h);			
		}
		
		int CalculateTotalTiles ()
		{
			return (int)(Math.Ceiling((float)render_bounds.Width / (float)settings.TileWidth)
                                * Math.Ceiling((float)render_bounds.Height / (float)settings.TileHeight));
		}
		
		void HandleTileRenderCompletion (int renderId,
		                           int threadId,
		                           int tileIndex,
		                           Gdk.Rectangle tileBounds,
		                           bool canceled,
		                           Exception exception)
		{
			// Ignore completions of tiles after a cancel or from a previous render;
			if (!IsRendering && renderId != render_id)
				return;
			
			rendered_tiles++;
			
			//TODO perhaps combine bounds instead.
			if (!cancel_render_flag && exception == null)
				updated_rectangles.Add (tileBounds);
			
			if (exception != null)
				render_exceptions.Add (exception);
		}
		
		bool HandleTimerTick ()
		{
			if (stop_timer_flag) {
				Debug.WriteLine (DateTime.Now.ToString("HH:mm:ss:ffff") + " Stop timer.");
				return false;
			}
			
			Debug.WriteLine (DateTime.Now.ToString("HH:mm:ss:ffff") + " Timer tick.");
			
			if (updated_rectangles.Count == 0)
				return true;
			
			double progress = (double)rendered_tiles / (double)total_tiles;
			
			if (!cancel_render_flag)
				OnUpdate (progress, updated_rectangles.ToArray());
			
			updated_rectangles.Clear ();
			
			return true;
		}
		
		void HandleRenderCompletion ()
		{
			var exceptions = (render_exceptions == null) ? null : render_exceptions.ToArray ();
		
			HandleTimerTick ();
			
			stop_timer_flag = true;
			
			OnCompletion (cancel_render_flag, exceptions);
			
			if (restart_render_flag)
				StartRender ();
			else
				is_rendering = false;
		}
	}
}
