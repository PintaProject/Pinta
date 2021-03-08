// 
// CanvasWindow.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2015 Jonathan Pobst
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
using System.Diagnostics.CodeAnalysis;
using Gdk;
using Gtk;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta
{
	public class CanvasWindow : Grid
	{
		private Document document;
		private Ruler horizontal_ruler;
		private Ruler vertical_ruler;
		private ScrolledWindow scrolled_window;

		public PintaCanvas Canvas { get; set; }
		public bool HasBeenShown { get; set; }

		public CanvasWindow (Document document)
		{
			this.document = document;

			Build (document);

			scrolled_window.Hadjustment.ValueChanged += UpdateRulerRange;
			scrolled_window.Vadjustment.ValueChanged += UpdateRulerRange;
			document.Workspace.CanvasSizeChanged += UpdateRulerRange;
			Canvas.SizeAllocated += UpdateRulerRange;

			Canvas.MotionNotifyEvent += delegate (object? o, MotionNotifyEventArgs args) {
				if (!PintaCore.Workspace.HasOpenDocuments)
					return;

				var point = PintaCore.Workspace.WindowPointToCanvas (args.Event.X, args.Event.Y);

				horizontal_ruler.Position = point.X;
				vertical_ruler.Position = point.Y;
			};
		}

		public bool IsMouseOnCanvas {
			get {
				// Get the position of the mouse pointer relative
				// to canvas scrolled window top-left corner
				GdkExtensions.GetWidgetPointer (scrolled_window, out int x, out int y, out var mask);

				// Check if the pointer is on the canvas
				return (x > 0) && (x < scrolled_window.Allocation.Width) &&
				    (y > 0) && (y < scrolled_window.Allocation.Height);
			}
		}

		public bool RulersVisible {
			get => horizontal_ruler.Visible;
			set {
				if (horizontal_ruler.Visible != value) {
					horizontal_ruler.Visible = value;
					vertical_ruler.Visible = value;
				}
			}
		}

		public MetricType RulerMetric {
			get => horizontal_ruler.Metric;
			set {
				if (horizontal_ruler.Metric != value) {
					horizontal_ruler.Metric = value;
					vertical_ruler.Metric = value;
				}
			}
		}

		public void UpdateRulerRange (object? sender, EventArgs e)
		{
			Main.Iteration (); //Force update of scrollbar upper before recenter

			var lower = new Cairo.PointD (0, 0);
			var upper = new Cairo.PointD (0, 0);

			if (scrolled_window.Hadjustment == null || scrolled_window.Vadjustment == null)
				return;

			if (PintaCore.Workspace.HasOpenDocuments) {
				if (PintaCore.Workspace.Offset.X > 0) {
					lower.X = -PintaCore.Workspace.Offset.X / PintaCore.Workspace.Scale;
					upper.X = PintaCore.Workspace.ImageSize.Width - lower.X;
				} else {
					lower.X = scrolled_window.Hadjustment.Value / PintaCore.Workspace.Scale;
					upper.X = (scrolled_window.Hadjustment.Value + scrolled_window.Hadjustment.PageSize) / PintaCore.Workspace.Scale;
				}
				if (PintaCore.Workspace.Offset.Y > 0) {
					lower.Y = -PintaCore.Workspace.Offset.Y / PintaCore.Workspace.Scale;
					upper.Y = PintaCore.Workspace.ImageSize.Height - lower.Y;
				} else {
					lower.Y = scrolled_window.Vadjustment.Value / PintaCore.Workspace.Scale;
					upper.Y = (scrolled_window.Vadjustment.Value + scrolled_window.Vadjustment.PageSize) / PintaCore.Workspace.Scale;
				}
			}

			horizontal_ruler.SetRange (lower.X, upper.X);
			vertical_ruler.SetRange (lower.Y, upper.Y);
		}

		[MemberNotNull (nameof (Canvas), nameof (horizontal_ruler), nameof (vertical_ruler), nameof (scrolled_window))]
		private void Build (Document document)
		{
			ColumnHomogeneous = false;
			RowHomogeneous = false;

			scrolled_window = new ScrolledWindow ();

			var vp = new Viewport () {
				ShadowType = ShadowType.None
			};

			vp.ScrollEvent += ViewPort_ScrollEvent;

			Canvas = new PintaCanvas (this, document) {
				Name = "canvas",
				CanDefault = true,
				CanFocus = true,
				Events = (Gdk.EventMask) 16134
			};

			// Rulers
			horizontal_ruler = new Ruler (Orientation.Horizontal) {
				Metric = MetricType.Pixels
			};

			Attach (horizontal_ruler, 1, 0, 1, 1);

			vertical_ruler = new Ruler (Orientation.Vertical) {
				Metric = MetricType.Pixels
			};

			Attach (vertical_ruler, 0, 1, 1, 1);

			scrolled_window.Hexpand = true;
			scrolled_window.Vexpand = true;
			Attach (scrolled_window, 1, 1, 1, 1);

			scrolled_window.Add (vp);
			vp.Add (Canvas);

			ShowAll ();
			Canvas.Show ();
			vp.Show ();

			horizontal_ruler.Visible = false;
			vertical_ruler.Visible = false;
		}

		private void ViewPort_ScrollEvent (object o, ScrollEventArgs args)
		{
			// Allow the user to zoom in/out with Ctrl-Mousewheel
			if (args.Event.State.IsControlPressed () && args.Event.Direction == ScrollDirection.Smooth) {
				if (args.Event.DeltaX > 0 || args.Event.DeltaY < 0)
					document.Workspace.ZoomInFromMouseScroll (new Cairo.PointD (args.Event.X, args.Event.Y));
				else if (args.Event.DeltaX < 0 || args.Event.DeltaY > 0)
					document.Workspace.ZoomOutFromMouseScroll (new Cairo.PointD (args.Event.X, args.Event.Y));

				args.RetVal = true;
			}
		}
	}
}
