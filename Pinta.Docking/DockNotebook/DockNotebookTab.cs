//
// DockNotebookTab.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Xwt.Motion;

namespace Pinta.Docking.DockNotebook
{
	public class DockNotebookTab: IAnimatable
	{
		DockNotebook notebook;
		readonly TabStrip strip;

		string text;
		string markup;
		Gdk.Pixbuf icon;
		Widget content;

		internal Gdk.Rectangle Allocation;
		internal Gdk.Rectangle CloseButtonAllocation;

		public DockNotebook Notebook { get { return notebook; } }

		public int Index { get; internal set; }

		public bool Notify { get; set; }

		public double WidthModifier { get; set; }

		public double Opacity { get; set; }

		public double GlowStrength { get; set; }

		public bool Hidden { get; set; }

		public double DirtyStrength { get; set; }
		
		void IAnimatable.BatchBegin () { }
		void IAnimatable.BatchCommit () { QueueDraw (); }

		bool dirty;
		public bool Dirty {
			get { return dirty; }
			set { 
				if (dirty == value)
					return;
				dirty = value;
				this.Animate ("Dirty", f => DirtyStrength = f,
				              easing: Easing.CubicInOut,
				              start: DirtyStrength, end: value ? 1 : 0);
			}
		}

		public string Text {
			get {
				return text;
			}
			set {
				text = value;
				markup = null;
				strip.Update ();
			}
		}

		public string Markup {
			get {
				return markup;
			}
			set {
				markup = value;
				text = null;
				strip.Update ();
			}
		}

		public Gdk.Pixbuf Icon {
			get {
				return icon;
			}
			set {
				icon = value;
				strip.Update ();
			}
		}

		public Widget Content {
			get {
				return content;
			}
			set {
				content = value;
				notebook.ShowContent (this);
			}
		}

		public string Tooltip { get; set; }

		internal DockNotebookTab (DockNotebook notebook, TabStrip strip)
		{
			this.notebook = notebook;
			this.strip = strip;
		}

		internal Gdk.Rectangle SavedAllocation { get; private set; }
		internal double SaveStrength { get; set; }

		internal void SaveAllocation ()
		{
			SavedAllocation = Allocation;
		}

		public void QueueDraw ()
		{
			strip.QueueDraw ();
		}
	}
}
