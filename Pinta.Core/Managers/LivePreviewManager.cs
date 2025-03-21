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

public interface ILivePreview
{
	RectangleI RenderBounds { get; }
}

public sealed class LivePreviewManager : ILivePreview
{
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

		// Handle selection path.
		tools.Commit ();

		Document doc = workspace.ActiveDocument;

		DocumentSelection selection = doc.Selection;

		IsEnabled = true;

		//TODO Use the current tool layer instead.
		LivePreviewSurface = CairoExtensions.CreateImageSurface (
			Cairo.Format.Argb32,
			workspace.ImageSize.Width,
			workspace.ImageSize.Height);

		Cairo.Path? selectionPath = selection.Visible ? selection.SelectionPath : null;
		RenderBounds = (selectionPath != null) ? selectionPath.GetBounds () : LivePreviewSurface.GetBounds ();
		RenderBounds = workspace.ClampToImageSize (RenderBounds);

		AsyncEffectRenderer.Settings settings = new (
			threadCount: system.RenderThreads,
			renderBounds: RenderBounds,
			effectIsTileable: effect.IsTileable,
			updateMilliseconds: 100,
			threadPriority: ThreadPriority.BelowNormal);

		int handlersInQueue = 0;
		Layer layer = doc.Layers.CurrentUserLayer;

		string effectName = effect.Name;

		SimpleHistoryItem historyItem = new (effect.Icon, effect.Name);
		historyItem.TakeSnapshotOfLayer (doc.Layers.CurrentUserLayerIndex);

		using AsyncEffectRenderer renderer = new (settings);
		renderer.Updated += OnUpdate;

		IProgressDialog dialog = chrome.ProgressDialog;
		dialog.Title = Translations.GetString ("Rendering Effect");
		dialog.Text = effect.Name;
		dialog.Progress = renderer.Progress;
		dialog.Canceled += HandleProgressDialogCancel;

		try {
			// Paint the pre-effect layer surface into into the working surface.
			using Cairo.Context ctx = new (LivePreviewSurface);
			layer.Draw (ctx, layer.Surface, 1);

			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "Start Live preview.");

			if (effect.EffectData != null)
				effect.EffectData.PropertyChanged += EffectData_PropertyChanged;

			renderer.Start (
				effect,
				layer.Surface,
				LivePreviewSurface);

			bool userConfirmed = !effect.IsConfigurable || await effect.LaunchConfiguration ();

			chrome.MainWindowBusy = true;

			if (!userConfirmed) {
				Debug.WriteLine ("User decided not to proceed with the render");
				await renderer.Finish (cancel: true);
				return;
			}

			// The user confirmed, so show progress dialog

			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "LivePreviewManager.Apply()");

			dialog.Show ();

			var result = await renderer.Finish (cancel: false);

			foreach (var ex in result.Errors)
				Debug.WriteLine ("AsyncEffectRenderer Error while rendering effect: " + effectName + " exception: " + ex.Message + "\n" + ex.StackTrace);

			if (result.WasCanceled) {
				Debug.WriteLine ("User decided to cancel the render");
				await renderer.Finish (cancel: true);
				return;
			}

			// Was not canceled, so finally apply

			Debug.WriteLine ("Render completed without the user canceling");

			using Cairo.Context context = new (layer.Surface);

			context.Save ();
			workspace.ActiveDocument.Selection.Clip (context);

			layer.DrawWithOperator (context, LivePreviewSurface, Cairo.Operator.Source);
			context.Restore ();

			workspace.ActiveDocument.History.PushNewItem (historyItem);

		} finally {

			IsEnabled = false;
			LivePreviewSurface = null!;
			workspace.Invalidate ();

			if (effect.EffectData != null)
				effect.EffectData.PropertyChanged -= EffectData_PropertyChanged;

			chrome.MainWindowBusy = false;

			dialog.Canceled -= HandleProgressDialogCancel;

			dialog.Hide ();
		}

		// === Methods ===

		void HandleProgressDialogCancel (object? o, EventArgs e)
		{
			renderer.Finish (cancel: true);
		}

		async void EffectData_PropertyChanged (object? sender, PropertyChangedEventArgs e)
		{
			// TODO: calculate bounds
			handlersInQueue++;
			await renderer.Finish (cancel: true);
			handlersInQueue--;
			if (handlersInQueue > 0) return;
			renderer.Start (effect, layer.Surface, LivePreviewSurface);
		}

		void OnUpdate (
			double progress,
			RectangleI updatedBounds)
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnUpdate() progress: " + progress);
			chrome.ProgressDialog.Progress = progress;
			HandleUpdate (updatedBounds);
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
