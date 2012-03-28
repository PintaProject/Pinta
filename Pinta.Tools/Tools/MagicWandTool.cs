// 
// MagicWandTool.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright (c) 2010 Olivier Dufour
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
	public class MagicWandTool : FloodTool
	{
		private CombineMode combineMode;
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.S; } }

		public MagicWandTool ()
		{
			LimitToSelection = false;
		}

		public override string Name {
			get { return Catalog.GetString ("Magic Wand Select"); }
		}

		public override string Icon {
			get { return "Tools.MagicWand.png"; }
		}

		public override string StatusBarText {
			get { return Catalog.GetString ("Click to select region of similar color."); }
		}
		
		public override Gdk.Cursor DefaultCursor {
			get { return new Gdk.Cursor (PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon ("Tools.MagicWand.png"), 0, 0); }
		}
		public override int Priority { get { return 17; } }

		private enum CombineMode
		{
			Union,
			Xor,
			Exclude,
			Replace
		}
		// nothing = replace
		// Ctrl = union
		// RMB = exclude
		// Ctrl+RMB = xor

		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			//SetCursor (Cursors.WaitCursor);

			if (args.Event.IsControlPressed () && args.Event.Button == 1)
				this.combineMode = CombineMode.Union;
			else if (args.Event.IsControlPressed () && args.Event.Button == 3)
				this.combineMode = CombineMode.Xor;
			else if (args.Event.Button == 3)
				this.combineMode = CombineMode.Exclude;
			else
				this.combineMode = CombineMode.Replace;

			base.OnMouseDown (canvas, args, point);

			doc.ShowSelection = true;
		}

		protected override void OnFillRegionComputed (Point[][] polygonSet)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			SelectionHistoryItem undoAction = new SelectionHistoryItem (this.Icon, this.Name);
			undoAction.TakeSnapshot ();

			Path path = doc.Selection.Path;

			using (Context g = new Context (PintaCore.Layers.CurrentLayer.Surface)) {
				PintaCore.Layers.Selection.Path = g.CreatePolygonPath (polygonSet);

				switch (combineMode) {
					case CombineMode.Union:
						g.AppendPath (path);
						break;
					case CombineMode.Xor:
						//not supported
						break;
					case CombineMode.Exclude:
						//not supported
						break;
					case CombineMode.Replace:
						//do nothing
						break;
				}

			}

			(path as IDisposable).Dispose ();

			//Selection.PerformChanging();
			//Selection.SetContinuation(polygonSet, this.combineMode);
			//Selection.CommitContinuation();
			//Selection.PerformChanged();

			doc.History.PushNewItem (undoAction);
			doc.Workspace.Invalidate ();
		}
	}
}