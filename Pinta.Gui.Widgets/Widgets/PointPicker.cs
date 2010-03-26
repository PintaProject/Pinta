// 
// PointPicker.cs
//  
// Author:
//       dufoli <${AuthorEmail}>
// 
// Copyright (c) 2010 dufoli
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
using System.ComponentModel;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PointPicker : Gtk.Bin
	{

		[Category("Custom Properties")]
		public string Label {
			get { return label.Text; }
			set { label.Text = value; }
		}

		[Category("Custom Properties")]
		public Gdk.Point DefaultPoint { get; set; }

		[Category("Custom Properties")]
		public Gdk.Point Point {
			get { return new Gdk.Point(spinX.ValueAsInt, spinY.ValueAsInt); }
			set {
				if (value.X != spinX.ValueAsInt || value.Y != spinY.ValueAsInt) {
					spinX.Value = value.X;
					spinY.Value = value.Y;
					OnPointPicked ();
				}
			}
		}
		
		//[Category("Custom Properties")]
		//public Gdk.Rectangle Bound  { get; set; }
		
		public PointPicker ()
		{
			this.Build ();
			spinX.ValueChanged += HandleSpinXValueChanged;
			spinY.ValueChanged += HandleSpinYValueChanged;
			//TODO : made a surface area with a thundmnail of the full image and place a cursor to show the point position
			//drawingarea1.
		}
		
		private void HandleSpinXValueChanged (object sender, EventArgs e)
		{
			/*if (spinX.Value < Bound.Left)
				spinX.Value = Bound.Left;
			else if (spinX.Value > Bound.Right)
				spinX.Value = Bound.Right;
			else {*/
				//TODO Update drawingArea
				OnPointPicked ();
			//}
		}
		
		private void HandleSpinYValueChanged (object sender, EventArgs e)
		{
			/*if (spinY.Value < Bound.Top)
				spinY.Value = Bound.Top;
			else if (spinY.Value > Bound.Bottom)
				spinY.Value = Bound.Bottom;
			else {*/
				//TODO Update drawingArea
				OnPointPicked ();
			//}
		}
		
		protected override void OnShown ()
		{
			base.OnShown ();
			Point = DefaultPoint;
		}
		
		#region Protected Methods
		protected void OnPointPicked ()
		{
			if (PointPicked != null)
				PointPicked (this, EventArgs.Empty);
		}
		#endregion

		#region Public Events
		public event EventHandler PointPicked;
		#endregion
		
	}
}
