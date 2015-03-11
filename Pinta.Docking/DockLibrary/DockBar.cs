//
// DockBar.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using Gtk;
using System.Collections.Generic;

namespace Pinta.Docking
{
	public class DockBar: Gtk.EventBox
	{
		Gtk.PositionType position;
		Box box;
		DockFrame frame;
		Label filler;
		bool alwaysVisible;
		bool showBorder = true;

		internal DockBar (DockFrame frame, Gtk.PositionType position)
		{
			VisibleWindow = false;
			this.frame = frame;
			this.position = position;
			Gtk.Alignment al = new Alignment (0,0,0,0);
			if (Orientation == Gtk.Orientation.Horizontal)
				box = new HBox ();
			else
				box = new VBox ();

			al.Add (box);
			Add (al);
			
			filler = new Label ();
			filler.WidthRequest = 4;
			filler.HeightRequest = 4;
			box.PackEnd (filler);
			
			ShowAll ();
			UpdateVisibility ();
		}
		
		public bool IsExtracted {
			get { return OriginalBar != null; }
		}
		
		internal DockBar OriginalBar { get; set; }
		
		public bool AlwaysVisible {
			get { return this.alwaysVisible; }
			set { this.alwaysVisible = value; UpdateVisibility (); }
		}

		public bool AlignToEnd { get; set; }

		public bool ShowBorder {
			get { return showBorder; }
			set { showBorder = value; QueueResize (); }
		}
		
		internal Gtk.Orientation Orientation {
			get {
				return (position == PositionType.Left || position == PositionType.Right) ? Gtk.Orientation.Vertical : Gtk.Orientation.Horizontal;
			}
		}
		
		internal Gtk.PositionType Position {
			get {
				return position;
			}
		}

		internal DockFrame Frame {
			get {
				return frame;
			}
		}

		DateTime hoverActivationDelay;

		void DisableHoverActivation ()
		{
			// Temporarily disables hover activation of items in this bar
			// Useful to avoid accidental triggers when the bar items
			// are reallocated
			hoverActivationDelay = DateTime.Now + TimeSpan.FromSeconds (1.5);
		}

		internal bool HoverActivationEnabled {
			get { return DateTime.Now >= hoverActivationDelay; }
		}
		
		internal DockBarItem AddItem (DockItem item, int size)
		{
			DisableHoverActivation ();
			DockBarItem it = new DockBarItem (this, item, size);
			box.PackStart (it, false, false, 0);
			it.ShowAll ();
			UpdateVisibility ();
			it.Shown += OnItemVisibilityChanged;
			it.Hidden += OnItemVisibilityChanged;
			return it;
		}
		
		void OnItemVisibilityChanged (object o, EventArgs args)
		{
			DisableHoverActivation ();
			UpdateVisibility ();
		}
		
		internal void OnCompactLevelChanged ()
		{
			UpdateVisibility ();
			if (OriginalBar != null)
				OriginalBar.UpdateVisibility ();
		}
		
		internal void UpdateVisibility ()
		{
			if (Frame.OverlayWidgetVisible) {
				Visible = false;
			} else {
				filler.Visible = (Frame.CompactGuiLevel < 3);
				int visibleCount = 0;
				foreach (Gtk.Widget w in box.Children) {
					if (w.Visible)
						visibleCount++;
				}
				Visible = alwaysVisible || filler.Visible || visibleCount > 0;
			}
		}
		
		internal void RemoveItem (DockBarItem it)
		{
			DisableHoverActivation ();
			box.Remove (it);
			it.Shown -= OnItemVisibilityChanged;
			it.Hidden -= OnItemVisibilityChanged;
			UpdateVisibility ();
		}

		internal void UpdateTitle (DockItem item)
		{
			foreach (Widget w in box.Children) {
				DockBarItem it = w as DockBarItem;
				if (it != null && it.DockItem == item) {
					it.UpdateTab ();
					break;
				}
			}
		}
		
		internal void UpdateStyle (DockItem item)
		{
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			if (ShowBorder) {
				// Add space for the separator
				if (Orientation == Gtk.Orientation.Vertical)
					requisition.Width++;
				else
					requisition.Height++;
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (ShowBorder && Child != null) {
				switch (Position) {
				case PositionType.Left: allocation.Width--; break;
				case PositionType.Right: allocation.X++; allocation.Width--; break;
				case PositionType.Top: allocation.Height--; break;
				case PositionType.Bottom: allocation.Y++; allocation.Height--; break;
				}
				Child.SizeAllocate (allocation);
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			var alloc = Allocation;
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				ctx.Rectangle (alloc.X, alloc.Y, alloc.X + alloc.Width, alloc.Y + alloc.Height);
				Cairo.LinearGradient gr;
				if (Orientation == Gtk.Orientation.Vertical)
					gr = new Cairo.LinearGradient (alloc.X, alloc.Y, alloc.X + alloc.Width, alloc.Y);
				else
					gr = new Cairo.LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height);
				using (gr) {
					gr.AddColorStop (0, Styles.DockBarBackground1);
					gr.AddColorStop (1, Styles.DockBarBackground2);
					ctx.SetSource (gr);
				}
				ctx.Fill ();

				// Light shadow
				double offs = ShowBorder ? 1.5 : 0.5;
				switch (Position) {
				case PositionType.Left:ctx.MoveTo (alloc.X + alloc.Width - offs, alloc.Y); ctx.RelLineTo (0, Allocation.Height); break;
				case PositionType.Right: ctx.MoveTo (alloc.X + offs, alloc.Y); ctx.RelLineTo (0, Allocation.Height); break;
				case PositionType.Top: ctx.MoveTo (alloc.X, alloc.Y + alloc.Height - offs); ctx.RelLineTo (Allocation.Width, 0); break;
				case PositionType.Bottom: ctx.MoveTo (alloc.X, alloc.Y + offs); ctx.RelLineTo (Allocation.Width, 0); break;
				}
				ctx.LineWidth = 1;
				ctx.SetSourceColor (Styles.DockBarSeparatorColorLight);
				ctx.Stroke ();
			}

			if (Child != null)
				PropagateExpose (Child, evnt);

			if (ShowBorder) {
				using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
					ctx.LineWidth = 1;

					// Dark separator
					switch (Position) {
					case PositionType.Left:ctx.MoveTo (alloc.X + alloc.Width - 0.5, alloc.Y); ctx.RelLineTo (0, Allocation.Height); break;
					case PositionType.Right: ctx.MoveTo (alloc.X + 0.5, alloc.Y); ctx.RelLineTo (0, Allocation.Height); break;
					case PositionType.Top: ctx.MoveTo (alloc.X, alloc.Y + alloc.Height + 0.5); ctx.RelLineTo (Allocation.Width, 0); break;
					case PositionType.Bottom: ctx.MoveTo (alloc.X, alloc.Y + 0.5); ctx.RelLineTo (Allocation.Width, 0); break;
					}
					ctx.SetSourceColor (Styles.DockSeparatorColor.ToCairoColor ());
					ctx.Stroke ();
				}
			}
			return true;
		}
	}
}

