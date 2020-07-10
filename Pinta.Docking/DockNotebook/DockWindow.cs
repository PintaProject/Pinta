//
// DockWindow.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

using Gdk;
using Gtk;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pinta.Docking.DockNotebook
{
	public class DockWindow : Gtk.Window
	{
		static List<DockWindow> allWindows = new List<DockWindow> ();

		public DockWindow () : base (Gtk.WindowType.Toplevel)
		{
			IdeApp.CommandService.RegisterTopWindow (this);
			AddAccelGroup (IdeApp.CommandService.AccelGroup);

			allWindows.Add (this);

			var notebook = new SdiDragNotebook ((DefaultWorkbench)IdeApp.Workbench.RootWindow);
			notebook.NavigationButtonsVisible = false;
			Child = new DockNotebookContainer (notebook);
			notebook.InitSize ();
		}

		public static IEnumerable<DockWindow> GetAllWindows ()
		{
			return allWindows;
		}

		public DockNotebookContainer Container {
			get {
				return (DockNotebookContainer) Child;
			}
		}

		bool IsChildOfMe (Document d)
		{
			Widget control = ((SdiWorkspaceWindow)d.Window).TabControl;
			while (control.Parent != null)
				control = control.Parent;
			return control == this;
		}

		protected override bool OnDeleteEvent (Event evnt)
		{
			var documents = IdeApp.Workbench.Documents.Where (IsChildOfMe).ToList ();

			int howManyDirtyFiles = documents.Count (doc => doc.IsDirty);
			if (howManyDirtyFiles > 1) {
				using (var dlg = new DirtyFilesDialog (documents, closeWorkspace: false, groupByProject: false)) {
					dlg.Modal = true;
					if (MessageService.ShowCustomDialog (dlg) != (int)Gtk.ResponseType.Ok)
						return true;
				}
			} else if (howManyDirtyFiles == 1) {
				// Ensure dirty file is closed first. This prevents saved files being closed
				// if the save is cancelled.
				documents.Sort (DirtyFilesFirst);
			}

			if (howManyDirtyFiles > 1) {
				foreach (var d in documents)
					d.Close (true).Ignore ();
			} else if (documents.Count > 0) {
				// Document.Close() could leave the UI synchronization context, letting the Gtk signal handler pass
				// and Gtk would destroy the window immediately. Since we need to preserve the window
				// state until the async document.Close () has finished, we interrupt the signal (return true)
				// and destoy the window in a continuation task after the document has been closed.
				CloseDocumentsAsync (documents).Ignore ();
				return true;
			}
			return base.OnDeleteEvent (evnt);
		}

		async Task CloseDocumentsAsync (List<Document> documents)
		{
			foreach (var d in documents) {
				if (!await d.Close ())
					return;
			}
			Destroy ();
		}

		static int DirtyFilesFirst (Document x, Document y)
		{
			if (x.IsDirty == y.IsDirty)
				return 0;
			else if (x.IsDirty)
				return -1;
			else
				return 1;
		}

		protected override bool OnConfigureEvent (EventConfigure evnt)
		{
			((DefaultWorkbench)IdeApp.Workbench.RootWindow).SetActiveWidget (Focus);
			return base.OnConfigureEvent (evnt);
		}

		protected override bool OnFocusInEvent (EventFocus evnt)
		{
			((DefaultWorkbench)IdeApp.Workbench.RootWindow).SetActiveWidget (Focus);
			return base.OnFocusInEvent (evnt);
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			return ((DefaultWorkbench)IdeApp.Workbench.RootWindow).FilterWindowKeypress (evnt) || base.OnKeyPressEvent (evnt);
		}

		protected override void OnDestroyed ()
		{
			allWindows.Remove (this);
			RemoveAccelGroup (IdeApp.CommandService.AccelGroup);
			base.OnDestroyed ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			// A small delay to make sure the window is fully rendered before showing it
			GdkWindow.Opacity = 0;
			GLib.Timeout.Add (120, delegate {
				GdkWindow.Opacity = 1;
				return false;
			});
		}
	}
	
}
