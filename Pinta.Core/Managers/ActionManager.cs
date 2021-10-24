// 
// ActionManager.cs
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
using Gtk;
using System.Collections.Generic;
using ClipperLib;

namespace Pinta.Core
{
	public class ActionManager
	{
		public AccelGroup AccelGroup { get; private set; }

		public AppActions App { get; private set; }
		public FileActions File { get; private set; }
		public EditActions Edit { get; private set; }
		public ViewActions View { get; private set; }
		public ImageActions Image { get; private set; }
		public LayerActions Layers { get; private set; }
		public AdjustmentsActions Adjustments { get; private set; }
		public EffectsActions Effects { get; private set; }
		public AddinActions Addins { get; private set; }
		public WindowActions Window { get; private set; }
		public HelpActions Help { get; private set; }
		
		public ActionManager ()
		{
			AccelGroup = new AccelGroup ();

			App = new AppActions();
			File = new FileActions ();
			Edit = new EditActions ();
			View = new ViewActions ();
			Image = new ImageActions ();
			Layers = new LayerActions ();
			Adjustments = new AdjustmentsActions ();
			Effects = new EffectsActions ();
			Addins = new AddinActions ();
			Window = new WindowActions ();
			Help = new HelpActions ();
		}
		
		public void CreateToolBar (Gtk.Toolbar toolbar)
		{
			toolbar.AppendItem (File.New.CreateToolBarItem ());
			toolbar.AppendItem (File.Open.CreateToolBarItem ());
			toolbar.AppendItem (File.Save.CreateToolBarItem ());
			// Printing is disabled for now until it is fully functional.
#if false
			toolbar.AppendItem (File.Print.CreateToolBarItem ());
#endif
			toolbar.AppendItem (new SeparatorToolItem ());

			// Cut/Copy/Paste comes before Undo/Redo on Windows
			if (PintaCore.System.OperatingSystem == OS.Windows) {
				toolbar.AppendItem (Edit.Cut.CreateToolBarItem ());
				toolbar.AppendItem (Edit.Copy.CreateToolBarItem ());
				toolbar.AppendItem (Edit.Paste.CreateToolBarItem ());
				toolbar.AppendItem (new SeparatorToolItem ());
				toolbar.AppendItem (Edit.Undo.CreateToolBarItem ());
				toolbar.AppendItem (Edit.Redo.CreateToolBarItem ());
			} else {
				toolbar.AppendItem (Edit.Undo.CreateToolBarItem ());
				toolbar.AppendItem (Edit.Redo.CreateToolBarItem ());
				toolbar.AppendItem (new SeparatorToolItem ());
				toolbar.AppendItem (Edit.Cut.CreateToolBarItem ());
				toolbar.AppendItem (Edit.Copy.CreateToolBarItem ());
				toolbar.AppendItem (Edit.Paste.CreateToolBarItem ());
			}

			toolbar.AppendItem (new SeparatorToolItem ());
			toolbar.AppendItem (Image.CropToSelection.CreateToolBarItem ());
			toolbar.AppendItem (Edit.Deselect.CreateToolBarItem ());
		}

		public void CreateStatusBar (Statusbar statusbar)
		{
			// Document zoom widget
			View.CreateStatusBar (statusbar);

			// Selection size widget
			var SelectionSize = new ToolBarLabel ("  0, 0");

			statusbar.AppendItem (SelectionSize);
			statusbar.AppendItem (new ToolBarImage (Resources.Icons.ToolSelectRectangle));

			PintaCore.Workspace.SelectionChanged += delegate {
				var bounds = PintaCore.Workspace.HasOpenDocuments ? PintaCore.Workspace.ActiveDocument.Selection.GetBounds () : new Cairo.Rectangle ();
				SelectionSize.Text = string.Format ("  {0}, {1}", bounds.Width, bounds.Height);
			};

			statusbar.AppendItem (new SeparatorToolItem { Margin = 6 }, 6);

			// Cursor position widget
			var cursor = new ToolBarLabel ("  0, 0");

			statusbar.AppendItem (cursor);
			statusbar.AppendItem (new ToolBarImage (Resources.Icons.CursorPosition));

			PintaCore.Chrome.LastCanvasCursorPointChanged += delegate {
				var pt = PintaCore.Chrome.LastCanvasCursorPoint;
				cursor.Text = string.Format ("  {0}, {1}", pt.X, pt.Y);
			};
		}

		public void RegisterHandlers ()
		{
			File.RegisterHandlers ();
			Edit.RegisterHandlers ();
			Image.RegisterHandlers ();
			Layers.RegisterHandlers ();
			View.RegisterHandlers ();
			Help.RegisterHandlers ();
		}
	}
}
