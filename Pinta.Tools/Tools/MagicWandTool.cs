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
using ClipperLibrary;
using System.Collections.Generic;

namespace Pinta.Tools
{
	public class MagicWandTool : FloodTool
	{
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.S; } }

		SelectionModeHandler selHandler;

		public MagicWandTool()
		{
			LimitToSelection = false;
		}

		protected override void OnActivated()
		{
			base.OnActivated();

			selHandler = new SelectionModeHandler();
		}

		protected override void OnBuildToolBar(Gtk.Toolbar tb)
		{
			base.OnBuildToolBar(tb);

			selHandler.BuildToolbar(tb);
		}

		public override string Name
		{
			get { return Catalog.GetString("Magic Wand Select"); }
		}

		public override string Icon
		{
			get { return "Tools.MagicWand.png"; }
		}

		public override string StatusBarText
		{
			get { return Catalog.GetString("Click to select region of similar color."); }
		}

		public override Gdk.Cursor DefaultCursor
		{
			get { return new Gdk.Cursor(PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon("Cursor.MagicWand.png"), 21, 10); }
		}
		public override int Priority { get { return 17; } }

		protected override void OnMouseDown(Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			//SetCursor (Cursors.WaitCursor);

			selHandler.DetermineCombineMode(args);

			base.OnMouseDown(canvas, args, point);

			doc.ShowSelection = true;
		}

		protected override void OnFillRegionComputed(Point[][] polygonSet)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			SelectionHistoryItem undoAction = new SelectionHistoryItem(this.Icon, this.Name);
			undoAction.TakeSnapshot();


			selHandler.PerformSelectionMode(polygonSet);


			doc.History.PushNewItem(undoAction);
			doc.Workspace.Invalidate();
		}
	}
}
