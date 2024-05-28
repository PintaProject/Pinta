// 
// MoveSelectedTool.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class MoveSelectedTool : BaseTransformTool
{
	private MovePixelsHistoryItem? hist;
	private DocumentSelection? original_selection;
	private readonly Matrix original_transform = CairoExtensions.CreateIdentityMatrix ();

	public MoveSelectedTool (IServiceProvider services) : base (services) { }

	public override string Name => Translations.GetString ("Move Selected Pixels");
	public override string Icon => Pinta.Resources.Icons.ToolMove;
	// Translators: {0} is 'Ctrl', or a platform-specific key such as 'Command' on macOS.
	public override string StatusBarText => Translations.GetString (
		"Left click and drag the selection to move selected content." +
		"\nHold {0} to scale instead of move." +
		"\nRight click and drag the selection to rotate selected content." +
		"\nHold Shift to rotate in steps." +
		"\nUse arrow keys to move selected content by a single pixel.",
		GtkExtensions.CtrlLabel ());
	public override Gdk.Cursor DefaultCursor => Gdk.Cursor.NewFromTexture (Resources.GetIcon (Pinta.Resources.Icons.ToolMoveCursor), 0, 0, null);
	public override Gdk.Key ShortcutKey => Gdk.Key.M;
	public override int Priority => 5;

	protected override RectangleD GetSourceRectangle (Document document)
		=> document.Selection.SelectionPath.GetBounds ().ToDouble ();

	protected override void OnStartTransform (Document document)
	{
		base.OnStartTransform (document);

		// If there is no selection, select the whole image.
		if (document.Selection.SelectionPolygons.Count == 0) {
			RectangleD imageBounds = new (0, 0, document.ImageSize.Width, document.ImageSize.Height);
			document.Selection.CreateRectangleSelection (imageBounds);
		}

		original_selection = document.Selection.Clone ();
		original_transform.InitMatrix (document.Layers.SelectionLayer.Transform);

		hist = new MovePixelsHistoryItem (Icon, Name, document);
		hist.TakeSnapshot (!document.Layers.ShowSelectionLayer);

		if (!document.Layers.ShowSelectionLayer) {
			// Copy the selection to the temp layer
			document.Layers.CreateSelectionLayer ();
			document.Layers.ShowSelectionLayer = true;
			// Make the SelectionLayer fully opaque and visible.
			document.Layers.SelectionLayer.BlendMode = BlendMode.Normal;
			document.Layers.SelectionLayer.Opacity = 1.0;
			document.Layers.SelectionLayer.Hidden = false;

			var g = new Context (document.Layers.SelectionLayer.Surface);
			g.AppendPath (document.Selection.SelectionPath);
			g.FillRule = FillRule.EvenOdd;
			g.SetSourceSurface (document.Layers.CurrentUserLayer.Surface, 0, 0);

			// Ensure that the pixels we copy have 100% opacity. The layer we're pasting them back into
			// still has its opacity (which may be less than 1.0) set as a persistent property, so as
			// soon as the move operation is committed, that opacity will apply to them once again.
			// Within the context of the layer, we want the pixels to be moved as though they are
			// opaque.
			var savedOpacity = document.Layers.CurrentUserLayer.Opacity;
			var savedBlendMode = document.Layers.CurrentUserLayer.BlendMode;

			try
			{
				document.Layers.CurrentUserLayer.Opacity = 1.0;
				document.Layers.CurrentUserLayer.BlendMode = BlendMode.Normal;

				g.Clip ();
				g.Paint ();
			}
			finally
			{
				document.Layers.CurrentUserLayer.Opacity = savedOpacity;
				document.Layers.CurrentUserLayer.BlendMode = savedBlendMode;
			}

			var surf = document.Layers.CurrentUserLayer.Surface;

			g = new Context (surf);
			g.AppendPath (document.Selection.SelectionPath);
			g.FillRule = FillRule.EvenOdd;
			g.Operator = Cairo.Operator.Clear;
			g.Fill ();
		}

		document.Workspace.Invalidate ();
	}

	protected override void OnUpdateTransform (Document document, Matrix transform)
	{
		base.OnUpdateTransform (document, transform);

		document.Selection = original_selection!.Transform (transform); // NRT - Set in OnStartTransform
		document.Selection.Visible = true;

		document.Layers.SelectionLayer.Transform.InitMatrix (original_transform);
		document.Layers.SelectionLayer.Transform.Multiply (transform);

		document.Workspace.Invalidate ();
	}

	protected override void OnFinishTransform (Document document, Matrix transform)
	{
		base.OnFinishTransform (document, transform);

		// Also transform the base selection used for the various select modes.
		var prev_selection = document.PreviousSelection;
		document.PreviousSelection = prev_selection.Transform (transform);

		if (hist != null)
			document.History.PushNewItem (hist);

		hist = null;
		original_selection = null;
		original_transform.InitIdentity ();
	}

	protected override void OnCommit (Document? document)
	{
		document?.FinishSelection ();
	}

	protected override void OnDeactivated (Document? document, BaseTool? newTool)
	{
		base.OnDeactivated (document, newTool);

		document?.FinishSelection ();
	}
}
