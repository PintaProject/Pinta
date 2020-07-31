// 
// MoveSelectionTool.cs
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
	public class MoveSelectionTool : BaseTransformTool
	{
		private SelectionHistoryItem hist;
		private DocumentSelection original_selection;
		
		public override string Name {
			get { return Catalog.GetString ("Move Selection"); }
		}
		public override string Icon {
			get { return "Tools.MoveSelection.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Left click and drag the selection to move selection outline. Hold Ctrl to scale instead of move. Right click and drag the selection to rotate selection outline. Hold Shift to rotate in steps."); }
		}
		public override Gdk.Cursor DefaultCursor {
            get { return new Gdk.Cursor (Gdk.Display.Default, PintaCore.Resources.GetIcon ("Tools.MoveSelection.png"), 0, 0); }
		}
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.M; } }
		public override int Priority { get { return 11; } }

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
			original_selection = doc.Selection.Clone ();

			hist = new SelectionHistoryItem (Icon, Name);
			hist.TakeSnapshot ();
		}

		protected override void OnUpdateTransform (Matrix transform)
		{
			base.OnUpdateTransform (transform);

			Document doc = PintaCore.Workspace.ActiveDocument;
			doc.Selection.Dispose ();
			doc.Selection = original_selection.Transform (transform);
			doc.Selection.Visible = true;

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

			original_selection.Dispose ();
			original_selection = null;
		}
		#endregion
	}
}
