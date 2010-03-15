// 
// LivePreviewManager.cs
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

using System;
using System.Collections.Generic;
using System.Threading;
using Debug = System.Diagnostics.Debug;

namespace Pinta.Core
{

	public class LivePreviewManager
	{
		bool live_preview_enabled;		
		Layer layer;
		IBaseEffectLivePreviewHack effect; //TODO change this back to BaseEffect.	
		
		bool apply_live_preview_flag;
		bool cancel_live_preview_flag;
		bool cancel_render_flag;
		bool restart_render_flag;
			
		int render_id;
		EffectData effect_data;
		Cairo.ImageSurface surface;
		Gdk.Rectangle render_bounds;		
		int total_tiles;
		int rendered_tiles;
		List<Exception> render_exceptions;
		
		int running_render_threads;
		
		const int render_thread_count = 1;
		const uint render_completion_wait_millis = 50;
		const int render_tile_width = 128;
		const int render_tile_height = 128;

		delegate void Task ();
		
		public LivePreviewManager ()
		{
			live_preview_enabled = false;			
			render_id = 0;
			render_exceptions = new List<Exception> ();
		}
		
		public bool IsEnabled { get { return live_preview_enabled; } }
		
		public event EventHandler<LivePreviewStartedEventArgs> Started;
		public event EventHandler<LivePreviewRenderUpdatedEventArgs> RenderUpdated;
		public event EventHandler<LivePreviewEndedEventArgs> Ended;
		
		//TODO use current selection when applying effect.
		public void Start (IBaseEffectLivePreviewHack effect)
		{			
			if (live_preview_enabled)
				throw new InvalidOperationException ("LivePreviewManager.Start() called while live preview is already enabled.");	
			
			// Create live preview surface.
			// Start rendering.
			// Listen for changes to effectConfiguration object, and restart render if needed.
			
			live_preview_enabled = true;
			apply_live_preview_flag = false;
			cancel_live_preview_flag = false;
			
			layer = PintaCore.Layers.CurrentLayer;
			this.effect = effect;
			
			// Show a busy cursor, and make the main window insensitive,
			// until the cancel has completed.
			PintaCore.Chrome.MainWindowBusy = true;			
			
			// Set render bounds to selection.
			PintaCore.Layers.FinishSelection ();
			render_bounds = PintaCore.Layers.SelectionPath.GetBounds ();
			render_bounds = PintaCore.Workspace.ClampToImageSize (render_bounds);			
									
			//TODO use current tool layer.
			surface = new Cairo.ImageSurface (Cairo.Format.Argb32,
			                                  PintaCore.Workspace.CanvasSize.X,
			                                  PintaCore.Workspace.CanvasSize.Y);
			
			// Clear the surface and paint the pre-effect layer surface into into it.
			using (var ctx = new Cairo.Context (surface)) {					
				ctx.Operator = Cairo.Operator.Clear;
				ctx.Paint ();
				
				ctx.Operator = Cairo.Operator.Over;
				ctx.SetSourceSurface (layer.Surface, (int) layer.Offset.X, (int) layer.Offset.Y);
				ctx.Paint ();
			}
			
			effect.EffectData.PropertyChanged += EffectData_PropertyChanged;
			
			if (Started != null) {
				var args = new LivePreviewStartedEventArgs(layer,
				                                           effect,
				                                           surface);				                                                 
				Started (this, args);
			}			
			
			StartRender ();
			
			if (effect.IsConfigurable) {		
				if (!effect.LaunchConfiguration ())
					Cancel ();
				else
					Apply ();
			}
		}
		
		// Method asks render task to complete, and then returns immediately. The cancel 
		// is not actually complete until the LivePreviewRenderCompleted event is fired.
		void Cancel ()
		{
			Debug.WriteLine ("LivePreviewManager.Cancel()");
			
			cancel_live_preview_flag = true;
			cancel_render_flag = true;
			restart_render_flag = false;
			
			// Show a busy cursor, and make the main window insensitive,
			// until the cancel has completed.
			PintaCore.Chrome.MainWindowBusy = true;
			
			if (AllThreadsAreStopped ())
				HandleCancel ();
		}
		
		void HandleCancel ()
		{
			Debug.WriteLine ("LivePreviewManager.HandleCancel()");
			
			FireLivePreviewEndedEvent(RenderStatus.Cancelled, null);
			live_preview_enabled = false;
			
			if (surface != null) {
				var disposable = surface as IDisposable;
					disposable.Dispose ();
			}
			
			PintaCore.Workspace.Invalidate ();
			CleanUp ();
		}
		
		void Apply ()
		{
			Debug.WriteLine ("LivePreviewManager.Apply()");
			apply_live_preview_flag = true;
			
			if (AllThreadsAreStopped ()) {
				HandleApply ();
			} else  {
				var dialog = PintaCore.Chrome.ProgressDialog;
				dialog.Title = "Render effect progress";
				dialog.Text = effect.Text;
				dialog.Canceled += HandleProgressDialogCancel;
				RenderUpdated += UpdateProgressDialog;
				dialog.Show ();				
			}
		}
		
		void HandleProgressDialogCancel (object o, EventArgs e)
		{
			Cancel();
		}
		
		void UpdateProgressDialog (object o, LivePreviewRenderUpdatedEventArgs e)
		{
			PintaCore.Chrome.ProgressDialog.Progress = e.Progress;
		}
		
		void HandleApply ()
		{
			Debug.WriteLine ("LivePreviewManager.HandleApply()");

			var item = new SimpleHistoryItem (effect.Icon, effect.Text);
			item.TakeSnapshotOfLayer (PintaCore.Layers.CurrentLayerIndex);			
			
			using (var ctx = new Cairo.Context (layer.Surface)) {
				ctx.AppendPath (PintaCore.Layers.SelectionPath);
				ctx.FillRule = Cairo.FillRule.EvenOdd;
				ctx.Clip ();

				ctx.SetSource (surface);
				ctx.Paint ();
			}
			
			PintaCore.History.PushNewItem (item);
			
			FireLivePreviewEndedEvent(RenderStatus.Completed, null);
			
			live_preview_enabled = false;
			PintaCore.Workspace.Invalidate (); //TODO keep track of dirty bounds.
			CleanUp ();
		}
		
		// Clean up resources when live preview is disabled.
		void CleanUp ()
		{
			live_preview_enabled = false;
			
			if (effect != null) {
				effect.EffectData.PropertyChanged -= EffectData_PropertyChanged;
				effect = null;
				effect_data = null;
			}
							
			surface = null;
			
			// Hide progress dialog and clean up events.
			var dialog = PintaCore.Chrome.ProgressDialog;
			dialog.Hide ();
			dialog.Canceled -= HandleProgressDialogCancel;
			
			RenderUpdated += UpdateProgressDialog;
			
			PintaCore.Chrome.MainWindowBusy = false;
		}
		
		void EffectData_PropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			//TODO calculate bounds.						
			StartRender ();
		}		
		
		void StartRender ()
		{							
			if (!live_preview_enabled || cancel_live_preview_flag)
				return;
			
			// If a render is already in progress, then cancel it,
			// and start a new render.
			if (!AllThreadsAreStopped ()) {
				cancel_render_flag = true;
				restart_render_flag = true;
				return;
			}
			
			cancel_render_flag = false;
			restart_render_flag = false;
			
			render_id++;
			rendered_tiles = 0;			
			total_tiles = (int)(Math.Ceiling((float)render_bounds.Width / (float)render_tile_width)
                                * Math.Ceiling((float)render_bounds.Height / (float)render_tile_height));
			
			for (int i=0; i < total_tiles; i++)
				GetTileBounds (i);
			
			effect_data = effect.EffectData.Clone ();				
			
			render_exceptions.Clear ();
			
			Debug.WriteLine ("StartRender() Render " + render_id + " starting.");
			
			for (int i = 0; i < render_thread_count; i++)
				StartRenderThread (i);
		}	
		
		Thread StartRenderThread (int threadIndex)
		{
			var thread = new Thread(() => {
				Interlocked.Increment(ref running_render_threads);
				
				Debug.WriteLine ("LivePreviewManager render thread started. " + threadIndex);
				
				for (int tileIndex = threadIndex; tileIndex < total_tiles; tileIndex += render_thread_count) {					
					if (cancel_render_flag) {
						Debug.WriteLine ("LivePreviewManager render thread cancelled. "  + threadIndex);
						break;
					}
					
					RenderTile (render_id, tileIndex);
				}
				
				Debug.WriteLine ("LivePreviewManager render thread ended. "  + threadIndex);
				
				Interlocked.Decrement(ref running_render_threads);
				
				CheckRenderCompletion (render_id, threadIndex, cancel_render_flag);
			});
			
			thread.Start ();
			
			//TODO Perhaps set thread priority below that of the UI thread, to make sure
			// that the UI remains responsive on single core computers.
			
			return thread;
		}
		
		// Can be called on any thread.
		Gdk.Rectangle GetTileBounds (int tileIndex)
		{
			int horizTileCount = (int)Math.Ceiling((float)render_bounds.Width 
			                                       / (float)render_tile_width);
			
            int x = ((tileIndex % horizTileCount) * render_tile_width) + render_bounds.X;
            int y = ((tileIndex / horizTileCount) * render_tile_height) + render_bounds.Y;
            int w = Math.Min(render_tile_width, render_bounds.Right - x);
            int h = Math.Min(render_tile_height, render_bounds.Bottom - y);
			
			return new Gdk.Rectangle (x, y, w, h);			
		}
		
		// Can be called on any thread.
		void RenderTile (int renderId, int tileIndex)
		{			
			Exception exception = null;
			Gdk.Rectangle bounds = new Gdk.Rectangle ();
			
			try {
				
				bounds = GetTileBounds (tileIndex);
				
				if (!cancel_render_flag)
					effect.RenderEffect (layer.Surface, surface, new [] { bounds }, effect_data);
				
			} catch (Exception ex) {		
				exception = ex;
				//cancel_render_flag = true; //TODO could cancel render on first error detected.
				Debug.WriteLine ("LivePreview Error: " + ex.Message + "\n" + ex.StackTrace);
			}
			
			// When this is running multithread, we need to marshall this code back onto the
			// UI thread.
			Gtk.Application.Invoke ((o, e) => {				
				HandleTileCompletion (renderId, tileIndex, bounds, cancel_render_flag, exception);
			});			
		}
		
		void HandleTileCompletion (int renderId,
		                           int tileIndex,
		                           Gdk.Rectangle bounds,
		                           bool cancelled,
		                           Exception exception)
		{
			if (renderId != render_id)
				return;
			
			rendered_tiles++;
					
			float progress = (float)rendered_tiles / (float)total_tiles;
			
			if (!cancelled && exception == null && RenderUpdated != null) {
				Debug.Write ("*");
				var args = new LivePreviewRenderUpdatedEventArgs(progress, bounds);
				RenderUpdated (this, args);
			}
			
			if (exception != null) {
				lock (render_exceptions) {
					render_exceptions.Add (exception);
				}
			}
		}
		
		void CheckRenderCompletion (int renderId, int threadId, bool cancelled)
		{			
			Gtk.Application.Invoke ((o,e) => {
				if (AllThreadsAreStopped())
					HandleRenderCompletion (render_id, cancel_render_flag, render_exceptions.ToArray());
			});
		}
		
		bool AllThreadsAreStopped ()
		{
			return running_render_threads == 0;
		}		
		
		void HandleRenderCompletion (int renderId,
		                             bool cancelled,
		                             Exception[] exceptions)
		{
			Debug.WriteLine ("HandleRenderCompletion() renderId: " + renderId + " cancelled: " + cancelled);
			
			if (running_render_threads > 0)
				throw new ApplicationException ("HandleRenderCompletion() called while render threads are running.");
			
			if (!live_preview_enabled)
				return;
			
			if (cancel_live_preview_flag)
				HandleCancel ();
			else if (apply_live_preview_flag)
				HandleApply ();
			else if (restart_render_flag)
				StartRender ();
		}
		
		void FireLivePreviewEndedEvent (RenderStatus status, Exception ex)
		{
			if (Ended != null) {
				var args = new LivePreviewEndedEventArgs (status, ex);
				Ended (this, args);
			}			
		}		
	}
}
