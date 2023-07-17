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
using System.Collections.Generic;
using ClipperLib;
using Gtk;

namespace Pinta.Core
{
	public class ActionManager
	{
		public AppActions App { get; private set; } = new ();
		public FileActions File { get; private set; } = new ();
		public EditActions Edit { get; private set; } = new ();
		public ViewActions View { get; private set; } = new ();
		public ImageActions Image { get; private set; } = new ();
		public LayerActions Layers { get; private set; } = new ();
		public AdjustmentsActions Adjustments { get; private set; } = new ();
		public EffectsActions Effects { get; private set; } = new ();
		public WindowActions Window { get; private set; } = new ();
		public HelpActions Help { get; private set; } = new ();
		public AddinActions Addins { get; private set; } = new ();

		public ActionManager ()
		{
		}

		public void CreateToolBar (Gtk.Box toolbar)
		{
			toolbar.Append (File.New.CreateToolBarItem ());
			toolbar.Append (File.Open.CreateToolBarItem ());
			toolbar.Append (File.Save.CreateToolBarItem ());
			// Printing is disabled for now until it is fully functional.
#if false
			toolbar.AppendItem (File.Print.CreateToolBarItem ());
#endif
			toolbar.Append (GtkExtensions.CreateToolBarSeparator ());

			// Cut/Copy/Paste comes before Undo/Redo on Windows
			if (PintaCore.System.OperatingSystem == OS.Windows) {
				toolbar.Append (Edit.Cut.CreateToolBarItem ());
				toolbar.Append (Edit.Copy.CreateToolBarItem ());
				toolbar.Append (Edit.Paste.CreateToolBarItem ());
				toolbar.Append (GtkExtensions.CreateToolBarSeparator ());
				toolbar.Append (Edit.Undo.CreateToolBarItem ());
				toolbar.Append (Edit.Redo.CreateToolBarItem ());
			} else {
				toolbar.Append (Edit.Undo.CreateToolBarItem ());
				toolbar.Append (Edit.Redo.CreateToolBarItem ());
				toolbar.Append (GtkExtensions.CreateToolBarSeparator ());
				toolbar.Append (Edit.Cut.CreateToolBarItem ());
				toolbar.Append (Edit.Copy.CreateToolBarItem ());
				toolbar.Append (Edit.Paste.CreateToolBarItem ());
			}

			toolbar.Append (GtkExtensions.CreateToolBarSeparator ());
			toolbar.Append (Image.CropToSelection.CreateToolBarItem ());
			toolbar.Append (Edit.Deselect.CreateToolBarItem ());
		}

		public void CreateHeaderToolBar (Adw.HeaderBar header)
		{
			header.PackStart (File.New.CreateToolBarItem ());
			header.PackStart (File.Open.CreateToolBarItem ());
			header.PackStart (File.Save.CreateToolBarItem ());

			header.PackStart (GtkExtensions.CreateToolBarSeparator ());
			header.PackStart (Edit.Undo.CreateToolBarItem ());
			header.PackStart (Edit.Redo.CreateToolBarItem ());

			header.PackStart (GtkExtensions.CreateToolBarSeparator ());
			header.PackStart (Edit.Cut.CreateToolBarItem ());
			header.PackStart (Edit.Copy.CreateToolBarItem ());
			header.PackStart (Edit.Paste.CreateToolBarItem ());
		}

		public void CreateStatusBar (Box statusbar)
		{
			// Cursor position widget
			statusbar.Append (Gtk.Image.NewFromIconName (Resources.Icons.CursorPosition));
			var cursor = Label.New ("  0, 0");
			statusbar.Append (cursor);

			PintaCore.Chrome.LastCanvasCursorPointChanged += delegate {
				var pt = PintaCore.Chrome.LastCanvasCursorPoint;
				cursor.SetText ($"  {pt.X}, {pt.Y}");
			};

			statusbar.Append (GtkExtensions.CreateToolBarSeparator ());

			// Selection size widget
			statusbar.Append (Gtk.Image.NewFromIconName (Resources.Icons.ToolSelectRectangle));
			var selection_size = Label.New ("  0, 0");
			statusbar.Append (selection_size);

			PintaCore.Workspace.SelectionChanged += delegate {
				var bounds = PintaCore.Workspace.HasOpenDocuments ? PintaCore.Workspace.ActiveDocument.Selection.GetBounds () : new RectangleD ();
				selection_size.SetText ($"  {bounds.Width}, {bounds.Height}");
			};

			// Document zoom widget
			View.CreateStatusBar (statusbar);
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
