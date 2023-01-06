// 
// WindowActions.cs
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
using System.Linq;
using Gtk;

namespace Pinta.Core
{
	public class WindowActions
	{
		private Gio.Menu doc_section = null!; // NRT - Set in RegisterActions
		private static readonly string doc_action_id = "active_document";
		private Gio.SimpleAction active_doc_action;

		public Command SaveAll { get; private set; }
		public Command CloseAll { get; private set; }

		// TODO-GTK4 - add pre-defined VariantType's to gir.core
		private static readonly GLib.VariantType IntVariantType = new ("i");

		public WindowActions ()
		{
			SaveAll = new Command ("SaveAll", Translations.GetString ("Save All"), null, Resources.StandardIcons.DocumentSave);
			CloseAll = new Command ("CloseAll", Translations.GetString ("Close All"), null, Resources.StandardIcons.WindowClose);

			active_doc_action = Gio.SimpleAction.NewStateful (doc_action_id, IntVariantType, GLib.Variant.Create (-1));

#if false // TODO-GTK4 - seems to be some issues here in gir.core
			active_doc_action.OnActivate += (o, e) => {
				var idx = e.Parameter!.GetInt();
				if (idx < PintaCore.Workspace.OpenDocuments.Count) {
					PintaCore.Workspace.SetActiveDocumentInternal (PintaCore.Workspace.OpenDocuments[idx]);
					active_doc_action.ChangeState (e.Parameter);
				}
			};
#endif
		}

		#region Initialization
		public void RegisterActions (Gtk.Application app, Gio.Menu menu)
		{
			app.AddAccelAction (SaveAll, "<Ctrl><Alt>A");
			menu.AppendItem (SaveAll.CreateMenuItem ());

			app.AddAccelAction (CloseAll, "<Primary><Shift>W");
			menu.AppendItem (CloseAll.CreateMenuItem ());

			doc_section = Gio.Menu.New ();
			menu.AppendSection (null, doc_section);

			app.AddAction (active_doc_action);
		}
		#endregion

		#region Public Methods

		public void SetActiveDocument (Document doc)
		{
			var idx = PintaCore.Workspace.OpenDocuments.IndexOf (doc);
#if false // TODO-GTK4 - re-enable after OnActivate() issues above are fixed
			active_doc_action.Activate (GLib.Variant.Create (idx));
#else
			if (idx < PintaCore.Workspace.OpenDocuments.Count) {
				PintaCore.Workspace.SetActiveDocumentInternal (PintaCore.Workspace.OpenDocuments[idx]);
			}
#endif
		}

		public void AddDocument (Document doc)
		{
			doc.Renamed += (o, e) => { RebuildDocumentMenu (); };
			doc.IsDirtyChanged += (o, e) => { RebuildDocumentMenu (); };

			AddDocumentMenuItem (PintaCore.Workspace.OpenDocuments.IndexOf (doc));
		}

		public void RemoveDocument (Document doc)
		{
			RebuildDocumentMenu ();
		}
		#endregion

		#region Private Methods
		private void AddDocumentMenuItem (int idx)
		{
			var doc = PintaCore.Workspace.OpenDocuments[idx];
			var action_id = string.Format ("app.{0}({1})", doc_action_id, idx);
			var label = string.Format ("{0}{1}", doc.DisplayName, doc.IsDirty ? "*" : string.Empty);
			var menu_item = Gio.MenuItem.New (label, action_id);
			doc_section.AppendItem (menu_item);

			// We only assign accelerators up to Alt-9
			if (idx < 9)
				PintaCore.Chrome.Application.SetAccelsForAction (action_id, new[] { string.Format ("<Alt>{0}", idx + 1) });
		}

		private void RebuildDocumentMenu ()
		{
			doc_section.RemoveAll ();
			for (int i = 0; i < PintaCore.Workspace.OpenDocuments.Count; ++i)
				AddDocumentMenuItem (i);

			PintaCore.Workspace.ResetTitle ();
		}
		#endregion
	}
}
