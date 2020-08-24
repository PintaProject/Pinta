//
// CurvesDialog.cs
//  
// Author:
//      Krzysztof Marecki <marecki.krzysztof@gmail.com>
// 
// Copyright (c) 2010 Krzysztof Marecki
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
using Mono.Unix;
using System.Collections.Generic;
using Cairo;

using Pinta.Core;

namespace Pinta.Effects
{

	public class CurvesDialog : Gtk.Dialog
	{
		private ComboBox comboMap;
		private Label labelPoint;
		private DrawingArea drawing;
		private CheckButton checkRed;
		private CheckButton checkGreen;
		private CheckButton checkBlue;
		private Button buttonReset;
		private Label labelTip;

		private class ControlPointDrawingInfo 
		{
			public Color Color { get; set; }
			public bool IsActive { get; set; }
		}
		
		//drawing area width and height
		private const int size = 256;
		//control point radius
		private const int radius = 6;
		
		private int channels;
		//last added control point x;
		private int last_cpx;
		
		//control points for luminosity transfer mode
		private SortedList<int, int>[] luminosity_cps;
		//control points for rg transfer mode
		private SortedList<int, int>[] rgb_cps;
		
		public SortedList<int, int>[] ControlPoints { 
			get { 
				return (Mode == ColorTransferMode.Luminosity) ? luminosity_cps : rgb_cps;
			}
			set {
				if (Mode == ColorTransferMode.Luminosity)
					luminosity_cps = value;
				else
					rgb_cps = value;
			}
		}
		
		public ColorTransferMode Mode {
			get { 
				return (comboMap.Active == 0) ? 
						ColorTransferMode.Rgb : 
						ColorTransferMode.Luminosity;
			}
		}

		public CurvesData EffectData { get; private set; }
		
		public CurvesDialog (CurvesData effectData) : base (Catalog.GetString ("Curves"), PintaCore.Chrome.MainWindow,
		                                                    DialogFlags.Modal,
															Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
															Gtk.Stock.Ok, Gtk.ResponseType.Ok)
		{
			Build ();
			
			EffectData = effectData;
		
			drawing.DoubleBuffered = true;
			
			comboMap.Changed += HandleComboMapChanged;
			buttonReset.Clicked += HandleButtonResetClicked;
			checkRed.Toggled += HandleCheckToggled;
			checkGreen.Toggled += HandleCheckToggled;
			checkBlue.Toggled += HandleCheckToggled;
			drawing.ExposeEvent += HandleDrawingExposeEvent;
			drawing.MotionNotifyEvent += HandleDrawingMotionNotifyEvent;
			drawing.LeaveNotifyEvent += HandleDrawingLeaveNotifyEvent;
			drawing.ButtonPressEvent += HandleDrawingButtonPressEvent;
			
			ResetControlPoints ();
			AlternativeButtonOrder = new int[] { (int) Gtk.ResponseType.Ok, (int) Gtk.ResponseType.Cancel };
		}
		
		private void UpdateLivePreview (string propertyName)
		{
			if (EffectData != null) {
				EffectData.ControlPoints = ControlPoints;
				EffectData.Mode = Mode;
				EffectData.FirePropertyChanged (propertyName);
			}
		}		
		
		private void HandleCheckToggled (object o, EventArgs args)
		{
			InvalidateDrawing ();
		}

		void HandleButtonResetClicked (object sender, EventArgs e)
		{
			ResetControlPoints ();
			InvalidateDrawing ();
		}

		private void ResetControlPoints()
		{
			channels = (Mode == ColorTransferMode.Luminosity) ? 1 : 3;
			ControlPoints = new SortedList<int, int>[channels];
			
			for (int i = 0; i < channels; i++) {
				SortedList<int, int> list = new SortedList<int, int> ();
				
				list.Add (0, 0);
				list.Add (size - 1, size - 1);
				ControlPoints [i] = list;
			}
			
			UpdateLivePreview ("ControlPoints");
		}
		
		private void HandleComboMapChanged (object sender, EventArgs e)
		{
			if (ControlPoints == null)
				ResetControlPoints ();
			else
				UpdateLivePreview ("Mode");
			
			bool visible = (Mode == ColorTransferMode.Rgb);
			checkRed.Visible = checkGreen.Visible = checkBlue.Visible = visible;
			
			InvalidateDrawing ();
		}
		
		private void InvalidateDrawing ()
		{
			//to invalidate whole drawing area
			drawing.GdkWindow.Invalidate();		
		}
		
		private void HandleDrawingLeaveNotifyEvent (object o, Gtk.LeaveNotifyEventArgs args)
		{
			InvalidateDrawing ();
		}
		
		private IEnumerable<SortedList<int,int>> GetActiveControlPoints ()
		{
			if (Mode == ColorTransferMode.Luminosity)
				yield return ControlPoints [0];
			else {
				if (checkRed.Active)
					yield return ControlPoints [0];
				
				if (checkGreen.Active)
					yield return ControlPoints [1];
				
				if (checkBlue.Active)
					yield return ControlPoints [2];
			}
		}
				
		private void AddControlPoint (int x, int y)
		{
			foreach (var controlPoints in GetActiveControlPoints ()) {
				controlPoints [x] = size - 1 - y;
			}
			
			last_cpx = x;
			
			UpdateLivePreview ("ControlPoints");
		}
		
		private void HandleDrawingMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{	
			int x, y;
			Gdk.ModifierType mask;
			drawing.GdkWindow.GetPointer (out x, out y, out mask); 
			
			
			if (x < 0 || x >= size || y < 0 || y >=size)
				return;
			
			if (args.Event.State == Gdk.ModifierType.Button1Mask) {
				// first and last control point cannot be removed
				if (last_cpx != 0 && last_cpx != size - 1) {
					foreach (var controlPoints in GetActiveControlPoints ()) {
						if (controlPoints.ContainsKey (last_cpx))
							controlPoints.Remove (last_cpx);
					}
				}	
				
				AddControlPoint (x, y);
			}
			
			InvalidateDrawing ();	
		}
		
		private void HandleDrawingButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			int x, y;
			Gdk.ModifierType mask;
			drawing.GdkWindow.GetPointer (out x, out y, out mask); 
			
			if (args.Event.Button == 1) {
				AddControlPoint (x, y);
			} 
			
			// user pressed right button
			if (args.Event.Button == 3) {
				foreach (var controlPoints in GetActiveControlPoints ()) {
					for (int i = 0; i < controlPoints.Count; i++) {
						int cpx = controlPoints.Keys [i];
						int cpy = size - 1 - (int)controlPoints.Values [i];
					
						//we cannot allow user to remove first or last control point
						if (cpx == 0 && cpy == size - 1)
							continue;	
						if (cpx == size -1 && cpy == 0)
							continue;
						    
						if (CheckControlPointProximity (cpx, cpy, x, y)) {
							controlPoints.RemoveAt (i);
							break;
						}
					}
				}
			}
			
			InvalidateDrawing();
		}

		private void DrawBorder (Context g)
		{
			g.Rectangle (0, 0, size - 1, size - 1);
			g.LineWidth = 1;
			g.Stroke ();
		}
		
		private void DrawPointerCross (Context g)
		{
			int x, y;
			Gdk.ModifierType mask;
			drawing.GdkWindow.GetPointer (out x, out y, out mask); 
			
			if (x >= 0 && x < size && y >= 0 && y < size) {
				g.LineWidth = 0.5;
				g.MoveTo (x, 0);
				g.LineTo (x, size);
				g.MoveTo (0, y);
				g.LineTo (size , y);
				g.Stroke ();
				
				this.labelPoint.Text = string.Format ("({0}, {1})", x, y);
			} else
				this.labelPoint.Text = string.Empty;
		}
		
		private void DrawGrid (Context g)
		{
			g.SetSourceColor (new Color (0.05, 0.05, 0.05));
			g.SetDash (new double[] {4, 4}, 2);
			g.LineWidth = 1;
			
			for (int i = 1; i < 4; i++) {
				g.MoveTo (i * size / 4, 0);
				g.LineTo (i * size / 4, size);
				g.MoveTo (0, i * size / 4);
				g.LineTo (size, i * size / 4);
			}
			
			g.MoveTo (0, size - 1);
			g.LineTo (size - 1, 0);
			g.Stroke ();
			
			g.SetDash (new double[] {}, 0);
		}
		
		//cpx, cpyx - control point's x and y coordinates
		private bool CheckControlPointProximity (int cpx, int cpy, int x, int y)
		{
			return (Math.Sqrt (Math.Pow (cpx - x, 2) + Math.Pow (cpy - y, 2)) < radius);
		}
		
		private IEnumerator<ControlPointDrawingInfo> GetDrawingInfos ()
		{
			if (Mode == ColorTransferMode.Luminosity)
				yield return new ControlPointDrawingInfo () { 
					Color = new Color (0.4, 0.4, 0.4), IsActive = true 
				};
			
			else {
				yield return new ControlPointDrawingInfo () {
					Color = new Color (0.9, 0, 0), IsActive = checkRed.Active
				};
				yield return new ControlPointDrawingInfo () {
					Color = new Color (0, 0.9, 0), IsActive = checkGreen.Active
				};
				yield return new ControlPointDrawingInfo () {
					Color = new Color(0, 0, 0.9), IsActive = checkBlue.Active
				};
			}
		}
		
		private void DrawControlPoints (Context g)
		{
			int x, y;
			Gdk.ModifierType mask;
			drawing.GdkWindow.GetPointer (out x, out y, out mask); 
			
			var infos = GetDrawingInfos ();
			
			foreach (var controlPoints in ControlPoints) {
				
				infos.MoveNext ();
				var info = infos.Current;
				
				for (int i = 0; i < controlPoints.Count; i++) {
					int cpx = controlPoints.Keys [i];
					int cpy = size - 1 - (int)controlPoints.Values [i];			
					Rectangle rect;
	
					if (info.IsActive)  {
						if (CheckControlPointProximity (cpx, cpy, x, y)) {
							rect = new Rectangle (cpx - (radius + 2) / 2, cpy - (radius + 2) / 2, radius + 2, radius + 2);
							g.DrawEllipse (rect, new Color (0.2, 0.2, 0.2), 2);
							rect = new Rectangle (cpx - radius / 2, cpy - radius / 2, radius, radius);
							g.FillEllipse (rect, new Color (0.9, 0.9, 0.9));
						} else {
							rect = new Rectangle (cpx - radius / 2, cpy - radius / 2, radius, radius);
							g.DrawEllipse (rect, info.Color, 2);
						}
					}
					
					rect = new Rectangle (cpx - (radius - 2) / 2, cpy - (radius - 2) / 2, radius - 2, radius -2);
					g.FillEllipse (rect, info.Color);
				}
			}
				
			g.Stroke ();
		}
			
		private void DrawSpline (Context g)
		{
			var infos = GetDrawingInfos ();
			
			foreach (var controlPoints in ControlPoints) {
			
				int points = controlPoints.Count;
				SplineInterpolator interpolator = new SplineInterpolator ();
				IList<int> xa = controlPoints.Keys;
				IList<int> ya = controlPoints.Values;
				PointD[] line = new PointD [size];
				
				for (int i = 0; i < points; i++) {
					interpolator.Add (xa [i], ya [i]);
				}
				
				for (int i = 0; i < line.Length; i++) {
	               			line[i].X = (float)i;
	                		line[i].Y = (float)(Utility.Clamp(size - 1 - interpolator.Interpolate (i), 0, size - 1));
					
	            		}
				
				g.LineWidth = 2;
				g.LineJoin = LineJoin.Round;
				
				g.MoveTo (line [0]);		
				for (int i = 1; i < line.Length; i++)
					g.LineTo (line [i]);
				
				infos.MoveNext ();
				var info = infos.Current;
					
				g.SetSourceColor (info.Color);
				g.LineWidth = info.IsActive ? 2 : 1;
				g.Stroke ();
			}
		}
		
		private void HandleDrawingExposeEvent (object o, Gtk.ExposeEventArgs args)
		{	
			using (Context g = Gdk.CairoHelper.Create (drawing.GdkWindow)) {
				
				DrawBorder (g);
				DrawPointerCross (g);
				DrawSpline (g);
				DrawGrid (g);
				DrawControlPoints (g);
			}
		}

		private void Build ()
        {
			WindowPosition = WindowPosition.CenterOnParent;
			Resizable = false;
			AllowGrow = false;

			const int spacing = 6;
			var hbox1 = new HBox () { Spacing = spacing };
			hbox1.PackStart (new Label (Catalog.GetString ("Transfer Map")), false, false, 0);
			hbox1.PackStart (new HSeparator (), true, true, 0);
			VBox.PackStart (hbox1, false, false, 0);

			var hbox2 = new HBox () { Spacing = spacing };
			comboMap = ComboBox.NewText ();
			comboMap.AppendText (Catalog.GetString ("RGB"));
			comboMap.AppendText (Catalog.GetString ("Luminosity"));
			comboMap.Active = 1;
			hbox2.PackStart (comboMap, false, false, 0);

			labelPoint = new Label ("(256, 256)");
			var labelAlign = new Alignment (1, 0.5f, 1, 0);
			labelAlign.Add (labelPoint);
			hbox2.PackEnd (labelAlign, false, false, 0);
			VBox.PackStart (hbox2, false, false, 0);

			drawing = new DrawingArea () {
				WidthRequest = 256,
				HeightRequest = 256,
				Events = (Gdk.EventMask)795646,
				CanFocus = true
			};
			VBox.PackStart (drawing, false, false, 8);

			var hbox3 = new HBox ();
			checkRed = new CheckButton (Catalog.GetString ("Red  ")) { Active = true };
			checkGreen = new CheckButton (Catalog.GetString ("Green")) { Active = true };
			checkBlue = new CheckButton (Catalog.GetString ("Blue ")) { Active = true };
			hbox3.PackStart (checkRed, false, false, 0);
			hbox3.PackStart (checkGreen, false, false, 0);
			hbox3.PackStart (checkBlue, false, false, 0);

			buttonReset = new Button () {
				WidthRequest = 81,
				HeightRequest = 30,
				Label = Catalog.GetString ("Reset")
			};
			hbox3.PackEnd (buttonReset, false, false, 0);
			VBox.PackStart (hbox3, false, false, 0);

			labelTip = new Label (Catalog.GetString ("Tip: Right-click to remove control points."));
			VBox.PackStart (labelTip, false, false, 0);

			VBox.ShowAll ();
			checkRed.Hide ();
			checkGreen.Hide ();
			checkBlue.Hide ();
		}
	}
}
