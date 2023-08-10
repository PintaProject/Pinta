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

#if (!LIVE_PREVIEW_DEBUG && DEBUG)
#undef DEBUG
#endif

using System;
using System.ComponentModel;
using System.Threading;
using Debug = System.Diagnostics.Debug;

namespace Pinta.Core
{

	public sealed class LivePreviewManager
	{
		// NRT - These are set in Start(). This should be rewritten to be provably non-null.
		bool live_preview_enabled;
		Layer layer = null!;
		BaseEffect effect = null!;
		Cairo.Path? selection_path;

		bool apply_live_preview_flag;
		bool cancel_live_preview_flag;

		Cairo.ImageSurface live_preview_surface = null!;
		RectangleI render_bounds;
		SimpleHistoryItem history_item = null!;

		AsyncEffectRenderer renderer = null!;

		internal LivePreviewManager ()
		{
			live_preview_enabled = false;

			RenderUpdated += LivePreview_RenderUpdated;
		}

		public bool IsEnabled => live_preview_enabled;
		public Cairo.ImageSurface LivePreviewSurface => live_preview_surface;
		public RectangleI RenderBounds => render_bounds;

		public event EventHandler<LivePreviewStartedEventArgs>? Started;
		public event EventHandler<LivePreviewRenderUpdatedEventArgs>? RenderUpdated;
		public event EventHandler<LivePreviewEndedEventArgs>? Ended;

		public void Start (BaseEffect effect)
		{
			if (live_preview_enabled)
				throw new InvalidOperationException ("LivePreviewManager.Start() called while live preview is already enabled.");

			// Create live preview surface.
			// Start rendering.
			// Listen for changes to effectConfiguration object, and restart render if needed.

			var doc = PintaCore.Workspace.ActiveDocument;

			live_preview_enabled = true;
			apply_live_preview_flag = false;
			cancel_live_preview_flag = false;

			layer = doc.Layers.CurrentUserLayer;
			this.effect = effect;

			//TODO Use the current tool layer instead.
			live_preview_surface = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32,
											  PintaCore.Workspace.ImageSize.Width,
											  PintaCore.Workspace.ImageSize.Height);

			// Handle selection path.
			PintaCore.Tools.Commit ();
			var selection = doc.Selection;
			selection_path = (selection.Visible) ? selection.SelectionPath : null;
			render_bounds = (selection_path != null) ? selection_path.GetBounds () : live_preview_surface.GetBounds ();
			render_bounds = PintaCore.Workspace.ClampToImageSize (render_bounds);

			history_item = new SimpleHistoryItem (effect.Icon, effect.Name);
			history_item.TakeSnapshotOfLayer (doc.Layers.CurrentUserLayerIndex);

			// Paint the pre-effect layer surface into into the working surface.
			var ctx = new Cairo.Context (live_preview_surface);
			layer.Draw (ctx, layer.Surface, 1);

			if (effect.EffectData != null)
				effect.EffectData.PropertyChanged += EffectData_PropertyChanged;

			if (Started != null) {
				Started (this, new LivePreviewStartedEventArgs ());
			}

			var settings = new AsyncEffectRenderer.Settings () {
				ThreadCount = PintaCore.System.RenderThreads,
				TileWidth = render_bounds.Width,
				TileHeight = 1,
				ThreadPriority = ThreadPriority.BelowNormal
			};

			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "Start Live preview.");

			renderer = new Renderer (this, settings);
			renderer.Start (effect, layer.Surface, live_preview_surface, render_bounds);

			if (effect.IsConfigurable) {
				EventHandler<BaseEffect.ConfigDialogResponseEventArgs>? handler = null;
				handler = (_, args) => {
					if (!args.Accepted) {
						PintaCore.Chrome.MainWindowBusy = true;
						Cancel ();
					} else {
						PintaCore.Chrome.MainWindowBusy = true;
						Apply ();
					}

					// Unsubscribe once we're done.
					effect.ConfigDialogResponse -= handler;
				};

				effect.ConfigDialogResponse += handler;

				effect.LaunchConfiguration ();

			} else {
				PintaCore.Chrome.MainWindowBusy = true;
				Apply ();
			}
		}

		// Used by the workspace drawing area expose render loop.
		// Takes care of the clipping.
		public void RenderLivePreviewLayer (Cairo.Context ctx, double opacity)
		{
			if (!IsEnabled)
				throw new InvalidOperationException ("Tried to render a live preview after live preview has ended.");

			// TODO remove seam around selection during live preview.

			ctx.Save ();
			if (selection_path != null) {

				// Paint area outsize of the selection path, with the pre-effect image.
				var imageSize = PintaCore.Workspace.ImageSize;
				ctx.Rectangle (0, 0, imageSize.Width, imageSize.Height);
				ctx.AppendPath (selection_path);
				ctx.Clip ();
				layer.Draw (ctx, layer.Surface, opacity);
				ctx.ResetClip ();

				// Paint area inside the selection path, with the post-effect image.
				ctx.AppendPath (selection_path);
				ctx.Clip ();

				layer.Draw (ctx, live_preview_surface, opacity);
				ctx.PaintWithAlpha (opacity);

				ctx.AppendPath (selection_path);
				ctx.FillRule = Cairo.FillRule.EvenOdd;
				ctx.Clip ();
			} else {

				layer.Draw (ctx, live_preview_surface, opacity);
			}
			ctx.Restore ();
		}

		// Method asks render task to complete, and then returns immediately. The cancel 
		// is not actually complete until the LivePreviewRenderCompleted event is fired.
		void Cancel ()
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.Cancel()");

			cancel_live_preview_flag = true;

			if (renderer != null)
				renderer.Cancel ();

			// Show a busy cursor, and make the main window insensitive,
			// until the cancel has completed.
			PintaCore.Chrome.MainWindowBusy = true;

			if (renderer == null || !renderer.IsRendering)
				HandleCancel ();
		}

		// Called from asynchronously from Renderer.OnCompletion ()
		void HandleCancel ()
		{
			Debug.WriteLine ("LivePreviewManager.HandleCancel()");

			FireLivePreviewEndedEvent (RenderStatus.Canceled, null);
			live_preview_enabled = false;

			live_preview_surface = null!;

			PintaCore.Workspace.Invalidate ();
			CleanUp ();
		}

		void Apply ()
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "LivePreviewManager.Apply()");
			apply_live_preview_flag = true;

			if (!renderer.IsRendering) {
				HandleApply ();
			} else {
				var dialog = PintaCore.Chrome.ProgressDialog;
				dialog.Title = Translations.GetString ("Rendering Effect");
				dialog.Text = effect.Name;
				dialog.Progress = renderer.Progress;
				dialog.Canceled += HandleProgressDialogCancel;
				dialog.Show ();
			}
		}

		void HandleProgressDialogCancel (object? o, EventArgs e)
		{
			Cancel ();
		}

		// Called from asynchronously from Renderer.OnCompletion ()
		void HandleApply ()
		{
			Debug.WriteLine ("LivePreviewManager.HandleApply()");

			var ctx = new Cairo.Context (layer.Surface);
			ctx.Save ();
			PintaCore.Workspace.ActiveDocument.Selection.Clip (ctx);

			layer.DrawWithOperator (ctx, live_preview_surface, Cairo.Operator.Source);
			ctx.Restore ();

			PintaCore.Workspace.ActiveDocument.History.PushNewItem (history_item);
			history_item = null!;

			FireLivePreviewEndedEvent (RenderStatus.Completed, null);

			live_preview_enabled = false;

			PintaCore.Workspace.Invalidate (); //TODO keep track of dirty bounds.
			CleanUp ();
		}

		// Clean up resources when live preview is disabled.
		void CleanUp ()
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.CleanUp()");

			live_preview_enabled = false;

			if (effect != null) {
				if (effect.EffectData != null)
					effect.EffectData.PropertyChanged -= EffectData_PropertyChanged;
				effect = null!;
			}

			live_preview_surface = null!;

			if (renderer != null) {
				renderer.Dispose ();
				renderer = null!;
			}

			history_item = null!;

			// Hide progress dialog and clean up events.
			var dialog = PintaCore.Chrome.ProgressDialog;
			dialog.Hide ();
			dialog.Canceled -= HandleProgressDialogCancel;

			PintaCore.Chrome.MainWindowBusy = false;
		}

		void EffectData_PropertyChanged (object? sender, PropertyChangedEventArgs e)
		{
			//TODO calculate bounds.
			renderer.Start (effect, layer.Surface, live_preview_surface, render_bounds);
		}

		private sealed class Renderer : AsyncEffectRenderer
		{
			readonly LivePreviewManager manager;

			internal Renderer (LivePreviewManager manager, AsyncEffectRenderer.Settings settings)
				: base (settings)
			{
				this.manager = manager;
			}

			protected override void OnUpdate (double progress, RectangleI updatedBounds)
			{
				Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnUpdate() progress: " + progress);
				PintaCore.Chrome.ProgressDialog.Progress = progress;
				manager.FireLivePreviewRenderUpdatedEvent (progress, updatedBounds);
			}

			protected override void OnCompletion (bool cancelled, Exception[]? exceptions)
			{
				Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnCompletion() cancelled: " + cancelled);

				if (!manager.live_preview_enabled)
					return;

				if (manager.cancel_live_preview_flag)
					manager.HandleCancel ();
				else if (manager.apply_live_preview_flag)
					manager.HandleApply ();
			}
		}

		void FireLivePreviewEndedEvent (RenderStatus status, Exception? ex)
		{
			if (Ended != null) {
				var args = new LivePreviewEndedEventArgs (status, ex);
				Ended (this, args);
			}
		}

		void FireLivePreviewRenderUpdatedEvent (double progress, RectangleI bounds)
		{

			if (RenderUpdated != null) {
				RenderUpdated (this, new LivePreviewRenderUpdatedEventArgs (progress, bounds));
			}
		}

		private void LivePreview_RenderUpdated (object? o, LivePreviewRenderUpdatedEventArgs args)
		{
			double scale = PintaCore.Workspace.Scale;
			var offset = PintaCore.Workspace.Offset;

			var bounds = args.Bounds;

			// Transform bounds (Image -> Canvas -> Window)

			// Calculate canvas bounds.
			double x1 = bounds.Left * scale;
			double y1 = bounds.Top * scale;
			double x2 = (bounds.Right + 1) * scale;
			double y2 = (bounds.Bottom + 1) * scale;

			// TODO Figure out why when scale > 1 that I need add on an
			// extra pixel of padding.
			// I must being doing something wrong here.
			if (scale > 1.0) {
				//x1 = (bounds.Left-1) * scale;
				y1 = (bounds.Top - 1) * scale;
				//x2 = (bounds.Right+1) * scale;
				//y2 = (bounds.Bottom+1) * scale;
			}

			// Calculate window bounds.
			x1 += offset.X;
			y1 += offset.Y;
			x2 += offset.X;
			y2 += offset.Y;

			// Convert to integer, carefully not to miss partially covered
			// pixels by rounding incorrectly.
			int x = (int) Math.Floor (x1);
			int y = (int) Math.Floor (y1);
			int width = (int) Math.Ceiling (x2) - x;
			int height = (int) Math.Ceiling (y2) - y;

			// Tell GTK to expose the drawing area.			
			PintaCore.Workspace.ActiveWorkspace.InvalidateWindowRect (new RectangleI (x, y, width, height));
		}
	}
}
