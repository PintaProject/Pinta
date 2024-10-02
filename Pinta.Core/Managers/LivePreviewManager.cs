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
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Debug = System.Diagnostics.Debug;

namespace Pinta.Core;

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

	private readonly WorkspaceManager workspace_manager;
	private readonly ToolManager tool_manager;
	private readonly SystemManager system_manager;
	private readonly ChromeManager chrome_manager;
	internal LivePreviewManager (
		WorkspaceManager workspaceManager,
		ToolManager toolManager,
		SystemManager systemManager,
		ChromeManager chromeManager)
	{
		live_preview_enabled = false;

		RenderUpdated += LivePreview_RenderUpdated;

		workspace_manager = workspaceManager;
		tool_manager = toolManager;
		system_manager = systemManager;
		chrome_manager = chromeManager;
	}

	public bool IsEnabled => live_preview_enabled;
	public Cairo.ImageSurface LivePreviewSurface => live_preview_surface;
	public RectangleI RenderBounds => render_bounds;

	public event EventHandler<LivePreviewStartedEventArgs>? Started;
	public event EventHandler<LivePreviewRenderUpdatedEventArgs>? RenderUpdated;
	public event EventHandler<LivePreviewEndedEventArgs>? Ended;

	public async void Start (BaseEffect effect)
	{
		if (live_preview_enabled)
			throw new InvalidOperationException ("LivePreviewManager.Start() called while live preview is already enabled.");

		// Create live preview surface.
		// Start rendering.
		// Listen for changes to effectConfiguration object, and restart render if needed.

		var doc = workspace_manager.ActiveDocument;

		live_preview_enabled = true;
		apply_live_preview_flag = false;
		cancel_live_preview_flag = false;

		layer = doc.Layers.CurrentUserLayer;
		this.effect = effect;

		//TODO Use the current tool layer instead.
		live_preview_surface = CairoExtensions.CreateImageSurface (
			Cairo.Format.Argb32,
			workspace_manager.ImageSize.Width,
			workspace_manager.ImageSize.Height);

		// Handle selection path.
		tool_manager.Commit ();
		var selection = doc.Selection;
		selection_path = (selection.Visible) ? selection.SelectionPath : null;
		render_bounds = (selection_path != null) ? selection_path.GetBounds () : live_preview_surface.GetBounds ();
		render_bounds = workspace_manager.ClampToImageSize (render_bounds);

		history_item = new SimpleHistoryItem (effect.Icon, effect.Name);
		history_item.TakeSnapshotOfLayer (doc.Layers.CurrentUserLayerIndex);

		// Paint the pre-effect layer surface into into the working surface.
		Cairo.Context ctx = new (live_preview_surface);
		layer.Draw (ctx, layer.Surface, 1);

		if (effect.EffectData != null)
			effect.EffectData.PropertyChanged += EffectData_PropertyChanged;

		Started?.Invoke (this, new LivePreviewStartedEventArgs ());

		AsyncEffectRenderer.Settings settings = new (
			threadCount: system_manager.RenderThreads,
			renderBounds: render_bounds,
			effectIsTileable: effect.IsTileable,
			updateMilliseconds: 100,
			threadPriority: ThreadPriority.BelowNormal);

		Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "Start Live preview.");

		renderer = new Renderer (this, settings, chrome_manager);
		renderer.Start (effect, layer.Surface, live_preview_surface);

		if (effect.IsConfigurable) {

			Gtk.ResponseType response = await effect.LaunchConfiguration ();
			chrome_manager.MainWindowBusy = true;
			if (response == Gtk.ResponseType.Ok)
				Apply ();
			else
				Cancel ();

		} else {
			chrome_manager.MainWindowBusy = true;
			Apply ();
		}
	}

	// Method asks render task to complete, and then returns immediately. The cancel
	// is not actually complete until the LivePreviewRenderCompleted event is fired.
	void Cancel ()
	{
		Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.Cancel()");

		cancel_live_preview_flag = true;

		renderer?.Cancel ();

		// Show a busy cursor, and make the main window insensitive,
		// until the cancel has completed.
		chrome_manager.MainWindowBusy = true;

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

		workspace_manager.Invalidate ();
		CleanUp ();
	}

	void Apply ()
	{
		Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "LivePreviewManager.Apply()");
		apply_live_preview_flag = true;

		if (!renderer.IsRendering) {
			HandleApply ();
		} else {
			var dialog = chrome_manager.ProgressDialog;
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
		workspace_manager.ActiveDocument.Selection.Clip (ctx);

		layer.DrawWithOperator (ctx, live_preview_surface, Cairo.Operator.Source);
		ctx.Restore ();

		workspace_manager.ActiveDocument.History.PushNewItem (history_item);
		history_item = null!;

		FireLivePreviewEndedEvent (RenderStatus.Completed, null);

		live_preview_enabled = false;

		workspace_manager.Invalidate (); //TODO keep track of dirty bounds.
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
		var dialog = chrome_manager.ProgressDialog;
		dialog.Hide ();
		dialog.Canceled -= HandleProgressDialogCancel;

		chrome_manager.MainWindowBusy = false;
	}

	void EffectData_PropertyChanged (object? sender, PropertyChangedEventArgs e)
	{
		//TODO calculate bounds.
		renderer.Start (effect, layer.Surface, live_preview_surface);
	}

	private sealed class Renderer : AsyncEffectRenderer
	{
		readonly LivePreviewManager manager;
		readonly ChromeManager chrome;
		internal Renderer (
			LivePreviewManager manager,
			AsyncEffectRenderer.Settings settings,
			ChromeManager chrome)
			: base (settings)
		{
			this.manager = manager;
			this.chrome = chrome;
		}

		protected override void OnUpdate (
			double progress,
			RectangleI updatedBounds)
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnUpdate() progress: " + progress);
			chrome.ProgressDialog.Progress = progress;
			manager.FireLivePreviewRenderUpdatedEvent (progress, updatedBounds);
		}

		protected override void OnCompletion (
			IReadOnlyList<Exception> exceptions,
			CancellationToken cancellationToken)
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnCompletion() cancelled: " + cancellationToken.IsCancellationRequested);

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
		if (Ended == null) return;
		LivePreviewEndedEventArgs args = new (status, ex);
		Ended (this, args);
	}

	void FireLivePreviewRenderUpdatedEvent (double progress, RectangleI bounds)
	{

		RenderUpdated?.Invoke (this, new LivePreviewRenderUpdatedEventArgs (progress, bounds));
	}

	private void LivePreview_RenderUpdated (object? o, LivePreviewRenderUpdatedEventArgs args)
	{
		double scale = workspace_manager.Scale;
		var offset = workspace_manager.Offset;

		var bounds = args.Bounds;

		// Transform bounds (Image -> Canvas -> Window)

		// Calculate canvas bounds.
		PointD bounds1 = new (
			X: bounds.Left * scale,
			Y: bounds.Top * scale);

		PointD bounds2 = new (
			X: (bounds.Right + 1) * scale,
			Y: (bounds.Bottom + 1) * scale);

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
			Height: (int) Math.Ceiling (bounds2.Y) - y);

		// Tell GTK to expose the drawing area.
		workspace_manager.ActiveWorkspace.InvalidateWindowRect (areaToInvalidate);
	}
}
