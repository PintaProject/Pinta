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
		int current_tile;
		int total_tiles;		
		List<Exception> render_exceptions;
		
		uint timer_tick_id;
		
		object updated_lock;
		bool is_updated;
		int updated_x1;
		int updated_y1;
		int updated_x2;
		int updated_y2;
		
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
			updated_lock = new object ();
			is_updated = false;
			render_exceptions = new List<Exception> ();	
			
			timer_tick_id = 0;
		}		
		
		internal bool IsRendering {
			get { return is_rendering; }
		}
		
		internal double Progress {
			get {
				if (total_tiles == 0 || current_tile < 0)
					return 0;
				else if (current_tile < total_tiles)
					return (double)current_tile / (double)total_tiles;
				else
					return 1;
			}
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
		
		protected abstract void OnUpdate (double progress, Gdk.Rectangle updatedBounds);
		
		protected abstract void OnCompletion (bool canceled, Exception[] exceptions);
		
		internal void Dispose ()
		{
			if (timer_tick_id > 0)
				GLib.Source.Remove (timer_tick_id);
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
				Gtk.Application.Invoke ((o,e) => HandleRenderCompletion ());
			});
			
			master.Priority = settings.ThreadPriority;
			master.Start ();
			
			// Start timer used to periodically fire update events on the UI thread.
			timer_tick_id = GLib.Timeout.Add((uint) settings.UpdateMillis, HandleTimerTick);			
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
			// Fetch the next tile index and render it.
			for (;;) {
				
				int tileIndex = Interlocked.Increment (ref current_tile);
				
				if (tileIndex >= total_tiles || cancel_render_flag)
					return;
				
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
				
				if (!cancel_render_flag) {
					dest_surface.Flush ();
					effect.Render (source_surface, dest_surface, new [] { bounds });
					dest_surface.MarkDirty (bounds.ToCairoRectangle ());
				}
				
			} catch (Exception ex) {		
				exception = ex;
				Debug.WriteLine ("AsyncEffectRenderer Error while rendering effect: " + effect.Name + " exception: " + ex.Message + "\n" + ex.StackTrace);
			}
			
			// Ignore completions of tiles after a cancel or from a previous render.
			if (!IsRendering || renderId != render_id)
				return;
			
			// Update bounds to be shown on next expose.
			lock (updated_lock) {
				if (is_updated) {				
					updated_x1 = Math.Min (bounds.X, updated_x1);
					updated_y1 = Math.Min (bounds.Y, updated_y1);
					updated_x2 = Math.Max (bounds.X + bounds.Width, updated_x2);
					updated_y2 = Math.Max (bounds.Y + bounds.Height, updated_y2);
				} else {
					is_updated = true;
					updated_x1 = bounds.X;
					updated_y1 = bounds.Y;
					updated_x2 = bounds.X + bounds.Width;
					updated_y2 = bounds.Y + bounds.Height;
				}
			}
			
			if (exception != null) {
				lock (render_exceptions) {
					render_exceptions.Add (exception);
				}
			}
		}
		
		// Runs on a background thread.
		Gdk.Rectangle GetTileBounds (int tileIndex)
		{
			int horizTileCount = (int)Math.Ceiling((float)render_bounds.Width 
			                                       / (float)settings.TileWidth);
			
            int x = ((tileIndex % horizTileCount) * settings.TileWidth) + render_bounds.X;
            int y = ((tileIndex / horizTileCount) * settings.TileHeight) + render_bounds.Y;
            int w = Math.Min(settings.TileWidth, render_bounds.GetRight () + 1 - x);
            int h = Math.Min(settings.TileHeight, render_bounds.GetBottom () + 1 - y);
			
			return new Gdk.Rectangle (x, y, w, h);			
		}
		
		int CalculateTotalTiles ()
		{
			return (int)(Math.Ceiling((float)render_bounds.Width / (float)settings.TileWidth)
                                * Math.Ceiling((float)render_bounds.Height / (float)settings.TileHeight));
		}
		
		// Called on the UI thread.
		bool HandleTimerTick ()
		{			
			Debug.WriteLine (DateTime.Now.ToString("HH:mm:ss:ffff") + " Timer tick.");
			
			Gdk.Rectangle bounds;
			
			lock (updated_lock) {
				
				if (!is_updated)
					return true;
			
				is_updated = false;
				
				bounds = new Gdk.Rectangle (updated_x1,
			    	                        updated_y1,
				    	                    updated_x2 - updated_x1,
				        	                updated_y2 - updated_y1);
			}
			
			if (IsRendering && !cancel_render_flag)
				OnUpdate (Progress, bounds);
			
			return true;
		}
		
		void HandleRenderCompletion ()
		{
			var exceptions = (render_exceptions == null || render_exceptions.Count == 0)
			                  ? null
			                  : render_exceptions.ToArray ();
			
			HandleTimerTick ();
			
			if (timer_tick_id > 0)
				GLib.Source.Remove (timer_tick_id);
			
			OnCompletion (cancel_render_flag, exceptions);
			
			if (restart_render_flag)
				StartRender ();
			else
				is_rendering = false;
		}
	}
}
