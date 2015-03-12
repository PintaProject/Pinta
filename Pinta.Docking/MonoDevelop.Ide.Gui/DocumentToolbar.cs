// 
// DocumentToolbar.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc
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
using MonoDevelop.Components;
using Pinta.Docking;


namespace Pinta.Docking.Gui
{
	public class DocumentToolbar
	{
		Gtk.Widget frame;
		Box box;
		bool empty = true;

		internal DocumentToolbar ()
		{
			box = new HBox ();
			box.Spacing = 3;
			box.Show ();
			var al = new ToolbarBox (0, 0, 1, 1);
			al.LeftPadding = 5;
			al.TopPadding = 4;
			al.BottomPadding = 4;
			al.Add (box);
			frame = al;
		}
		
		internal Widget Container {
			get { return frame; }
		}
		
		public void Add (Widget widget)
		{
			Add (widget, false);
		}
		
		public void Add (Widget widget, bool fill)
		{
			Add (widget, fill, -1);
		}
		
		public void Add (Widget widget, bool fill, int padding)
		{
			Add (widget, fill, padding, -1);
		}
		
		void Add (Widget widget, bool fill, int padding, int index)
		{
			int defaultPadding = 3;

			if (widget is Button) {
				((Button)widget).Relief = ReliefStyle.None;
				((Button)widget).FocusOnClick = false;
				defaultPadding = 0;
				ChangeColor (widget);
			}
			else if (widget is Entry) {
				((Entry)widget).HasFrame = false;
			}
			else if (widget is ComboBox) {
				((ComboBox)widget).HasFrame = false;
			}
			else if (widget is VSeparator) {
				((VSeparator)widget).HeightRequest = 10;
				ChangeColor (widget);
			}
			else
				ChangeColor (widget);
			
			if (padding == -1)
				padding = defaultPadding;
			
			box.PackStart (widget, fill, fill, (uint)padding);
			if (empty) {
				empty = false;
				frame.Show ();
			}
			if (index != -1) {
				Box.BoxChild bc = (Box.BoxChild) box [widget];
				bc.Position = index;
			}
		}

		void ChangeColor (Gtk.Widget w)
		{
			w.Realized += delegate {
				w.ModifyText (StateType.Normal, Styles.BreadcrumbTextColor);
				w.ModifyFg (StateType.Normal, Styles.BreadcrumbTextColor);
			};
			if (w is Gtk.Container) {
				foreach (var c in ((Gtk.Container)w).Children)
					ChangeColor (c);
			}
		}
		
		public void Insert (Widget w, int index)
		{
			Add (w, false, 0, index);
		}
		
		public void Remove (Widget widget)
		{
			box.Remove (widget);
		}
		
		public bool Visible {
			get {
				return empty || frame.Visible;
			}
			set {
				frame.Visible = value;
			}
		}
		
		public bool Sensitive {
			get { return frame.Sensitive; }
			set { frame.Sensitive = value; }
		}
		
		public void ShowAll ()
		{
			frame.ShowAll ();
		}
		
		public Widget[] Children {
			get { return box.Children; }
		}

		class ToolbarBox: Gtk.Alignment
		{
			public ToolbarBox (float xa, float ya, float sx, float sy): base (xa, ya, sx, sy)
			{
			}

			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
					ctx.Rectangle (0, 0, Allocation.Width, Allocation.Height);
					using (Cairo.LinearGradient g = new Cairo.LinearGradient (0, 0, 0, Allocation.Height)) {
						g.AddColorStop (0, Styles.BreadcrumbBackgroundColor);
						g.AddColorStop (1, Styles.BreadcrumbGradientEndColor);
						ctx.SetSource (g);
						ctx.Fill ();
					}

					ctx.MoveTo (0.5, Allocation.Height - 0.5);
					ctx.RelLineTo (Allocation.Width, 0);
					ctx.SetSourceColor (Styles.BreadcrumbBottomBorderColor);
					ctx.LineWidth = 1;
					ctx.Stroke ();
				}
				return base.OnExposeEvent (evnt);
			}
		}
	}

	public class DocumentToolButton: Gtk.Button
	{
		public DocumentToolButton (string stockId)
		{
			Image = new Gtk.Image (stockId, IconSize.Menu);
			Image.Show ();
		}
		
		public DocumentToolButton (string stockId, string label)
		{
			Label = label;
			Image = new Gtk.Image (stockId, IconSize.Menu);
			Image.Show ();
		}
	}
}

