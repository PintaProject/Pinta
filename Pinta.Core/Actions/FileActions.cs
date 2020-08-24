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
		public Gtk.RecentAction OpenRecent { get; private set; }
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
			menu.AppendSeparator ();
			menu.Append (Save.CreateAcceleratedMenuItem (Gdk.Key.S, Gdk.ModifierType.ControlMask));
			menu.Append (SaveAs.CreateAcceleratedMenuItem (Gdk.Key.S, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.AppendSeparator ();
			// Printing is disabled for now until it is fully functional.
#if false
			menu.Append (Print.CreateAcceleratedMenuItem (Gdk.Key.P, Gdk.ModifierType.ControlMask));
			menu.AppendSeparator ();
#endif
			menu.Append (Close.CreateAcceleratedMenuItem (Gdk.Key.W, Gdk.ModifierType.ControlMask));
			menu.Append (Exit.CreateAcceleratedMenuItem (Gdk.Key.Q, Gdk.ModifierType.ControlMask));
		}
		
		public void RegisterHandlers ()
		{
		}
#endregion

#region Event Invokers
		public void RaiseBeforeQuit ()
		{
			if (BeforeQuit != null)
				BeforeQuit (this, EventArgs.Empty);
		}

		internal bool RaiseSaveDocument (Document document, bool saveAs)
		{
			DocumentCancelEventArgs e = new DocumentCancelEventArgs (document, saveAs);

			if (SaveDocument == null)
				throw new InvalidOperationException ("GUI is not handling PintaCore.Workspace.SaveDocument");
			else
				SaveDocument (this, e);

			return !e.Cancel;
		}

		internal int RaiseModifyCompression (int defaultCompression, Gtk.Window parent)
		{
			ModifyCompressionEventArgs e = new ModifyCompressionEventArgs (defaultCompression, parent);
			
			if (ModifyCompression != null)
				ModifyCompression (this, e);
				
			return e.Cancel ? -1 : e.Quality;
		}
#endregion
	}
}
