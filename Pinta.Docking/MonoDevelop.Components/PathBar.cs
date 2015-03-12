// 
// PathBar.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using Gdk;
using Pinta.Docking;
using Pinta.Docking.Gui;

namespace MonoDevelop.Components
{
	enum EntryPosition
	{
		Left,
		Right
	}
	
	class PathEntry 
	{
		Gdk.Pixbuf darkIcon;

		public Gdk.Pixbuf Icon {
			get;
			private set;
		}
		
		public string Markup {
			get;
			private set;
		}
		
		public object Tag {
			get;
			set;
		}
		
		public bool IsPathEnd {
			get;
			set;
		}
		
		public EntryPosition Position {
			get;
			set;
		}

        public PathEntry (Gdk.Pixbuf icon, string markup)
		{
			this.Icon = icon;
			this.Markup = markup;
		}
		
		public PathEntry (string markup)
		{
			this.Markup = markup;
		}
		
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != typeof(PathEntry))
				return false;
			MonoDevelop.Components.PathEntry other = (MonoDevelop.Components.PathEntry)obj;
			return Icon == other.Icon && Markup == other.Markup;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (Icon != null ? Icon.GetHashCode () : 0) ^ (Markup != null ? Markup.GetHashCode () : 0);
			}
		}

		internal Gdk.Pixbuf DarkIcon {
			get {
				if (darkIcon == null && Icon != null) {
					darkIcon = Icon;
/*					if (Styles.BreadcrumbGreyscaleIcons)
						darkIcon = ImageService.MakeGrayscale (darkIcon);
					if (Styles.BreadcrumbInvertedIcons)
						darkIcon = ImageService.MakeInverted (darkIcon);*/
				}
				return darkIcon;
			}
		}
	}
	
	class PathBar : Gtk.DrawingArea
	{
		PathEntry[] leftPath  = new PathEntry[0];
		PathEntry[] rightPath = new PathEntry[0];
		Pango.Layout layout;
		Pango.AttrList boldAtts = new Pango.AttrList ();

        const char CR = (char)0x0D;
        const char LF = (char)0x0A;

		//HACK: a surrogate widget object to pass to style calls instead of "this" when using "button" hint.
		// This avoids GTK-Criticals in themes which try to cast the widget object to a button.
		Gtk.Button styleButton = new Gtk.Button ();

		// The widths array contains the widths of the items at the left and the right
		int[] widths;

		int height;
		int textHeight;

		bool pressed, hovering, menuVisible;
		int hoverIndex = -1;
		int activeIndex = -1;

		const int leftPadding = 6;
		const int rightPadding = 6;
		const int topPadding = 2;
		const int bottomPadding = 4;
		const int iconSpacing = 4;
		const int padding = 3;
		const int buttonPadding = 2;
		const int arrowLeftPadding = 10;
		const int arrowRightPadding = 10;
		const int arrowSize = 6;
		const int spacing = arrowLeftPadding + arrowRightPadding + arrowSize;
		const int minRegionSelectorWidth = 30;
		
		Func<int, Widget> createMenuForItem;
		Widget menuWidget;
		
		public PathBar (Func<int, Widget> createMenuForItem)
		{
			this.Events =  EventMask.ExposureMask | 
				           EventMask.EnterNotifyMask |
				           EventMask.LeaveNotifyMask |
				           EventMask.ButtonPressMask | 
				           EventMask.ButtonReleaseMask | 
				           EventMask.KeyPressMask | 
					       EventMask.PointerMotionMask;
			boldAtts.Insert (new Pango.AttrWeight (Pango.Weight.Bold));
			this.createMenuForItem = createMenuForItem;
			EnsureLayout ();
		}

		internal static string GetFirstLineFromMarkup (string markup)
		{
			var idx = markup.IndexOfAny (new [] { CR, LF }); 
			if (idx >= 0)
				return markup.Substring (0, idx);
			return markup;
		}
		
		public new PathEntry[] Path { get; private set; }
		public int ActiveIndex { get { return activeIndex; } }
		
		public void SetPath (PathEntry[] path)
		{
			if (ArrSame (this.leftPath, path))
				return;

			HideMenu ();

			this.Path = path ?? new PathEntry[0];
			this.leftPath = Path.Where (p => p.Position == EntryPosition.Left).ToArray ();
			this.rightPath = Path.Where (p => p.Position == EntryPosition.Right).ToArray ();
			
			activeIndex = -1;
			widths = null;
			EnsureWidths ();
			QueueResize ();
		}
		
		bool ArrSame (PathEntry[] a, PathEntry[] b)
		{
			if ((a == null || b == null) && a != b)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++)
				if (!a[i].Equals(b[i]))
					return false;
			return true;
		}
		
		public void SetActive (int index)
		{
			if (index >= leftPath.Length)
				throw new IndexOutOfRangeException ();
			
			if (activeIndex != index) {
				activeIndex = index;
				widths = null;
				QueueResize ();
			}
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			EnsureWidths ();
			requisition.Width = Math.Max (WidthRequest, 0);
			requisition.Height = height + topPadding + bottomPadding;
		}

		int[] GetCurrentWidths (out bool widthReduced)
		{
			int totalWidth = widths.Sum ();
			totalWidth += leftPadding + (arrowSize + arrowRightPadding) * leftPath.Length - 1;
			totalWidth += rightPadding + arrowSize * rightPath.Length - 1;
			int[] currentWidths = widths;
			widthReduced = false;
			int overflow = totalWidth - Allocation.Width;
			if (overflow > 0) {
				currentWidths = ReduceWidths (overflow);
				widthReduced = true;
			}
			return currentWidths;
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {

				ctx.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				using (var g = new Cairo.LinearGradient (0, 0, 0, Allocation.Height)) {
					g.AddColorStop (0, Styles.BreadcrumbBackgroundColor);
					g.AddColorStop (1, Styles.BreadcrumbGradientEndColor);
					ctx.SetSource (g);
				}
				ctx.Fill ();

				if (widths == null)
					return true;

				// Calculate the total required with, and the reduction to be applied in case it doesn't fit the available space

				bool widthReduced;
				var currentWidths = GetCurrentWidths (out widthReduced);

				// Render the paths

				int textTopPadding = topPadding + (height - textHeight) / 2;
				int xpos = leftPadding, ypos = topPadding;

				for (int i = 0; i < leftPath.Length; i++) {
					bool last = i == leftPath.Length - 1;

					// Reduce the item size when required
					int itemWidth = currentWidths [i];
					int x = xpos;
					xpos += itemWidth;

					if (hoverIndex >= 0 && hoverIndex < Path.Length && leftPath [i] == Path [hoverIndex] && (menuVisible || pressed || hovering))
						DrawButtonBorder (ctx, x - padding, itemWidth + padding + padding);

					int textOffset = 0;
					if (leftPath [i].DarkIcon != null) {
						int iy = (height - (int)leftPath [i].DarkIcon.Height) / 2 + topPadding;
						ctx.DrawImage (this, leftPath [i].DarkIcon, x, iy);
						textOffset += (int) leftPath [i].DarkIcon.Width + iconSpacing;
					}
					
					layout.Attributes = (i == activeIndex) ? boldAtts : null;
					layout.SetMarkup (GetFirstLineFromMarkup (leftPath [i].Markup));

					ctx.Save ();

					// If size is being reduced, ellipsize it
					bool showText = true;
					if (widthReduced) {
						int w = itemWidth - textOffset;
						if (w > 0) {
							ctx.Rectangle (x + textOffset, textTopPadding, w, height);
							ctx.Clip ();
						} else
							showText = false;
					} else
						layout.Width = -1;

					if (showText) {
						// Text
						ctx.SetSourceColor (Styles.BreadcrumbTextColor.ToCairoColor ());
						ctx.MoveTo (x + textOffset, textTopPadding);
						Pango.CairoHelper.ShowLayout (ctx, layout);
					}
					ctx.Restore ();

					if (!last) {
						xpos += arrowLeftPadding;
						if (leftPath [i].IsPathEnd) {
							Style.PaintVline (Style, GdkWindow, State, evnt.Area, this, "", ypos, ypos + height, xpos - arrowSize / 2);
						} else {
							int arrowH = Math.Min (height, arrowSize);
							int arrowY = ypos + (height - arrowH) / 2;
							DrawPathSeparator (ctx, xpos, arrowY, arrowH);
						}
						xpos += arrowSize + arrowRightPadding;
					}
				}
				
				int xposRight = Allocation.Width - rightPadding;
				for (int i = 0; i < rightPath.Length; i++) {
					//				bool last = i == rightPath.Length - 1;

					// Reduce the item size when required
					int itemWidth = currentWidths [i + leftPath.Length];
					xposRight -= itemWidth;
					xposRight -= arrowSize;
						
					int x = xposRight;
					
					if (hoverIndex >= 0 && hoverIndex < Path.Length && rightPath [i] == Path [hoverIndex] && (menuVisible || pressed || hovering))
						DrawButtonBorder (ctx, x - padding, itemWidth + padding + padding);
					
					int textOffset = 0;
					if (rightPath [i].DarkIcon != null) {
						ctx.DrawImage (this, rightPath [i].DarkIcon, x, ypos);
						textOffset += (int) rightPath [i].DarkIcon.Width + padding;
					}
					
					layout.Attributes = (i == activeIndex) ? boldAtts : null;
					layout.SetMarkup (GetFirstLineFromMarkup (rightPath [i].Markup));

					ctx.Save ();

					// If size is being reduced, ellipsize it
					bool showText = true;
					if (widthReduced) {
						int w = itemWidth - textOffset;
						if (w > 0) {
							ctx.Rectangle (x + textOffset, textTopPadding, w, height);
							ctx.Clip ();
						} else
							showText = false;
					} else
						layout.Width = -1;

					if (showText) {
						// Text
						ctx.SetSourceColor (Styles.BreadcrumbTextColor.ToCairoColor ());
						ctx.MoveTo (x + textOffset, textTopPadding);
						Pango.CairoHelper.ShowLayout (ctx, layout);
					}

					ctx.Restore ();
				}

				ctx.MoveTo (0, Allocation.Height - 0.5);
				ctx.RelLineTo (Allocation.Width, 0);
				ctx.SetSourceColor (Styles.BreadcrumbBottomBorderColor);
				ctx.LineWidth = 1;
				ctx.Stroke ();
			}

			return true;
		}

		void DrawPathSeparator (Cairo.Context ctx, double x, double y, double size)
		{
			ctx.MoveTo (x, y);
			ctx.LineTo (x + arrowSize, y + size / 2);
			ctx.LineTo (x, y + size);
			ctx.ClosePath ();
			ctx.SetSourceColor (CairoExtensions.ColorShade (Style.Dark (State).ToCairoColor (), 0.6));
			ctx.Fill ();
		}

		void DrawButtonBorder (Cairo.Context ctx, double x, double width)
		{
			x -= buttonPadding;
			width += buttonPadding;
			double y = topPadding - buttonPadding;
			double height = Allocation.Height - topPadding - bottomPadding + buttonPadding * 2;

			ctx.Rectangle (x, y, width, height);
			ctx.SetSourceColor (Styles.BreadcrumbButtonFillColor);
			ctx.Fill ();

			ctx.Rectangle (x + 0.5, y + 0.5, width - 1, height - 1);
			ctx.SetSourceColor (Styles.BreadcrumbButtonBorderColor);
			ctx.LineWidth = 1;
			ctx.Stroke ();
		}

		int[] ReduceWidths (int overflow)
		{
			int minItemWidth = 30;
			int[] currentWidths = new int[widths.Length];
			Array.Copy (widths, currentWidths, widths.Length);
			int itemsToShrink = widths.Count (i => i > minItemWidth);
			while (overflow > 0 && itemsToShrink > 0) {
				int itemSizeReduction = overflow / itemsToShrink;
				if (itemSizeReduction == 0)
					itemSizeReduction = 1;
				int reduced = 0;
				for (int n = 0; n < widths.Length && reduced < overflow; n++) {
					if (currentWidths [n] > minItemWidth) {
						var nw = currentWidths [n] - itemSizeReduction;
						if (nw <= minItemWidth) {
							nw = minItemWidth;
							itemsToShrink--;
						}
						reduced += currentWidths [n] - nw;
						currentWidths [n] = nw;
					}
				}
				overflow -= reduced;
			}
			return currentWidths;
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			HideMenu ();
			if (hovering) {
				pressed = true;
				QueueDraw ();
			}
			return true;
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			pressed = false;
			if (hovering) {
				QueueDraw ();
				ShowMenu ();
			}
			return true;
		}
		
		void ShowMenu ()
		{
			if (hoverIndex < 0)
				return;

			HideMenu ();

			menuWidget = createMenuForItem (hoverIndex);
			if (menuWidget == null)
				return;
			menuWidget.Hidden += delegate {
				
				menuVisible = false;
				QueueDraw ();
				
				//FIXME: for some reason the menu's children don't get activated if we destroy 
				//directly here, so use a timeout to delay it
				GLib.Timeout.Add (100, delegate {
					HideMenu ();
					return false;
				});
			};
			menuVisible = true;
			if (menuWidget is Menu) {
				((Menu)menuWidget).Popup (null, null, PositionFunc, 0, Gtk.Global.CurrentEventTime);
			} else {
				PositionWidget (menuWidget);
				menuWidget.ShowAll ();
			}
		}

		public void HideMenu ()
		{
			if (menuWidget != null) {
				menuWidget.Destroy ();
				menuWidget = null;
			}
		}
		
		public int GetHoverXPosition (out int w)
		{
			bool widthReduced;
			int[] currentWidths = GetCurrentWidths (out widthReduced);

			if (Path[hoverIndex].Position == EntryPosition.Left) {
				int idx = leftPath.TakeWhile (p => p != Path[hoverIndex]).Count ();
				
				if (idx >= 0) {
					w = currentWidths[idx];
					return currentWidths.Take (idx).Sum () + idx * spacing;
				}
			} else {
				int idx = rightPath.TakeWhile (p => p != Path[hoverIndex]).Count ();
				if (idx >= 0) {
					w = currentWidths[idx + leftPath.Length];
					return Allocation.Width - padding - currentWidths[idx + leftPath.Length] - spacing;
				}
			}
			w = Allocation.Width;
			return 0;
		}

		void PositionWidget (Gtk.Widget widget)
		{
			if (!(widget is Gtk.Window))
				return;
			int ox, oy;
			ParentWindow.GetOrigin (out ox, out oy);
			int w;
			int itemXPosition = GetHoverXPosition (out w);
			int dx = ox + this.Allocation.X + itemXPosition;
			int dy = oy + this.Allocation.Bottom;
			
			var req = widget.SizeRequest ();
			
			Gdk.Rectangle geometry = GtkWorkarounds.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtPoint (dx, dy));
			int width = System.Math.Max (req.Width, w);
			if (width >= geometry.Width - spacing * 2) {
				width = geometry.Width - spacing * 2;
				dx = geometry.Left + spacing;
			}
			widget.WidthRequest = width;
			if (dy + req.Height > geometry.Bottom)
				dy = oy + this.Allocation.Y - req.Height;
			if (dx + width > geometry.Right)
				dx = geometry.Right - width;
			(widget as Gtk.Window).Move (dx, dy);
			(widget as Gtk.Window).Resize (width, req.Height);
			widget.GrabFocus ();
		}
		
		
		
		void PositionFunc (Menu mn, out int x, out int y, out bool push_in)
		{
			this.GdkWindow.GetOrigin (out x, out y);
			int w;
			var rect = this.Allocation;
			y += rect.Height;
			x += GetHoverXPosition (out w);
			//if the menu would be off the bottom of the screen, "drop" it upwards
			if (y + mn.Requisition.Height > this.Screen.Height) {
				y -= mn.Requisition.Height;
				y -= rect.Height;
			}
			
			//let GTK reposition the button if it still doesn't fit on the screen
			push_in = true;
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			SetHover (GetItemAt ((int)evnt.X, (int)evnt.Y));
			return true;
		}
		
		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			pressed = false;
			SetHover (-1);
			return true;
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			SetHover (GetItemAt ((int)evnt.X, (int)evnt.Y));
			return true;
		}
		
		void SetHover (int i)
		{
			bool oldHovering = hovering;
			hovering = i > -1;
			
			if (hoverIndex != i || oldHovering != hovering) {
				if (hovering)
					hoverIndex = i;
				QueueDraw ();
			}
		}
		
		public int IndexOf (PathEntry entry)
		{
			return Path.TakeWhile (p => p != entry).Count ();
		}

		int GetItemAt (int x, int y)
		{
			int xpos = padding, xposRight = Allocation.Width - padding;
			if (widths == null || x < xpos || x > xposRight)
				return -1;

			bool widthReduced;
			int[] currentWidths = GetCurrentWidths (out widthReduced);

			for (int i = 0; i < rightPath.Length; i++) {
				xposRight -= currentWidths[i + leftPath.Length] + spacing;
				if (x > xposRight)
					return IndexOf (rightPath[i]);
			}

			for (int i = 0; i < leftPath.Length; i++) {
				xpos += currentWidths[i] + spacing;
				if (x < xpos)
					return IndexOf (leftPath[i]);
			}
			return -1;
		}
		
		void EnsureLayout ()
		{
			if (layout != null)
				layout.Dispose ();
			layout = new Pango.Layout (PangoContext);
		}
		
		void CreateWidthArray (int[] result, int index, PathEntry[] path)
		{
			// Assume that there will be icons of at least 16 pixels. This avoids
			// annoying path bar height changes when switching between empty and full paths
			int maxIconHeight = 16;

			for (int i = 0; i < path.Length; i++) {
				layout.Attributes = (i == activeIndex)? boldAtts : null;
				layout.SetMarkup (GetFirstLineFromMarkup (path[i].Markup));
				layout.Width = -1;
				int w, h;
				layout.GetPixelSize (out w, out h);
				textHeight = Math.Max (h, textHeight);
				if (path[i].DarkIcon != null) {
					maxIconHeight = Math.Max ((int)path[i].DarkIcon.Height, maxIconHeight);
					w += (int)path[i].DarkIcon.Width + iconSpacing;
				}
				result[i + index] = w;
			}
			height = Math.Max (height, maxIconHeight);
			height = Math.Max (height, textHeight);
		}

		void EnsureWidths ()
		{
			if (widths != null) 
				return;
			
			layout.SetText ("#");
			int w;
			layout.GetPixelSize (out w, out this.height);
			textHeight = height;

			widths = new int [leftPath.Length + rightPath.Length];
			CreateWidthArray (widths, 0, leftPath);
			CreateWidthArray (widths, leftPath.Length, rightPath);
		}
		
		protected override void OnStyleSet (Style previous)
		{
			base.OnStyleSet (previous);
			KillLayout ();
			EnsureLayout ();
		}
		
		void KillLayout ()
		{
			if (layout == null)
				return;
			layout.Dispose ();
			layout = null;
			boldAtts.Dispose ();
			
			widths = null;
		}
		
		public override void Destroy ()
		{
			base.Destroy ();
			styleButton.Destroy ();
			KillLayout ();
			this.boldAtts.Dispose ();
		}
	}
}
