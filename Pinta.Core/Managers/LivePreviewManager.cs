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
using System.Threading.Tasks;
using Cairo;
using Mono.Addins.Localization;
using Debug = System.Diagnostics.Debug;

namespace Pinta.Core;

public interface ILivePreview
{
	RectangleI RenderBounds { get; }
	bool IsEnabled { get; }
	ImageSurface LivePreviewSurface { get; }
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

	public ImageSurface LivePreviewSurface { get; private set; } = null!;
	public RectangleI RenderBounds { get; private set; }
	public bool IsEnabled { get; private set; }

	public async void Start (BaseEffect effect)
	{
		if (IsEnabled)
			throw new InvalidOperationException ("LivePreviewManager.Start() called while live preview is already enabled.");

		tools.Commit ();

		Document doc = workspace.ActiveDocument;
		DocumentSelection selection = doc.Selection;

		IsEnabled = true;

		//TODO Use the current tool layer instead.
		LivePreviewSurface = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			workspace.ImageSize.Width,
			workspace.ImageSize.Height);

		RenderBounds = selection.Visible ? selection.GetBounds ().ToInt () : LivePreviewSurface.GetBounds ();
		RenderBounds = workspace.ClampToImageSize (RenderBounds);

		const uint UPDATE_MILLISECONDS = 100;

		AsyncEffectRenderer.Settings settings = new (
			threadCount: system.RenderThreads,
			renderBounds: RenderBounds,
			effectIsTileable: effect.IsTileable);

		Layer layer = doc.Layers.CurrentUserLayer;

		string effectName = effect.Name;

		SimpleHistoryItem historyItem = new (effect.Icon, effect.Name);
		historyItem.TakeSnapshotOfLayer (doc.Layers.CurrentUserLayerIndex);

		RenderSession session = new (
			chrome,
			workspace,
			effect,
			() => AsyncEffectRenderer.Start (
				settings,
				effect,
				layer.Surface,
				LivePreviewSurface
			)
		);


		IProgressDialog dialog = chrome.ProgressDialog;
		dialog.Title = Translations.GetString ("Rendering Effect");
		dialog.Text = effect.Name;
		dialog.Progress = 0;
		dialog.Canceled += HandleProgressDialogCancel;

		bool renderAlive = true;
		bool userCanceled = false;

		try {
			// Paint the pre-effect layer surface into into the working surface.
			using Context ctx = new (LivePreviewSurface);
			layer.Draw (ctx, layer.Surface, 1);

			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "Start Live preview.");

			session.Start ();

			using GLibTimer _ = GLib.Functions.TimeoutAdd (
				0,
				UPDATE_MILLISECONDS,
				() => {
					if (!renderAlive) return false;
					PollForUpdate (session.CurrentRender);
					return true; // Keep ticking as long as the effect is active.
				}
			);

			bool userConfirmed = !effect.IsConfigurable || await effect.LaunchConfiguration (session);

			chrome.MainWindowBusy = true;

			if (!userConfirmed) {
				Debug.WriteLine ("User decided not to proceed with the render");
				await session.StopAsync ();
				return;
			}

			// The user confirmed, so show progress dialog
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "LivePreviewManager.Apply()");

			dialog.Show ();

			CompletionInfo result = await session.WaitForCompletionAsync ();

			// Final poll after the renderer finishes to ensure the last-rendered tiles are displayed.
			PollForUpdate (session.CurrentRender);

			foreach (var ex in result.Errors)
				Debug.WriteLine ("AsyncEffectRenderer Error while rendering effect: " + effectName + " exception: " + ex.Message + "\n" + ex.StackTrace);

			if (userCanceled) {
				Debug.WriteLine ("*User* decided to cancel the render");
				return;
			}

			Debug.WriteLine ("Render completed without the user canceling");

			using Context context = new (layer.Surface);

			context.Save ();
			workspace.ActiveDocument.Selection.Clip (context);

			layer.DrawWithOperator (context, LivePreviewSurface, Operator.Source);
			context.Restore ();

			workspace.ActiveDocument.History.PushNewItem (historyItem);

		} finally {

			IsEnabled = false;
			LivePreviewSurface = null!;
			workspace.Invalidate ();

			chrome.MainWindowBusy = false;

			dialog.Canceled -= HandleProgressDialogCancel;

			dialog.Hide ();

			renderAlive = false;
		}

		// === Methods ===

		void HandleProgressDialogCancel (object? o, EventArgs e)
		{
			userCanceled = true;
			session.Cancel ();
		}

		// This method now polls the renderer for its state instead of being a passive event handler.
		void PollForUpdate (RenderHandle renderTask)
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " Polling for update.");

			chrome.ProgressDialog.Progress = renderTask.Progress;

			if (!renderTask.TryConsumeBounds (out RectangleI updatedBounds))
				return;

			double scale = workspace.Scale;

			// Transform bounds (Image -> Canvas -> Window)

			// Calculate canvas bounds.
			PointD bounds1 = new (
				X: updatedBounds.Left * scale,
				Y: updatedBounds.Top * scale);

			PointD bounds2 = new (
				X: (updatedBounds.Right + 1) * scale,
				Y: (updatedBounds.Bottom + 1) * scale);

			// TODO Figure out why when scale > 1 that I need add on an
			// extra pixel of padding.
			// I must being doing something wrong here.
			if (scale > 1.0) {
				//x1 = (bounds.Left-1) * scale;
				bounds1 = bounds1 with { Y = (updatedBounds.Top - 1) * scale };
				//x2 = (bounds.Right+1) * scale;
				//y2 = (bounds.Bottom+1) * scale;
			}

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

	private sealed class RenderSession : ILivePreviewSession
	{
		private readonly IChromeService chrome;
		private readonly IWorkspaceService workspace;
		private readonly BaseEffect effect;
		private readonly Func<RenderHandle> start_render;
		private Task restart = Task.CompletedTask;
		internal RenderHandle CurrentRender { get; private set; } = null!; // NRT: assigned in Start()
		internal bool IsActive { get; private set; } // False once canceled, no more restarts

		internal RenderSession (
			IChromeService chrome,
			IWorkspaceService workspace,
			BaseEffect effect,
			Func<RenderHandle> startRender)
		{
			this.chrome = chrome;
			this.workspace = workspace;
			this.effect = effect;
			start_render = startRender;
		}

		public Task<bool> LaunchSimpleEffectDialog (IAddinLocalizer? localizer = null)
		{
			return chrome.LaunchSimpleEffectDialog (
				chrome.MainWindow,
				effect,
				localizer ?? new TemporaryLocalizer (),
				workspace,
				onChanged: _ => NotifyChanged ());
		}

		internal void Start ()
		{
			IsActive = true;
			CurrentRender = start_render ();
		}

		public void NotifyChanged ()
		{
			if (!IsActive || !restart.IsCompleted) return;
			restart = RestartAsync (); // New render clones effect, so it sees the changes
		}

		internal async Task<CompletionInfo> WaitForCompletionAsync ()
		{
			await restart;
			return await CurrentRender.Completion;
		}

		internal void Cancel ()
		{
			IsActive = false;
			CurrentRender.Cancel ();
		}

		internal async Task StopAsync ()
		{
			Cancel (); // Sets IsActive to false. Keep this in mind
			await restart; // Ends without starting new render: IsActive is false
			await CurrentRender.Completion;
		}

		private async Task RestartAsync () // Ensures no
						   // two renders overlap in time
		{
			CurrentRender.Cancel ();
			await CurrentRender.Completion;
			if (IsActive)
				CurrentRender = start_render ();
		}
	}
}
