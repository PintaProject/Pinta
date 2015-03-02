// 
// CanvasPad.cs
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
using Mono.Unix;
using MonoDevelop.Components.Docking;
using Pinta.Core;
using Pinta.Gui.Widgets;
using System;

namespace Pinta
{
	public class CanvasPad : IDockPad
	{
		private ScrolledWindow sw;
		private PintaCanvas canvas;
		private HRuler hruler;
		private VRuler vruler;

		public ScrolledWindow ScrolledWindow { get { return sw; } }
		public PintaCanvas Canvas { get { return canvas; } }
		public HRuler HorizontalRuler { get { return hruler; } }
		public VRuler VerticalRuler { get { return vruler; } }

		public void Initialize (DockFrame workspace, Menu padMenu)
		{
			// Create canvas
			Table mainTable = new Table (2, 2, false);

			sw = new ScrolledWindow () {
				Name = "sw",
				ShadowType = ShadowType.EtchedOut
			};

			Viewport vp = new Viewport () {
				ShadowType = ShadowType.None
			};

			canvas = new PintaCanvas () {
				Name = "canvas",
				CanDefault = true,
				CanFocus = true,
				Events = (Gdk.EventMask)16134
			};

			// Canvas pad
			DockItem documentDockItem = workspace.AddItem ("Canvas");
			documentDockItem.Behavior = DockItemBehavior.Locked;
			documentDockItem.Expand = true;

			documentDockItem.DrawFrame = false;
			documentDockItem.Label = Catalog.GetString ("Canvas");
			documentDockItem.Content = mainTable;
			documentDockItem.Icon = PintaCore.Resources.GetIcon ("Menu.Effects.Artistic.OilPainting.png");

			//rulers
			hruler = new HRuler ();
			hruler.Metric = MetricType.Pixels;
			mainTable.Attach (hruler, 1, 2, 0, 1, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink | AttachOptions.Fill, 0, 0);

			vruler = new VRuler ();
			vruler.Metric = MetricType.Pixels;
			mainTable.Attach (vruler, 0, 1, 1, 2, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink | AttachOptions.Fill, 0, 0);

			sw.Hadjustment.ValueChanged += delegate {
				UpdateRulerRange ();
			};

			sw.Vadjustment.ValueChanged += delegate {
				UpdateRulerRange ();
			};

			PintaCore.Workspace.CanvasSizeChanged += delegate {
				UpdateRulerRange ();
			};

			canvas.MotionNotifyEvent += delegate (object o, MotionNotifyEventArgs args) {
				if (!PintaCore.Workspace.HasOpenDocuments)
					return;

				Cairo.PointD point = PintaCore.Workspace.WindowPointToCanvas (args.Event.X, args.Event.Y);

				hruler.Position = point.X;
				vruler.Position = point.Y;

			};

			mainTable.Attach (sw, 1, 2, 1, 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 0);

			sw.Add (vp);
			vp.Add (canvas);

			mainTable.ShowAll ();
			canvas.Show ();
			vp.Show ();

			hruler.Visible = false;
			vruler.Visible = false;


			PintaCore.Chrome.InitializeCanvas (canvas);

			canvas.SizeAllocated += delegate { UpdateRulerRange (); };

			PintaCore.Actions.View.Rulers.Toggled += HandleRulersToggled;
			PintaCore.Actions.View.Pixels.Activated += (o, e) => { SetRulersUnit (MetricType.Pixels); };
			PintaCore.Actions.View.Inches.Activated += (o, e) => { SetRulersUnit (MetricType.Inches); };
			PintaCore.Actions.View.Centimeters.Activated += (o, e) => { SetRulersUnit (MetricType.Centimeters); };
		}

		private void HandleRulersToggled (object sender, EventArgs e)
		{
			var visible = ((ToggleAction)sender).Active;

			hruler.Visible = visible;
			vruler.Visible = visible;
		}

		public void UpdateRulerRange ()
		{
			Gtk.Main.Iteration (); //Force update of scrollbar upper before recenter

			Cairo.PointD lower = new Cairo.PointD (0, 0);
			Cairo.PointD upper = new Cairo.PointD (0, 0);

			if (PintaCore.Workspace.HasOpenDocuments) {
				if (PintaCore.Workspace.Offset.X > 0) {
					lower.X = -PintaCore.Workspace.Offset.X / PintaCore.Workspace.Scale;
					upper.X = PintaCore.Workspace.ImageSize.Width - lower.X;
				} else {
					lower.X = sw.Hadjustment.Value / PintaCore.Workspace.Scale;
					upper.X = (sw.Hadjustment.Value + sw.Hadjustment.PageSize) / PintaCore.Workspace.Scale;
				}
				if (PintaCore.Workspace.Offset.Y > 0) {
					lower.Y = -PintaCore.Workspace.Offset.Y / PintaCore.Workspace.Scale;
					upper.Y = PintaCore.Workspace.ImageSize.Height - lower.Y;
				} else {
					lower.Y = sw.Vadjustment.Value / PintaCore.Workspace.Scale;
					upper.Y = (sw.Vadjustment.Value + sw.Vadjustment.PageSize) / PintaCore.Workspace.Scale;
				}
			}

			hruler.SetRange (lower.X, upper.X, 0, upper.X);
			vruler.SetRange (lower.Y, upper.Y, 0, upper.Y);
		}

		private void SetRulersUnit (Gtk.MetricType metric)
		{
			hruler.Metric = metric;
			vruler.Metric = metric;
		}
	}
}
