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
using System.Linq;
using System.Threading;
using Cairo;
using static Pinta.Core.AsyncEffectRenderer;
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

	private sealed record LivePreviewRenderSettings (
		Document activeDocument,
		Layer currentUserLayer,
		ImageSurface livePreviewSurface,
		RectangleI renderBounds,
		ImageSurface source,
		ImageSurface dest,
		Settings settings,
		Context ctx,
		IReadOnlyList<TileRenderInfo> renderInfos);
	private static LivePreviewRenderSettings CreateSettings (BaseEffect effect)
	{
		var activeDocument = PintaCore.Workspace.ActiveDocument;
		Layer layer = activeDocument.Layers.CurrentUserLayer;

		// Create live preview surface.
		//TODO Use the current tool layer instead.
		var livePreviewSurface = CairoExtensions.CreateImageSurface (
			Cairo.Format.Argb32,
			PintaCore.Workspace.ImageSize.Width,
			PintaCore.Workspace.ImageSize.Height
		);

		var renderBounds = GetRenderBounds (activeDocument, livePreviewSurface);

		var source = layer.Surface;
		var dest = livePreviewSurface;

		var settings = new AsyncEffectRenderer.Settings () {
			ThreadCount = PintaCore.System.RenderThreads,
			TileWidth = renderBounds.Width,
			TileHeight = 1,
			ThreadPriority = ThreadPriority.BelowNormal
		};

		// Paint the pre-effect layer surface into into the working surface.
		var ctx = new Cairo.Context (livePreviewSurface);

		//TODO calculate bounds.
		int totalTiles = CalculateTotalTiles (renderBounds, settings);
		var renderInfos = effect.IsTileable ?
		(
			Enumerable.Range (0, totalTiles)
			.Select (tileIndex => new TileRenderInfo (tileIndex, GetTileBounds (settings, renderBounds, tileIndex)))
			.ToArray ()
		)
		:
		(
			new[] {
				new TileRenderInfo (0, renderBounds),
			}
		);

		return new (
			activeDocument: activeDocument,
			currentUserLayer: layer,
			livePreviewSurface: livePreviewSurface,
			renderBounds: renderBounds,
			source: source,
			dest: dest,
			settings: settings,
			ctx: ctx,
			renderInfos: renderInfos
		);
	}

	private static int CalculateTotalTiles (RectangleI renderBounds, Settings settings)
	{
		return (int) (Math.Ceiling ((float) renderBounds.Width / (float) settings.TileWidth)
			* Math.Ceiling ((float) renderBounds.Height / (float) settings.TileHeight));
	}

	// Runs on a background thread.
	private static RectangleI GetTileBounds (Settings settings, RectangleI renderBounds, int tileIndex)
	{
		int horizTileCount = (int) Math.Ceiling ((float) renderBounds.Width
						       / (float) settings.TileWidth);

		int x = ((tileIndex % horizTileCount) * settings.TileWidth) + renderBounds.X;
		int y = ((tileIndex / horizTileCount) * settings.TileHeight) + renderBounds.Y;
		int w = Math.Min (settings.TileWidth, renderBounds.Right + 1 - x);
		int h = Math.Min (settings.TileHeight, renderBounds.Bottom + 1 - y);

		return new RectangleI (x, y, w, h);
	}

	public void Start (BaseEffect effect)
	{
		if (IsEnabled)
			throw new InvalidOperationException ("LivePreviewManager.Start() called while live preview is already enabled.");

		IsEnabled = true;

		PintaCore.Tools.Commit ();

		LivePreviewRenderSettings settings = CreateSettings (effect);

		bool apply_live_preview_flag = false;
		bool cancel_live_preview_flag = false;

		SimpleHistoryItem history_item = new SimpleHistoryItem (effect.Icon, effect.Name);
		history_item.TakeSnapshotOfLayer (settings.activeDocument.Layers.CurrentUserLayerIndex);

		settings.currentUserLayer.Draw (settings.ctx, settings.currentUserLayer.Surface, 1);

		AsyncEffectRenderer renderer = null!;
		renderer = new Renderer (settings.settings, OnUpdate, OnCompletion);


		LivePreviewSurface = settings.livePreviewSurface;
		RenderBounds = settings.renderBounds;

		// Listen for changes to effectConfiguration object, and restart render if needed.
		if (effect.EffectData != null)
			effect.EffectData.PropertyChanged += EffectData_PropertyChanged;

		Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + "Start Live preview.");

		OnStarted (new LivePreviewStartedEventArgs ());

		// Start rendering.
		renderer.Start (effect, settings.source, settings.dest, settings.renderInfos);

		LaunchConfig ();

		// Method asks render task to complete, and then returns immediately. The cancel
		// is not actually complete until the LivePreviewRenderCompleted event is fired.
		void Cancel ()
		{
			Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.Cancel()");

			cancel_live_preview_flag = true;

			renderer.Cancel (settings.source, settings.dest, settings.renderInfos);

			// Show a busy cursor, and make the main window insensitive,
			// until the cancel has completed.
			PintaCore.Chrome.MainWindowBusy = true;

			if (renderer == null || !renderer.IsRendering)
				HandleCancel ();
		}

		void HandleProgressDialogCancel (object? o, EventArgs? e)
		{
			Cancel ();
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

			renderer?.Dispose ();
			renderer = null!;

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

			var ctx = new Cairo.Context (settings.currentUserLayer.Surface);
			ctx.Save ();
			PintaCore.Workspace.ActiveDocument.Selection.Clip (ctx);

			settings.currentUserLayer.DrawWithOperator (ctx, LivePreviewSurface, Cairo.Operator.Source);
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
			renderer!.Start (effect, settings.currentUserLayer.Surface, LivePreviewSurface, settings.renderInfos);
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

		void LaunchConfig ()
		{
			if (effect.IsConfigurable)
				HandleConfigurable ();
			else
				HandleNotConfigurable ();
		}

		void HandleNotConfigurable ()
		{
			PintaCore.Chrome.MainWindowBusy = true;
			Apply ();
			return;
		}

		void HandleConfigurable ()
		{
			EventHandler<BaseEffect.ConfigDialogResponseEventArgs>? handler = null;
			handler = (_, args) => {
				if (args.Accepted) {
					PintaCore.Chrome.MainWindowBusy = true;
					Apply ();
				} else {
					PintaCore.Chrome.MainWindowBusy = true;
					Cancel ();
				}

				// Unsubscribe once we're done.
				effect.ConfigDialogResponse -= handler;
			};

			effect.ConfigDialogResponse += handler;
			effect.LaunchConfiguration ();
		}
	}

	void OnUpdate (double progress, RectangleI updatedBounds)
	{
		Debug.WriteLine (DateTime.Now.ToString ("HH:mm:ss:ffff") + " LivePreviewManager.OnUpdate() progress: " + progress);
		PintaCore.Chrome.ProgressDialog.Progress = progress;
		FireLivePreviewRenderUpdatedEvent (progress, updatedBounds);
	}

	void FireLivePreviewRenderUpdatedEvent (double progress, RectangleI bounds)
	{
		OnRenderUpdated (new LivePreviewRenderUpdatedEventArgs (progress, bounds));
	}

	private static RectangleI GetRenderBounds (Document doc, Cairo.ImageSurface livePreviewSurface)
	{
		// Handle selection path.
		var selection = doc.Selection;
		Cairo.Path? selection_path = (selection.Visible) ? selection.SelectionPath : null;
		var renderBounds = (selection_path != null) ? selection_path.GetBounds () : livePreviewSurface.GetBounds ();
		renderBounds = PintaCore.Workspace.ClampToImageSize (renderBounds);
		return renderBounds;
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

	static void LivePreview_RenderUpdated (LivePreviewRenderUpdatedEventArgs args)
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
