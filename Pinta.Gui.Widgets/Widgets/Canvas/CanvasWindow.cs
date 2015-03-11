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
using Gtk;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta
{
    public class CanvasWindow : Table
    {
        private HRuler horizontal_ruler;
        private VRuler vertical_ruler;
        private ScrolledWindow scrolled_window;
        
        public PintaCanvas Canvas { get; set; }
        public bool HasBeenShown { get; set; }

        public CanvasWindow (Document document) : base (2, 2, false)
        {
            scrolled_window = new ScrolledWindow ();

            var vp = new Viewport () {
                ShadowType = ShadowType.None
            };

            Canvas = new PintaCanvas (this, document) {
                Name = "canvas",
                CanDefault = true,
                CanFocus = true,
                Events = (Gdk.EventMask)16134
            };

            // Rulers
            horizontal_ruler = new HRuler ();
            horizontal_ruler.Metric = MetricType.Pixels;
            Attach (horizontal_ruler, 1, 2, 0, 1, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink | AttachOptions.Fill, 0, 0);

            vertical_ruler = new VRuler ();
            vertical_ruler.Metric = MetricType.Pixels;
            Attach (vertical_ruler, 0, 1, 1, 2, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink | AttachOptions.Fill, 0, 0);

            scrolled_window.Hadjustment.ValueChanged += delegate {
                UpdateRulerRange ();
            };

            scrolled_window.Vadjustment.ValueChanged += delegate {
                UpdateRulerRange ();
            };

            document.Workspace.CanvasSizeChanged += delegate {
                UpdateRulerRange ();
            };

            Canvas.MotionNotifyEvent += delegate (object o, MotionNotifyEventArgs args) {
                if (!PintaCore.Workspace.HasOpenDocuments)
                    return;

                var point = PintaCore.Workspace.WindowPointToCanvas (args.Event.X, args.Event.Y);

                horizontal_ruler.Position = point.X;
                vertical_ruler.Position = point.Y;
            };

            Attach (scrolled_window, 1, 2, 1, 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            scrolled_window.Add (vp);
            vp.Add (Canvas);

            ShowAll ();
            Canvas.Show ();
            vp.Show ();

            horizontal_ruler.Visible = false;
            vertical_ruler.Visible = false;

            Canvas.SizeAllocated += delegate { UpdateRulerRange (); };
        }

        public bool IsMouseOnCanvas {
            get {
                var x = 0;
                var y = 0;

                // Get the position of the mouse pointer relative
                // to canvas scrolled window top-left corner
                scrolled_window.GetPointer (out x, out y);

                // Check if the pointer is on the canvas
                return (x > 0) && (x < scrolled_window.Allocation.Width) &&
                    (y > 0) && (y < scrolled_window.Allocation.Height);
            }
        }

        public bool RulersVisible {
            get { return horizontal_ruler.Visible; }
            set {
                if (horizontal_ruler.Visible != value) {
                    horizontal_ruler.Visible = value;
                    vertical_ruler.Visible = value;
                }
            }
        }

        public MetricType RulerMetric {
            get { return horizontal_ruler.Metric; }
            set {
                if (horizontal_ruler.Metric != value) {
                    horizontal_ruler.Metric = value;
                    vertical_ruler.Metric = value;
                }
            }
        }

        public void UpdateRulerRange ()
        {
            Gtk.Main.Iteration (); //Force update of scrollbar upper before recenter

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

            horizontal_ruler.SetRange (lower.X, upper.X, 0, upper.X);
            vertical_ruler.SetRange (lower.Y, upper.Y, 0, upper.Y);
        }
    }
}
