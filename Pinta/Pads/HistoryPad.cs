// 
// HistoryPad.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2011 Jonathan Pobst
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

using Gtk;
using Pinta.Core;
using Pinta.Docking;
using Pinta.Gui.Widgets;

namespace Pinta
{
	public class HistoryPad : IDockPad
	{
		public void Initialize (Dock workspace, Application app, Gio.Menu padMenu)
		{
			var history = new HistoryListView ();
			DockItem history_item = new DockItem (history, "History") {
				Label = Translations.GetString ("History")
			};

			var history_tb = history_item.AddToolBar ();
			history_tb.Append (PintaCore.Actions.Edit.Undo.CreateDockToolBarItem ());
			history_tb.Append (PintaCore.Actions.Edit.Redo.CreateDockToolBarItem ());

			workspace.AddItem (history_item, DockPlacement.Right);

			var show_history = new ToggleCommand ("history", Translations.GetString ("History"), null, Resources.Icons.LayerDuplicate) {
				Value = true
			};
			app.AddAction (show_history);
			padMenu.AppendItem (show_history.CreateMenuItem ());

			show_history.Toggled += (val) => { history_item.Visible = val; };
#if false // TODO-GTK4 - visibility-notify-event no longer exists, can probably replace with a dedicated event on the DockItem class
			history_item.VisibilityNotifyEvent += (o, args) => { show_history.Value = history_item.Visible; };
#endif
		}
	}
}
