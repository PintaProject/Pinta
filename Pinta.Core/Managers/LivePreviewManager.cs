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

namespace Pinta.Core;

public sealed class LivePreviewManager
{
	internal LivePreviewManager () { }

	// NRT - These are set in Start(). This should be rewritten to be provably non-null.
	public bool IsEnabled { get; private set; } = false; // Referring to live preview
	public Cairo.ImageSurface LivePreviewSurface { get; private set; } = null!;
	public RectangleI RenderBounds { get; private set; }

	public event EventHandler<LivePreviewStartedEventArgs>? Started;
	public event EventHandler<LivePreviewRenderUpdatedEventArgs>? RenderUpdated;
	public event EventHandler<LivePreviewEndedEventArgs>? Ended;

	private void OnStarted (LivePreviewStartedEventArgs args)
	{
		Started?.Invoke (this, args);
	}

	private void OnRenderUpdated (LivePreviewRenderUpdatedEventArgs args)
	{
		LivePreview_RenderUpdated (args);
		RenderUpdated?.Invoke (this, args);
	}

	private void OnEnded (LivePreviewEndedEventArgs args)
	{
		Ended?.Invoke (this, args);
	}

	public void Start (BaseEffect effect)
	{
		if (IsEnabled)
			throw new InvalidOperationException ("LivePreviewManager.Start() called while live preview is already enabled.");

		// Create live preview surface.
		// Start rendering.
		// Listen for changes to effectConfiguration object, and restart render if needed.

		var doc = PintaCore.Workspace.ActiveDocument;

		IsEnabled = true;

		bool apply_live_preview_flag = false;
		bool cancel_live_preview_flag = false;

		AsyncEffectRenderer renderer = null!;

		Layer layer = doc.Layers.CurrentUserLayer;

		//TODO Use the current tool layer instead.
		LivePreviewSurface = CairoExtensions.CreateImageSurface (
			Cairo.Format.Argb32,
			PintaCore.Workspace.ImageSize.Width,
			PintaCore.Workspace.ImageSize.Height
		);

		// Handle selection path.
		PintaCore.Tools.Commit ();
		var selection = doc.Selection;
		Cairo.Path? selection_path = (selection.Visible) ? selection.SelectionPath : null;
		RenderBounds = (selection_path != null) ? selection_path.GetBounds () : LivePreviewSurface.GetBounds ();
		RenderBounds = PintaCore.Workspace.ClampToImageSize (RenderBounds);

		SimpleHistoryItem history_item = new SimpleHistoryItem (effect.Icon, effect.Name);
		history_item.TakeSnapshotOfLayer (doc.Layers.CurrentUserLayerIndex);

		// Paint the pre-effect layer surface into into the working surface.
		var ctx = new Cairo.Context (LivePreviewSurface);
		layer.Draw (ctx, layer.Surface, 1);

		if (effect.EffectData != null)
			effect.EffectData.PropertyChanged += EffectData_PropertyChanged;

		OnStarted (new LivePreviewStartedEventArgs ());

		var settings = new AsyncEffectRenderer.Settings () {
			ThreadCount = PintaCore.System.RenderThreads,
			TileWidth = RenderBounds.Width,
			TileHeight = 1,
			ThreadPriority = ThreadPriority.BelowNormal
		};

		Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "Start Live preview.");

		var source = layer.Surface;
		var dest = LivePreviewSurface;

		renderer = new Renderer (settings, OnUpdate, OnCompletion);
		renderer.Start (effect, source, dest, RenderBounds);

		LaunchConfig ();

		// Method asks render task to complete, and then returns immediately. The cancel
		// is not actually complete until the LivePreviewRenderCompleted event is fired.
		void Cancel (
			Cairo.ImageSurface sourceSurface,
			Cairo.ImageSurface destSurface)
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.Cancel()");

			cancel_live_preview_flag = true;

			renderer?.Cancel (sourceSurface, destSurface, RenderBounds);

			// Show a busy cursor, and make the main window insensitive,
			// until the cancel has completed.
			PintaCore.Chrome.MainWindowBusy = true;

			if (renderer == null || !renderer.IsRendering)
				HandleCancel ();
		}

		void HandleProgressDialogCancel (object? o, EventArgs? e)
		{
			Cancel (layer.Surface, LivePreviewSurface);
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

		// Clean up resources when live preview is disabled.
		void CleanUp ()
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.CleanUp()");

			IsEnabled = false;

			if (effect.EffectData != null)
				effect.EffectData.PropertyChanged -= EffectData_PropertyChanged;

			LivePreviewSurface = null!;

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

		// Called from asynchronously from Renderer.OnCompletion ()
		void HandleCancel ()
		{
			Debug.WriteLine ("LivePreviewManager.HandleCancel()");

			FireLivePreviewEndedEvent (RenderStatus.Canceled, null);
			IsEnabled = false;

			LivePreviewSurface = null!;

			PintaCore.Workspace.Invalidate ();
			CleanUp ();
		}

		// Called from asynchronously from Renderer.OnCompletion ()
		void HandleApply ()
		{
			Debug.WriteLine ("LivePreviewManager.HandleApply()");

			var ctx = new Cairo.Context (layer.Surface);
			ctx.Save ();
			PintaCore.Workspace.ActiveDocument.Selection.Clip (ctx);

			layer.DrawWithOperator (ctx, LivePreviewSurface, Cairo.Operator.Source);
			ctx.Restore ();

			PintaCore.Workspace.ActiveDocument.History.PushNewItem (history_item);
			history_item = null!;

			FireLivePreviewEndedEvent (RenderStatus.Completed, null);

			IsEnabled = false;

			PintaCore.Workspace.Invalidate (); //TODO keep track of dirty bounds.
			CleanUp ();
		}

		void EffectData_PropertyChanged (object? sender, PropertyChangedEventArgs e)
		{
			//TODO calculate bounds.
			renderer!.Start (effect, layer.Surface, LivePreviewSurface, RenderBounds);
		}

		void OnUpdate (double progress, RectangleI updatedBounds)
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnUpdate() progress: " + progress);
			PintaCore.Chrome.ProgressDialog.Progress = progress;
			FireLivePreviewRenderUpdatedEvent (progress, updatedBounds);
		}

		void OnCompletion (bool cancelled, Exception[] exceptions)
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnCompletion() cancelled: " + cancelled);

			if (!IsEnabled)
				return;

			if (cancel_live_preview_flag)
				HandleCancel ();
			else if (apply_live_preview_flag)
				HandleApply ();
		}

		void FireLivePreviewEndedEvent (RenderStatus status, Exception? ex)
		{
			OnEnded (new LivePreviewEndedEventArgs (status, ex));
		}

		void FireLivePreviewRenderUpdatedEvent (double progress, RectangleI bounds)
		{
			OnRenderUpdated (new LivePreviewRenderUpdatedEventArgs (progress, bounds));
		}

		void LaunchConfig ()
		{
			if (!effect.IsConfigurable) {
				PintaCore.Chrome.MainWindowBusy = true;
				Apply ();
				return;
			}

			EventHandler<BaseEffect.ConfigDialogResponseEventArgs>? handler = null;
			handler = (_, args) => {
				if (!args.Accepted) {
					PintaCore.Chrome.MainWindowBusy = true;
					Cancel (source, dest);
				} else {
					PintaCore.Chrome.MainWindowBusy = true;
					Apply ();
				}

				// Unsubscribe once we're done.
				effect.ConfigDialogResponse -= handler;
			};

			effect.ConfigDialogResponse += handler;
			effect.LaunchConfiguration ();
		}
	}

	private sealed class Renderer : AsyncEffectRenderer
	{
		private readonly Action<double, RectangleI> on_update;
		private readonly Action<bool, Exception[]> on_completion;

		internal Renderer (
			Settings settings,
			Action<double, RectangleI> onUpdate,
			Action<bool, Exception[]> onCompletion
		) : base (settings)
		{
			on_update = onUpdate;
			on_completion = onCompletion;
		}

		protected override void OnUpdate (double progress, RectangleI updatedBounds)
		{
			on_update (progress, updatedBounds);
		}

		protected override void OnCompletion (bool canceled, Exception[] exceptions)
		{
			on_completion (canceled, exceptions);
		}
	}

	void LivePreview_RenderUpdated (LivePreviewRenderUpdatedEventArgs args)
	{
		double scale = PintaCore.Workspace.Scale;
		var offset = PintaCore.Workspace.Offset;

		var bounds = args.Bounds;

		// Transform bounds (Image -> Canvas -> Window)

		// Calculate canvas bounds.
		PointD bounds1 = new (
			X: bounds.Left * scale,
			Y: bounds.Top * scale
		);
		PointD bounds2 = new (
			X: (bounds.Right + 1) * scale,
			Y: (bounds.Bottom + 1) * scale
		);

		// TODO Figure out why when scale > 1 that I need add on an
		// extra pixel of padding.
		// I must being doing something wrong here.
		if (scale > 1.0) {
			//x1 = (bounds.Left-1) * scale;
			bounds1 = bounds1 with { Y = (bounds.Top - 1) * scale };
			//x2 = (bounds.Right+1) * scale;
			//y2 = (bounds.Bottom+1) * scale;
		}

		// Calculate window bounds.
		bounds1 += offset;
		bounds2 += offset;

		// Convert to integer, carefully not to miss partially covered
		// pixels by rounding incorrectly.
		int x = (int) Math.Floor (bounds1.X);
		int y = (int) Math.Floor (bounds1.Y);
		RectangleI areaToInvalidate = new (
			X: x,
			Y: y,
			Width: (int) Math.Ceiling (bounds2.X) - x,
			Height: (int) Math.Ceiling (bounds2.Y) - y
		);

		// Tell GTK to expose the drawing area.
		PintaCore.Workspace.ActiveWorkspace.InvalidateWindowRect (areaToInvalidate);
	}
}
