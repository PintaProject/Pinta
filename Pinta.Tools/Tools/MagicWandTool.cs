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
using Gtk;

namespace Pinta.Tools
{
	public class MagicWandTool : FloodTool
	{
		private readonly IWorkspaceService workspace;

		private CombineMode combine_mode;

		public MagicWandTool (IServiceManager services) : base (services)
		{
			workspace = services.GetService<IWorkspaceService> ();

			LimitToSelection = false;
		}

		public override Gdk.Key ShortcutKey => Gdk.Key.S;
		public override string Name => Translations.GetString ("Magic Wand Select");
		public override string Icon => Pinta.Resources.Icons.ToolSelectMagicWand;
		public override string StatusBarText => Translations.GetString ("Click to select region of similar color.");
		public override Gdk.Cursor DefaultCursor => new Gdk.Cursor (Gdk.Display.Default, Resources.GetIcon ("Cursor.MagicWand.png"), 21, 10);
		public override int Priority => 19;

		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			tb.AppendItem (SelectionSeparator);

			workspace.SelectionHandler.BuildToolbar (tb, Settings);
		}


		protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
		{
			combine_mode = workspace.SelectionHandler.DetermineCombineMode (e);

			base.OnMouseDown (document, e);

			document.Selection.Visible = true;
		}

		protected override void OnFillRegionComputed (Document document, Point[][] polygonSet)
		{
			var undoAction = new SelectionHistoryItem (Icon, Name);
			undoAction.TakeSnapshot ();

			document.PreviousSelection.Dispose ();
			document.PreviousSelection = document.Selection.Clone ();

			document.Selection.SelectionPolygons.Clear ();
			SelectionModeHandler.PerformSelectionMode (combine_mode, DocumentSelection.ConvertToPolygons (polygonSet));

			document.History.PushNewItem (undoAction);
			document.Workspace.Invalidate ();
		}

		protected override void OnSaveSettings (ISettingsService settings)
		{
			base.OnSaveSettings (settings);

			workspace.SelectionHandler.OnSaveSettings (settings);
		}

		private SeparatorToolItem? selection_sep;
		private SeparatorToolItem SelectionSeparator => selection_sep ??= new SeparatorToolItem ();
	}
}
