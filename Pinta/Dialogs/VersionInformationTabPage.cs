// VersionInformationTabPage.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//

using System;
using Gtk;
using System.Reflection;
using Mono.Unix;

namespace Pinta
{
	internal class VersionInformationTabPage : VBox
	{
		private ListStore data = null;
		private CellRenderer cellRenderer = new CellRendererText ();

		public VersionInformationTabPage ()
		{
			TreeView treeView = new TreeView ();

			TreeViewColumn treeViewColumnTitle = new TreeViewColumn (Catalog.GetString ("Title"), cellRenderer, "text", 0);
			treeViewColumnTitle.FixedWidth = 200;
			treeViewColumnTitle.Sizing = TreeViewColumnSizing.Fixed;
			treeViewColumnTitle.Resizable = true;
			treeView.AppendColumn (treeViewColumnTitle);

			TreeViewColumn treeViewColumnVersion = new TreeViewColumn (Catalog.GetString ("Version"), cellRenderer, "text", 1);
			treeView.AppendColumn (treeViewColumnVersion);

			TreeViewColumn treeViewColumnPath = new TreeViewColumn (Catalog.GetString ("Path"), cellRenderer, "text", 2);
			treeView.AppendColumn (treeViewColumnPath);

			treeView.RulesHint = true;

			data = new ListStore (typeof (string), typeof (string), typeof (string));
			treeView.Model = data;

			ScrolledWindow scrolledWindow = new ScrolledWindow ();
			scrolledWindow.Add (treeView);
			scrolledWindow.ShadowType = ShadowType.In;

			BorderWidth = 6;

			PackStart (scrolledWindow, true, true, 0);

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
				try {
					AssemblyName assemblyName = assembly.GetName ();
					data.AppendValues (assemblyName.Name, assemblyName.Version.ToString (), System.IO.Path.GetFullPath (assembly.Location));
				} catch { }
			}

			data.SetSortColumnId (0, SortType.Ascending);
		}

		protected override void OnDestroyed ()
		{
			if (cellRenderer != null) {
				cellRenderer.Destroy ();
				cellRenderer = null;
			}

			if (data != null) {
				data.Dispose ();
				data = null;
			}

			base.OnDestroyed ();
		}
	}
}
