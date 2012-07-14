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

namespace Pinta.Tools
{
	public class MoveSelectedTool : BaseTransformTool
	{
		private MovePixelsHistoryItem hist;
		private readonly Matrix temp_transform = new Matrix();

		public override string Name {
			get { return Catalog.GetString ("Move Selected Pixels"); }
		}
		public override string Icon {
			get { return "Tools.Move.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Drag the selection to move selected content."); }
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
			return doc.SelectionPath.GetBounds().ToCairoRectangle();
		}

		protected override void OnStartTransform ()
		{
			base.OnStartTransform ();

			Document doc = PintaCore.Workspace.ActiveDocument;

			hist = new MovePixelsHistoryItem (Icon, Name, doc);
			hist.TakeSnapshot (!doc.ShowSelectionLayer);

			if (!doc.ShowSelectionLayer) {
				// Copy the selection to the temp layer
				doc.CreateSelectionLayer ();
				doc.ShowSelectionLayer = true;

				using (Cairo.Context g = new Cairo.Context (doc.SelectionLayer.Surface)) {
					g.AppendPath (doc.SelectionPath);
					g.FillRule = FillRule.EvenOdd;
					g.SetSource (doc.CurrentLayer.Surface);
					g.Clip ();
					g.Paint ();
				}

				Cairo.ImageSurface surf = doc.CurrentLayer.Surface;
				
				using (Cairo.Context g = new Cairo.Context (surf)) {
					g.AppendPath (doc.SelectionPath);
					g.FillRule = FillRule.EvenOdd;
					g.Operator = Cairo.Operator.Clear;
					g.Fill ();
				}
			}
			
			PintaCore.Workspace.Invalidate ();
		}

		protected override void OnUpdateTransform (Matrix newTransform, Matrix oldTransform)
		{
			base.OnUpdateTransform (newTransform, oldTransform);

			Document doc = PintaCore.Workspace.ActiveDocument;

			temp_transform.InitMatrix(oldTransform);
			temp_transform.Invert();
			temp_transform.Multiply(newTransform);

			using (Cairo.Context g = new Cairo.Context (doc.CurrentLayer.Surface)) {
				Path old = doc.SelectionPath;
				g.FillRule = FillRule.EvenOdd;
				g.AppendPath (doc.SelectionPath);
				g.Transform(temp_transform);

				doc.SelectionPath = g.CopyPath ();
				(old as IDisposable).Dispose ();
			}

			doc.ShowSelection = true;

			temp_transform.Invert();

			doc.SelectionLayer.Transform.Multiply(temp_transform);

			PintaCore.Workspace.Invalidate ();
		}

		protected override void OnFinishTransform ()
		{
			base.OnFinishTransform ();

			if (hist != null)
				PintaCore.History.PushNewItem (hist);

			hist = null;
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

			PintaCore.Workspace.ActiveDocument.FinishSelection ();
		}
	}
}
