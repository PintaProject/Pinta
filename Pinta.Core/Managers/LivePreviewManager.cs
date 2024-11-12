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

public interface ILivePreview
{
	RectangleI RenderBounds { get; }
}

public sealed class LivePreviewManager : ILivePreview
{
	bool apply_live_preview_flag;
	bool cancel_live_preview_flag;

	private readonly WorkspaceManager workspace;
	private readonly ToolManager tools;
	private readonly SystemManager system;
	private readonly ChromeManager chrome;
	internal LivePreviewManager (
		WorkspaceManager workspaceManager,
		ToolManager toolManager,
		SystemManager systemManager,
		ChromeManager chromeManager)
	{
		IsEnabled = false;

		workspace = workspaceManager;
		tools = toolManager;
		system = systemManager;
		chrome = chromeManager;
	}

	public Cairo.ImageSurface LivePreviewSurface { get; private set; } = null!;
	public RectangleI RenderBounds { get; private set; }
	public bool IsEnabled { get; private set; }

	public async void Start (BaseEffect effect)
	{
		if (IsEnabled)
			throw new InvalidOperationException ("LivePreviewManager.Start() called while live preview is already enabled.");

		// Create live preview surface.
		// Start rendering.
		// Listen for changes to effectConfiguration object, and restart render if needed.

		Document doc = workspace.ActiveDocument;

		IsEnabled = true;
		apply_live_preview_flag = false;
		cancel_live_preview_flag = false;

		Layer layer = doc.Layers.CurrentUserLayer;

		//TODO Use the current tool layer instead.
		LivePreviewSurface = CairoExtensions.CreateImageSurface (
			Cairo.Format.Argb32,
			workspace.ImageSize.Width,
			workspace.ImageSize.Height);

		// Handle selection path.
		tools.Commit ();

		DocumentSelection selection = doc.Selection;

		Cairo.Path? selectionPath = selection.Visible ? selection.SelectionPath : null;
		RenderBounds = (selectionPath != null) ? selectionPath.GetBounds () : LivePreviewSurface.GetBounds ();
		RenderBounds = workspace.ClampToImageSize (RenderBounds);

		SimpleHistoryItem historyItem = new (effect.Icon, effect.Name);
		historyItem.TakeSnapshotOfLayer (doc.Layers.CurrentUserLayerIndex);

		// Paint the pre-effect layer surface into into the working surface.
		using Cairo.Context ctx = new (LivePreviewSurface);
		layer.Draw (ctx, layer.Surface, 1);

		AsyncEffectRenderer.Settings settings = new (
			threadCount: system.RenderThreads,
			renderBounds: RenderBounds,
			effectIsTileable: effect.IsTileable,
			updateMilliseconds: 100,
			threadPriority: ThreadPriority.BelowNormal);

		Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "Start Live preview.");

		SemaphoreSlim restartSemaphore = new (1, 1);
		int handlersInQueue = 0;
		AsyncEffectRenderer renderer = new (settings);
		renderer.Updated += OnUpdate;
		renderer.Completed += OnCompletion;

		if (effect.EffectData != null)
			effect.EffectData.PropertyChanged += EffectData_PropertyChanged;

		renderer.Start (effect, layer.Surface, LivePreviewSurface);

		if (effect.IsConfigurable) {

			bool response = await effect.LaunchConfiguration ();
			chrome.MainWindowBusy = true;
			if (response)
				Apply ();
			else
				Cancel ();

		} else {
			chrome.MainWindowBusy = true;
			Apply ();
		}

		// === Methods ===

		// Method asks render task to complete, and then returns immediately. The cancel
		// is not actually complete until the LivePreviewRenderCompleted event is fired.
		void Cancel ()
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.Cancel()");

			cancel_live_preview_flag = true;

			renderer.Cancel ();

			// Show a busy cursor, and make the main window insensitive,
			// until the cancel has completed.
			chrome.MainWindowBusy = true;

			if (!renderer.IsRendering)
				HandleCancel ();
		}

		// Called from asynchronously from Renderer.OnCompletion ()
		void HandleCancel ()
		{
			Debug.WriteLine ("LivePreviewManager.HandleCancel()");

			IsEnabled = false;

			LivePreviewSurface = null!;

			workspace.Invalidate ();

			CleanUp ();
		}

		void Apply ()
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "LivePreviewManager.Apply()");

			apply_live_preview_flag = true;

			if (!renderer.IsRendering) {
				HandleApply ();
			} else {
				IProgressDialog dialog = chrome.ProgressDialog;
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

			using Cairo.Context ctx = new (layer.Surface);
			ctx.Save ();
			workspace.ActiveDocument.Selection.Clip (ctx);

			layer.DrawWithOperator (ctx, LivePreviewSurface, Cairo.Operator.Source);
			ctx.Restore ();

			workspace.ActiveDocument.History.PushNewItem (historyItem);

			IsEnabled = false;

			workspace.Invalidate (); //TODO keep track of dirty bounds.
			CleanUp ();
		}

		// Clean up resources when live preview is disabled.
		void CleanUp ()
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.CleanUp()");

			IsEnabled = false;

			if (effect.EffectData != null)
				effect.EffectData.PropertyChanged -= EffectData_PropertyChanged;

			LivePreviewSurface = null!;

			renderer.Dispose ();

			// Hide progress dialog and clean up events.
			IProgressDialog dialog = chrome.ProgressDialog;
			dialog.Hide ();
			dialog.Canceled -= HandleProgressDialogCancel;

			chrome.MainWindowBusy = false;
		}

		async void EffectData_PropertyChanged (object? sender, PropertyChangedEventArgs e)
		{
			Interlocked.Increment (ref handlersInQueue);
			await restartSemaphore.WaitAsync ();
			try {
				// TODO: calculate bounds
				Interlocked.Decrement (ref handlersInQueue);
				if (handlersInQueue > 0) return;
				await renderer.Cancel ();
				renderer.Start (effect, layer.Surface, LivePreviewSurface);
			} finally {
				restartSemaphore.Release ();
			}
		}

		void OnUpdate (
			double progress,
			RectangleI updatedBounds)
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnUpdate() progress: " + progress);
			chrome.ProgressDialog.Progress = progress;
			HandleUpdate (updatedBounds);
		}

		void OnCompletion (
			IReadOnlyList<Exception> exceptions,
			CancellationToken cancellationToken)
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnCompletion() cancelled: " + cancellationToken.IsCancellationRequested);

			if (!IsEnabled)
				return;

			if (cancel_live_preview_flag)
				HandleCancel ();
			else if (apply_live_preview_flag)
				HandleApply ();
		}

		void HandleUpdate (RectangleI bounds)
		{
			double scale = workspace.Scale;
			PointD offset = workspace.Offset;

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
			workspace.ActiveWorkspace.InvalidateWindowRect (areaToInvalidate);
		}
	}
}
