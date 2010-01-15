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
using Gtk;
using Gdk;

namespace Pinta.Core
{
	public class FileActions
	{
		public Gtk.Action New { get; private set; }
		public Gtk.Action Open { get; private set; }
		public Gtk.Action OpenRecent { get; private set; }
		public Gtk.Action Close { get; private set; }
		public Gtk.Action Save { get; private set; }
		public Gtk.Action SaveAs { get; private set; }
		public Gtk.Action Print { get; private set; }
		public Gtk.Action Exit { get; private set; }
		
		public FileActions ()
		{
			New = new Gtk.Action ("New", Mono.Unix.Catalog.GetString ("New"), null, "gtk-new");
			Open = new Gtk.Action ("Open", Mono.Unix.Catalog.GetString ("Open"), null, "gtk-open");
			OpenRecent = new Gtk.Action ("OpenRecent", Mono.Unix.Catalog.GetString ("Open Recent"), null, "gtk-open");
			Close = new Gtk.Action ("Close", Mono.Unix.Catalog.GetString ("Close"), null, "gtk-close");
			Save = new Gtk.Action ("Save", Mono.Unix.Catalog.GetString ("Save"), null, "gtk-save");
			SaveAs = new Gtk.Action ("SaveAs", Mono.Unix.Catalog.GetString ("Save As"), null, "gtk-save-as");
			Print = new Gtk.Action ("Print", Mono.Unix.Catalog.GetString ("Print"), null, "gtk-print");
			Exit = new Gtk.Action ("Exit", Mono.Unix.Catalog.GetString ("Exit"), null, "gtk-quit");
			
			OpenRecent.Sensitive = false;
			Save.Sensitive = false;
			Close.Sensitive = false;
			Print.Sensitive = false;
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Remove (menu.Children[1]);
			
			menu.Append (New.CreateAcceleratedMenuItem (Gdk.Key.N, Gdk.ModifierType.ControlMask));
			menu.Append (Open.CreateAcceleratedMenuItem (Gdk.Key.O, Gdk.ModifierType.ControlMask));
			menu.Append (OpenRecent.CreateMenuItem ());
			menu.Append (Close.CreateAcceleratedMenuItem (Gdk.Key.W, Gdk.ModifierType.ControlMask));
			menu.AppendSeparator ();
			menu.Append (Save.CreateAcceleratedMenuItem (Gdk.Key.S, Gdk.ModifierType.ControlMask));
			menu.Append (SaveAs.CreateAcceleratedMenuItem (Gdk.Key.S, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.AppendSeparator ();
			menu.Append (Print.CreateAcceleratedMenuItem (Gdk.Key.P, Gdk.ModifierType.ControlMask));
			menu.AppendSeparator ();
			menu.Append (Exit.CreateAcceleratedMenuItem (Gdk.Key.Q, Gdk.ModifierType.ControlMask));
		}
		
		public void RegisterHandlers ()
		{
			New.Activated += HandlePintaCoreActionsFileNewActivated;
			Open.Activated += HandlePintaCoreActionsFileOpenActivated;
			SaveAs.Activated += HandlePintaCoreActionsFileSaveAsActivated;
			Exit.Activated += HandlePintaCoreActionsFileExitActivated;
		}
		#endregion

		#region Action Handlers
		private void HandlePintaCoreActionsFileNewActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.Clear ();
			PintaCore.History.Clear ();
			
			// Start with an empty white layer
			Layer background = PintaCore.Layers.AddNewLayer ("Background");
			
			using (Cairo.Context g = new Cairo.Context (background.Surface)) {
				g.SetSourceRGB (255, 255, 255);
				g.Paint ();
			}
			
			PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
		}

		private void HandlePintaCoreActionsFileOpenActivated (object sender, EventArgs e)
		{
			Gtk.FileChooserDialog fcd = new Gtk.FileChooserDialog ("Open Image File", null, FileChooserAction.Open, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, Gtk.Stock.Open, Gtk.ResponseType.Ok);
			
			int response = fcd.Run ();
			
			if (response == (int)Gtk.ResponseType.Ok) {
				
				string file = fcd.Filename;
				
				PintaCore.Layers.Clear ();
				
				// Open the image and add it to the layers
				Layer layer = PintaCore.Layers.AddNewLayer (System.IO.Path.GetFileName (file));
				
				Pixbuf bg = new Pixbuf (file, (int)PintaCore.Workspace.ImageSize.X, (int)PintaCore.Workspace.ImageSize.Y, true);
				
				using (Cairo.Context g = new Cairo.Context (layer.Surface)) {
					CairoHelper.SetSourcePixbuf (g, bg, 0, 0);
					g.Paint ();
				}
				
				bg.Dispose ();
				
				PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
			}
			
			fcd.Destroy ();
		}

		private void HandlePintaCoreActionsFileSaveAsActivated (object sender, EventArgs e)
		{
			Gtk.FileChooserDialog fcd = new Gtk.FileChooserDialog ("Save Image File", null, FileChooserAction.Save, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, Gtk.Stock.Save, Gtk.ResponseType.Ok);
			
			int response = fcd.Run ();
			
			if (response == (int)Gtk.ResponseType.Ok) {
				
				string file = fcd.Filename;

				Cairo.ImageSurface surf = PintaCore.Layers.GetFlattenedImage ();
				
				Pixbuf pb = surf.ToPixbuf ();
				
				if (System.IO.Path.GetExtension (file) == ".jpeg" || System.IO.Path.GetExtension (file) == ".jpg")
					pb.Save (file, "jpeg");
				else
					pb.Save (file, "png");
				
				(pb as IDisposable).Dispose ();
				(surf as IDisposable).Dispose ();
			}
			
			fcd.Destroy ();
		}

		private void HandlePintaCoreActionsFileExitActivated (object sender, EventArgs e)
		{
			PintaCore.History.Clear ();
			(PintaCore.Layers.SelectionPath as IDisposable).Dispose ();
			Application.Quit ();
		}
		#endregion
	}
}
