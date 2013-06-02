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
using Mono.Unix;
using ClipperLibrary;
using System.Collections.Generic;

namespace Pinta.Tools
{
	public class MoveSelectedTool : BaseTransformTool
	{
		private MovePixelsHistoryItem hist;
		private List<List<IntPoint>> original_selection;
		private readonly Matrix original_transform = new Matrix ();

		public override string Name {
			get { return Catalog.GetString ("Move Selected Pixels"); }
		}
		public override string Icon {
			get { return "Tools.Move.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Left click and drag the selection to move selected content. Right click and drag the selection to rotate selected content."); }
		}
		public override Gdk.Cursor DefaultCursor {
			get { return new Gdk.Cursor (PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon ("Tools.Move.png"), 0, 0); }
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
					doc.SelectionLayer.Surface, new Cairo.Rectangle (0, 0, doc.ImageSize.Width, doc.ImageSize.Height));
			}

			original_selection = new List<List<IntPoint>> (doc.Selection.SelectionPolygons);
			original_transform.InitMatrix (doc.SelectionLayer.Transform);

			hist = new MovePixelsHistoryItem (Icon, Name, doc);
			hist.TakeSnapshot (!doc.ShowSelectionLayer);

			if (!doc.ShowSelectionLayer) {
				// Copy the selection to the temp layer
				doc.CreateSelectionLayer ();
				doc.ShowSelectionLayer = true;

				using (Cairo.Context g = new Cairo.Context (doc.SelectionLayer.Surface)) {
					g.AppendPath (doc.Selection.SelectionPath);
					g.FillRule = FillRule.EvenOdd;
					g.SetSource (doc.CurrentUserLayer.Surface);
					g.Clip ();
					g.Paint ();
				}

				Cairo.ImageSurface surf = doc.CurrentUserLayer.Surface;
				
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

			List<List<IntPoint>> newSelectionPolygons = DocumentSelection.Transform (original_selection, transform);

			Document doc = PintaCore.Workspace.ActiveDocument;
			doc.Selection.SelectionClipper.Clear ();
			doc.Selection.SelectionPolygons = newSelectionPolygons;
			using (var g = new Cairo.Context (doc.CurrentUserLayer.Surface)) {
				doc.Selection.SelectionPath = g.CreatePolygonPath (DocumentSelection.ConvertToPolygonSet (newSelectionPolygons));
				g.FillRule = FillRule.EvenOdd;
				g.AppendPath (doc.Selection.SelectionPath);
			}

			doc.ShowSelection = true;
			doc.SelectionLayer.Transform.InitMatrix (original_transform);
			doc.SelectionLayer.Transform.Multiply (transform);

			PintaCore.Workspace.Invalidate ();
		}

		protected override void OnFinishTransform ()
		{
			base.OnFinishTransform ();

			if (hist != null)
				PintaCore.History.PushNewItem (hist);

			hist = null;
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

		protected override void OnDeactivated ()
		{
			base.OnDeactivated ();

			if (PintaCore.Workspace.HasOpenDocuments) {
				PintaCore.Workspace.ActiveDocument.FinishSelection ();
			}
		}
	}
}
