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
using System.Collections.Generic;

namespace Pinta.Tools
{
	public class MoveSelectedTool : BaseTransformTool
	{
		private MovePixelsHistoryItem? hist;
		private DocumentSelection? original_selection;
		private readonly Matrix original_transform = new Matrix ();

		public override string Name {
			get { return Translations.GetString ("Move Selected Pixels"); }
		}
		public override string Icon {
			get { return Resources.Icons.ToolMove; }
		}
		public override string StatusBarText {
			get { return Translations.GetString ("Left click and drag the selection to move selected content. Hold Ctrl to scale instead of move. Right click and drag the selection to rotate selected content. Hold Shift to rotate in steps. Use arrow keys to move selected content by a single pixel."); }
		}
		public override Gdk.Cursor DefaultCursor {
			get { return new Gdk.Cursor (Gdk.Display.Default, Gtk.IconTheme.Default.LoadIcon(Resources.Icons.ToolMove, 16), 0, 0); }
		}
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.M; } }
		public override int Priority { get { return 7; } }

		#region Mouse Handlers

		protected override Rectangle GetSourceRectangle ()
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			return doc.Selection.SelectionPath.GetBounds().ToCairoRectangle();
		}

		protected override void OnStartTransform ()
		{
			base.OnStartTransform ();

			Document doc = PintaCore.Workspace.ActiveDocument;

			// If there is no selection, select the whole image.
			if (doc.Selection.SelectionPolygons.Count == 0) {
				doc.Selection.CreateRectangleSelection (
					new Cairo.Rectangle (0, 0, doc.ImageSize.Width, doc.ImageSize.Height));
			}

			original_selection = doc.Selection.Clone ();
			original_transform.InitMatrix (doc.Layers.SelectionLayer.Transform);

			hist = new MovePixelsHistoryItem (Icon, Name, doc);
			hist.TakeSnapshot (!doc.Layers.ShowSelectionLayer);

			if (!doc.Layers.ShowSelectionLayer) {
				// Copy the selection to the temp layer
				doc.Layers.CreateSelectionLayer ();
				doc.Layers.ShowSelectionLayer = true;
				//Use same BlendMode, Opacity and Visibility for SelectionLayer
				doc.Layers.SelectionLayer.BlendMode = doc.Layers.CurrentUserLayer.BlendMode;
				doc.Layers.SelectionLayer.Opacity = doc.Layers.CurrentUserLayer.Opacity;
				doc.Layers.SelectionLayer.Hidden = doc.Layers.CurrentUserLayer.Hidden;					

				using (Cairo.Context g = new Cairo.Context (doc.Layers.SelectionLayer.Surface)) {
					g.AppendPath (doc.Selection.SelectionPath);
					g.FillRule = FillRule.EvenOdd;
					g.SetSource (doc.Layers.CurrentUserLayer.Surface);
					g.Clip ();
					g.Paint ();
				}

				Cairo.ImageSurface surf = doc.Layers.CurrentUserLayer.Surface;
				
				using (Cairo.Context g = new Cairo.Context (surf)) {
					g.AppendPath (doc.Selection.SelectionPath);
					g.FillRule = FillRule.EvenOdd;
					g.Operator = Cairo.Operator.Clear;
					g.Fill ();
				}
			}
			
			PintaCore.Workspace.Invalidate ();
		}

		protected override void OnUpdateTransform (Matrix transform)
		{
			base.OnUpdateTransform (transform);

			Document doc = PintaCore.Workspace.ActiveDocument;
			doc.Selection.Dispose ();
			doc.Selection = original_selection!.Transform (transform); // NRT - Set in OnStartTransform
			doc.Selection.Visible = true;

			doc.Layers.SelectionLayer.Transform.InitMatrix (original_transform);
			doc.Layers.SelectionLayer.Transform.Multiply (transform);

			PintaCore.Workspace.Invalidate ();
		}

		protected override void OnFinishTransform (Matrix transform)
		{
			base.OnFinishTransform (transform);

			// Also transform the base selection used for the various select modes.
			var doc = PintaCore.Workspace.ActiveDocument;
			using (var prev_selection = doc.PreviousSelection)
				doc.PreviousSelection = prev_selection.Transform (transform);

			if (hist != null)
				PintaCore.Workspace.ActiveDocument.History.PushNewItem (hist);

			hist = null;
			original_selection?.Dispose ();
			original_selection = null;
			original_transform.InitIdentity ();
		}
		#endregion

		protected override void OnCommit ()
		{
			try {
				PintaCore.Workspace.ActiveDocument.FinishSelection ();
			} catch (Exception) {
				// Ignore an error where ActiveDocument fails.
			}
		}

		protected override void OnDeactivated(BaseTool newTool)
		{
			base.OnDeactivated (newTool);

			if (PintaCore.Workspace.HasOpenDocuments) {
				PintaCore.Workspace.ActiveDocument.FinishSelection ();
			}
		}
	}
}
