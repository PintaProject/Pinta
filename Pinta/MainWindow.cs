// 
// MainWindow.cs
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
using Gdk;
using Gtk;
using Pinta.Core;

namespace Pinta
{
	public partial class MainWindow : Gtk.Window
	{
		DialogHandlers dialog_handler;
		
		public MainWindow () : base (Gtk.WindowType.Toplevel)
		{
			Build ();

			Requisition req = new Requisition ();
			req.Height = 600;
			req.Width = 800;
			drawingarea1.Requisition = req;

			// Initialize interface things
			PintaCore.Actions.AccelGroup = new AccelGroup ();
			this.AddAccelGroup (PintaCore.Actions.AccelGroup);

			PintaCore.Initialize (tooltoolbar, label5, drawingarea1, treeview1, this);
			colorpalettewidget1.Initialize ();

			PintaCore.Chrome.StatusBarTextChanged += new EventHandler<TextChangedEventArgs> (Chrome_StatusBarTextChanged);
			PintaCore.History.HistoryItemAdded += new EventHandler<HistoryItemAddedEventArgs> (History_HistoryItemAdded);
			PintaCore.History.HistoryItemRemoved += new EventHandler<HistoryItemRemovedEventArgs> (History_HistoryItemRemoved);
			PintaCore.Workspace.CanvasInvalidated += new EventHandler<CanvasInvalidatedEventArgs> (Workspace_CanvasInvalidated);
			PintaCore.Workspace.CanvasSizeChanged += new EventHandler (Workspace_CanvasSizeChanged);
			CreateToolBox ();

			PintaCore.Actions.CreateMainMenu (menubar1);
			PintaCore.Actions.CreateToolBar (toolbar1);
			PintaCore.Actions.Layers.CreateLayerWindowToolBar (toolbar4);
			PintaCore.Actions.Edit.CreateHistoryWindowToolBar (toolbar2);

			Gtk.Image i = new Gtk.Image (PintaCore.Resources.GetIcon ("StatusBar.CursorXY.png"));
			i.Show ();

			statusbar1.Add (i);
			Gtk.Box.BoxChild box = (Gtk.Box.BoxChild)statusbar1[i];
			box.Position = 2;
			box.Fill = false;
			box.Expand = false;

			this.Icon = PintaCore.Resources.GetIcon ("Pinta.png");

			dialog_handler = new DialogHandlers (this);
			
			// Create a blank document
			Layer background = PintaCore.Layers.AddNewLayer ("Background");
			
			using (Cairo.Context g = new Cairo.Context (background.Surface)) {
				g.SetSourceRGB (255, 255, 255);
				g.Paint ();
			}
			
			PintaCore.Workspace.Filename = "Untitled1";
			PintaCore.Workspace.IsDirty = false;
			
			PintaCore.Workspace.Invalidate ();

			treeview1.Model = new ListStore (typeof (Pixbuf), typeof (string));
			treeview1.HeadersVisible = false;
			treeview1.RowActivated += HandleTreeview1RowActivated;
			AddColumns (treeview1);

			PintaCore.Actions.View.ZoomToWindow.Activated += new EventHandler (ZoomToWindow_Activated);
			DeleteEvent += new DeleteEventHandler (MainWindow_DeleteEvent);

			WindowAction.Visible = false;

			if (Platform.GetOS () == Platform.OS.Mac)
			{
				//enable the global key handler for keyboard shortcuts
				IgeMacMenu.GlobalKeyHandlerEnabled = true;
				
				//Tell the IGE library to use your GTK menu as the Mac main menu
				IgeMacMenu.MenuBar = menubar1;
				/*
				//tell IGE which menu item should be used for the app menu's quit item
				IgeMacMenu.QuitMenuItem = yourQuitMenuItem;
				*/
				//add a new group to the app menu, and add some items to it
				var appGroup = IgeMacMenu.AddAppMenuGroup();
				MenuItem aboutItem = (MenuItem) PintaCore.Actions.Help.About.CreateMenuItem();
				appGroup.AddMenuItem(aboutItem, Mono.Unix.Catalog.GetString ("About"));
				
				menubar1.Hide();
			}
		}

		private void HandleTreeview1RowActivated (object o, RowActivatedArgs args)
		{
			int rowIndex = args.Path.Indices[0];
			
			// Determine the number of times to undo. If there are 10 items (0-9)
			// and the one with index 9 was clicked(last), there'll be no undo. If the 0th is clicked
			// there will be 9 undoes, and only the one which was clicked will remain
			int nUndoes = (treeview1.Model as ListStore).IterNChildren() - rowIndex - 1;
			
			for (int i = 0; i < nUndoes; i++)
				PintaCore.History.Undo();
		}


		private void MainWindow_DeleteEvent (object o, DeleteEventArgs args)
		{
			PintaCore.Actions.File.Exit.Activate ();
		}

		private void Workspace_CanvasSizeChanged (object sender, EventArgs e)
		{
			Requisition req = new Requisition ();
			req.Height = PintaCore.Workspace.CanvasSize.Y;
			req.Width = PintaCore.Workspace.CanvasSize.X;
			drawingarea1.Requisition = req;

			drawingarea1.QueueResize ();
		}

		private void ZoomToWindow_Activated (object sender, EventArgs e)
		{
			// The image is small enough to fit in the window
			if (PintaCore.Workspace.ImageFitsInWindow) {
				PintaCore.Actions.View.ActualSize.Activate ();
				return;
			}
			
			int image_x = PintaCore.Workspace.ImageSize.X;
			int image_y = PintaCore.Workspace.ImageSize.Y;

			int window_x = GtkScrolledWindow.Children[0].Allocation.Width;
			int window_y = GtkScrolledWindow.Children[0].Allocation.Height;
			
			// The image is more constrained by width than height
			if ((double)image_x / (double)window_x >= (double)image_y / (double)window_y) {
				double ratio = (double)(window_x - 20) / (double)image_x;
				PintaCore.Workspace.Scale = ratio;
				PintaCore.Actions.View.SuspendZoomUpdate ();
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as ComboBoxEntry).Entry.Text = string.Format ("{0}%", (int)(PintaCore.Workspace.Scale * 100));
				PintaCore.Actions.View.ResumeZoomUpdate ();
			} else {
				double ratio2 = (double)(window_y - 20) / (double)image_y;
				PintaCore.Workspace.Scale = ratio2;
				PintaCore.Actions.View.SuspendZoomUpdate ();
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as ComboBoxEntry).Entry.Text = string.Format ("{0}%", (int)(PintaCore.Workspace.Scale * 100));
				PintaCore.Actions.View.ResumeZoomUpdate ();
			}
		}

		void Workspace_CanvasInvalidated (object sender, CanvasInvalidatedEventArgs e)
		{
			if (e.EntireSurface)
				drawingarea1.GdkWindow.Invalidate ();
			else
				drawingarea1.GdkWindow.InvalidateRect (e.Rectangle, false);
		}

		void History_HistoryItemRemoved (object sender, HistoryItemRemovedEventArgs e)
		{
			// Hack: Instead of looking for the correct item to remove, blindly remove
			// the last item from the tree
			ListStore historyModel = (treeview1.Model as ListStore);
			int nChildren = historyModel.IterNChildren ();
			
			TreeIter lastChild = new TreeIter();
			historyModel.IterNthChild(out lastChild, nChildren - 1);
			
			historyModel.Remove(ref lastChild);
		}
		
		void History_HistoryItemAdded (object sender, HistoryItemAddedEventArgs e)
		{
			(treeview1.Model as Gtk.ListStore).AppendValues (PintaCore.Resources.GetIcon (e.Item.Icon), e.Item.Text);
		}

		private void Chrome_StatusBarTextChanged (object sender, TextChangedEventArgs e)
		{
			label5.Text = e.Text;
		}

		void AddColumns (TreeView treeView)
		{
			CellRendererPixbuf pix = new CellRendererPixbuf ();
			TreeViewColumn col1 = new TreeViewColumn (string.Empty, pix, "pixbuf", 0);
			treeView.AppendColumn (col1);

			CellRendererText rendererText = new CellRendererText ();
			TreeViewColumn column = new TreeViewColumn (string.Empty, rendererText, "text", 1);
			treeView.AppendColumn (column);
		}

		private void CreateToolBox ()
		{
			// Create our tools
			PintaCore.Tools.AddTool (new RectangleSelectTool ());
			PintaCore.Tools.AddTool (new MoveSelectedTool ());
			PintaCore.Tools.AddTool (new LassoSelectTool ());
			PintaCore.Tools.AddTool (new MoveSelectionTool ());
			PintaCore.Tools.AddTool (new EllipseSelectTool ());
			PintaCore.Tools.AddTool (new ZoomTool ());
			PintaCore.Tools.AddTool (new MagicWandTool ());
			PintaCore.Tools.AddTool (new PanTool ());
			PintaCore.Tools.AddTool (new PaintBucketTool ());
			PintaCore.Tools.AddTool (new GradientTool ());

			BaseTool pb = new PaintBrushTool ();
			PintaCore.Tools.AddTool (pb);
			PintaCore.Tools.AddTool (new EraserTool ());
			PintaCore.Tools.SetCurrentTool (pb);

			PintaCore.Tools.AddTool (new PencilTool ());
			PintaCore.Tools.AddTool (new ColorPickerTool ());
			PintaCore.Tools.AddTool (new CloneStampTool ());
			PintaCore.Tools.AddTool (new RecolorTool ());
			PintaCore.Tools.AddTool (new TextTool ());
			PintaCore.Tools.AddTool (new LineCurveTool ());
			PintaCore.Tools.AddTool (new RectangleTool ());
			PintaCore.Tools.AddTool (new RoundedRectangleTool ());
			PintaCore.Tools.AddTool (new EllipseTool ());
			PintaCore.Tools.AddTool (new FreeformShapeTool ());

			bool even = true;

			foreach (var tool in PintaCore.Tools) {
				if (even)
					toolbox1.Insert (tool.ToolItem, toolbox1.NItems);
				else
					toolbox2.Insert (tool.ToolItem, toolbox2.NItems);

				even = !even;
			}
		}

		#region Drawing Area
		private void OnDrawingarea1ExposeEvent (object o, Gtk.ExposeEventArgs args)
		{
			double scale = PintaCore.Workspace.Scale;

			double x = PintaCore.Workspace.Offset.X;
			double y = PintaCore.Workspace.Offset.Y;

			using (Cairo.Context g = CairoHelper.Create (drawingarea1.GdkWindow)) {
				// Black 1px border around image
				g.DrawRectangle (new Cairo.Rectangle (x, y, PintaCore.Workspace.CanvasSize.X + 1, PintaCore.Workspace.CanvasSize.Y + 1), new Cairo.Color (0, 0, 0), 1);

				// Transparent checkerboard pattern
				using (Cairo.SurfacePattern sp = new Cairo.SurfacePattern (PintaCore.Layers.TransparentLayer.Surface)) {
					sp.Extend = Cairo.Extend.Repeat;

					g.FillRectangle (new Cairo.Rectangle (x, y, PintaCore.Workspace.CanvasSize.X, PintaCore.Workspace.CanvasSize.Y), sp);
				}

				// User's layers
				g.Save ();
				g.Translate (x, y);
				g.Scale (scale, scale);

				foreach (Layer layer in PintaCore.Layers.GetLayersToPaint ()) {
					g.SetSourceSurface (layer.Surface, (int)layer.Offset.X, (int)layer.Offset.Y);
					g.PaintWithAlpha (layer.Opacity);
				}

				g.Restore ();

				// Selection outline
				if (PintaCore.Layers.ShowSelection) {
					g.Save ();
					g.Translate (x, y);
					g.Translate (0.5, 0.5);
					g.Scale (scale, scale);

					g.AppendPath (PintaCore.Layers.SelectionPath);

					if (PintaCore.Tools.CurrentTool.Name.Contains ("Select") && !PintaCore.Tools.CurrentTool.Name.Contains ("Selected")) {
						g.Color = new Cairo.Color (.7, .8, .9, .2);
						g.FillRule = Cairo.FillRule.EvenOdd;
						g.FillPreserve ();
					}
					
					g.SetDash (new double[] { 2 / scale, 4 / scale }, 0);
					g.LineWidth = 1 / scale;
					g.Color = new Cairo.Color (0, 0, 0);

					g.Stroke ();
					g.Restore ();
				}
			}
		}

		private void OnDrawingarea1MotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			Cairo.PointD point = PintaCore.Workspace.WindowPointToCanvas (args.Event.X, args.Event.Y);

			if (PintaCore.Workspace.PointInCanvas (point))
				CursorPositionLabel.Text = string.Format ("{0}, {1}", (int)point.X, (int)point.Y);

			PintaCore.Tools.CurrentTool.DoMouseMove (o, args, point);
		}

		private void OnDrawingarea1ButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			PintaCore.Tools.CurrentTool.DoMouseUp (drawingarea1, args, PintaCore.Workspace.WindowPointToCanvas (args.Event.X, args.Event.Y));
		}

		private void OnDrawingarea1ButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			PintaCore.Tools.CurrentTool.DoMouseDown (drawingarea1, args, PintaCore.Workspace.WindowPointToCanvas (args.Event.X, args.Event.Y));
		}
		#endregion
	}
}
