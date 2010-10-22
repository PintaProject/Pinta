// 
// FileActions.cs
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
using System.IO;
using Gdk;
using Gtk;
using Mono.Unix;

namespace Pinta.Core
{
	public class FileActions
	{
		public Gtk.Action New { get; private set; }
		public Gtk.Action NewScreenshot { get; private set; }
		public Gtk.Action Open { get; private set; }
		public Gtk.Action OpenRecent { get; private set; }
		public Gtk.Action Close { get; private set; }
		public Gtk.Action Save { get; private set; }
		public Gtk.Action SaveAs { get; private set; }
		public Gtk.Action Print { get; private set; }
		public Gtk.Action Exit { get; private set; }
		
		public event EventHandler BeforeQuit;
		public event EventHandler<ModifyCompressionEventArgs> ModifyCompression;
		public event EventHandler<DocumentCancelEventArgs> SaveDocument;
		
		public FileActions ()
		{
			New = new Gtk.Action ("New", Catalog.GetString ("New..."), null, Stock.New);
			NewScreenshot = new Gtk.Action ("NewScreenshot", Catalog.GetString ("New Screenshot..."), null, Stock.Fullscreen);
			Open = new Gtk.Action ("Open", Catalog.GetString ("Open..."), null, Stock.Open);
			OpenRecent = new RecentAction ("OpenRecent", Catalog.GetString ("Open Recent"), null, Stock.Open, RecentManager.Default);
			
			RecentFilter recentFilter = new RecentFilter ();
			recentFilter.AddApplication ("Pinta");
			
			(OpenRecent as RecentAction).AddFilter (recentFilter);
			
			Close = new Gtk.Action ("Close", Catalog.GetString ("Close"), null, Stock.Close);
			Save = new Gtk.Action ("Save", Catalog.GetString ("Save"), null, Stock.Save);
			SaveAs = new Gtk.Action ("SaveAs", Catalog.GetString ("Save As..."), null, Stock.SaveAs);
			Print = new Gtk.Action ("Print", Catalog.GetString ("Print"), null, Stock.Print);
			Exit = new Gtk.Action ("Exit", Catalog.GetString ("Quit"), null, Stock.Quit);

			New.ShortLabel = Catalog.GetString ("New");
			Open.ShortLabel = Catalog.GetString ("Open");
			Open.IsImportant = true;
			Save.IsImportant = true;
			
			Close.Sensitive = false;
			Print.Sensitive = false;
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Append (New.CreateAcceleratedMenuItem (Gdk.Key.N, Gdk.ModifierType.ControlMask));
			menu.Append (NewScreenshot.CreateMenuItem ());
			menu.Append (Open.CreateAcceleratedMenuItem (Gdk.Key.O, Gdk.ModifierType.ControlMask));
			menu.Append (OpenRecent.CreateMenuItem ());
			menu.Append (Close.CreateAcceleratedMenuItem (Gdk.Key.W, Gdk.ModifierType.ControlMask));
			menu.AppendSeparator ();
			menu.Append (Save.CreateAcceleratedMenuItem (Gdk.Key.S, Gdk.ModifierType.ControlMask));
			menu.Append (SaveAs.CreateAcceleratedMenuItem (Gdk.Key.S, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.AppendSeparator ();
			//menu.Append (Print.CreateAcceleratedMenuItem (Gdk.Key.P, Gdk.ModifierType.ControlMask));
			//menu.AppendSeparator ();
			menu.Append (Exit.CreateAcceleratedMenuItem (Gdk.Key.Q, Gdk.ModifierType.ControlMask));
		}
		
		public void RegisterHandlers ()
		{
			Exit.Activated += HandlePintaCoreActionsFileExitActivated;
		}
		#endregion

		#region Public Methods
		public void NewFile (Size imageSize)
		{
			PintaCore.Workspace.CreateAndActivateDocument (null, imageSize);
			PintaCore.Workspace.ActiveDocument.HasFile = false;
			PintaCore.Workspace.ActiveWorkspace.CanvasSize = imageSize;

			// Start with an empty white layer
			Layer background = PintaCore.Workspace.ActiveDocument.AddNewLayer (Catalog.GetString ("Background"));
			
			using (Cairo.Context g = new Cairo.Context (background.Surface)) {
				g.SetSourceRGB (1, 1, 1);
				g.Paint ();
			}

			PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (new BaseHistoryItem (Stock.New, Catalog.GetString ("New Image")));
			PintaCore.Workspace.ActiveDocument.IsDirty = false;
			PintaCore.Actions.View.ZoomToWindow.Activate ();
		}
		
		public void NewFileWithScreenshot ()
		{
			Screen screen = Screen.Default;
			Pixbuf pb = Pixbuf.FromDrawable (screen.RootWindow, screen.RootWindow.Colormap, 0, 0, 0, 0, screen.Width, screen.Height);
			NewFile (new Size (screen.Width, screen.Height));

			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers[0].Surface)) {
				CairoHelper.SetSourcePixbuf (g, pb, 0, 0);
				g.Paint ();
			}

			(pb as IDisposable).Dispose ();
			PintaCore.Workspace.ActiveDocument.IsDirty = true;
		}
		#endregion

		#region Event Invokers
		internal bool RaiseSaveDocument (Document document)
		{
			DocumentCancelEventArgs e = new DocumentCancelEventArgs (document);

			if (SaveDocument == null)
				throw new InvalidOperationException ("GUI is not handling PintaCore.Workspace.SaveDocument");
			else
				SaveDocument (this, e);

			return !e.Cancel;
		}

		internal int PromptJpegCompressionLevel ()
		{
			ModifyCompressionEventArgs e = new ModifyCompressionEventArgs (85);
			
			if (ModifyCompression != null)
				ModifyCompression (this, e);
				
			return e.Cancel ? -1 : e.Quality;
		}
		#endregion
		
		#region Action Handlers
		private void HandlePintaCoreActionsFileExitActivated (object sender, EventArgs e)
		{
			while (PintaCore.Workspace.HasOpenDocuments) {
				int count = PintaCore.Workspace.OpenDocuments.Count;
				
				Close.Activate ();
				
				// If we still have the same number of open documents,
				// the user cancelled on a Save prompt.
				if (count == PintaCore.Workspace.OpenDocuments.Count)
					return;
			}
			
			if (BeforeQuit != null)
				BeforeQuit (this, EventArgs.Empty);
				
			Application.Quit ();
		}
		#endregion
	}
}
