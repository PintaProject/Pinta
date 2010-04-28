// 
// GradientTool.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright (c) 2010 Olivier Dufour
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
using Cairo;

namespace Pinta.Core
{
	public enum eGradientType
	{
		Linear,
		LinearReflected,
		Diamond,
		Radial,
		Conical
	}
	
	public enum eGradientColorMode
	{
		Color,
		Transparency
	}

	public class GradientTool : BaseTool
	{
		Cairo.PointD startpoint;
		bool tracking;
		protected ImageSurface undo_surface;
		uint button;

		public override string Name {
			get { return "Gradient"; }
		}

		public override string Icon {
			get { return "Tools.Gradient.png"; }
		}

		public override string StatusBarText {
			get { return "Click and drag to draw gradient from primary to secondary color.  Right click to reverse."; }
		}
		
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.G; } }
		protected override bool ShowAlphaBlendingButton { get { return true; } }
		
		#region mouse
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			base.OnMouseDown (canvas, args, point);
			startpoint = point;
			tracking = true;
			button = args.Event.Button;
			undo_surface = PintaCore.Layers.CurrentLayer.Surface.Clone ();
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			base.OnMouseUp (canvas, args, point);
			tracking = false;
			PintaCore.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, PintaCore.Layers.CurrentLayerIndex));
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			base.OnMouseMove (o, args, point);
			if (tracking) {
				
				UserBlendOps.NormalBlendOp normalBlendOp = new UserBlendOps.NormalBlendOp();
				GradientRenderer gr = null;
				switch (GradientType) {
					case eGradientType.Linear:
						gr = new GradientRenderers.LinearClamped (GradientColorMode  == eGradientColorMode.Transparency, normalBlendOp);
					break;
					case eGradientType.LinearReflected:
						gr = new GradientRenderers.LinearReflected (GradientColorMode  == eGradientColorMode.Transparency, normalBlendOp);
					break;
					case eGradientType.Radial:
						gr = new GradientRenderers.Radial (GradientColorMode  == eGradientColorMode.Transparency, normalBlendOp);
					break;
					case eGradientType.Diamond:
						gr = new GradientRenderers.LinearDiamond (GradientColorMode  == eGradientColorMode.Transparency, normalBlendOp);
					break;
					case eGradientType.Conical:
						gr = new GradientRenderers.Conical (GradientColorMode  == eGradientColorMode.Transparency, normalBlendOp);
					break;
				}
				if (button == 3) {//right
					gr.StartColor = PintaCore.Palette.SecondaryColor.ToColorBgra ();
	            	gr.EndColor = PintaCore.Palette.PrimaryColor.ToColorBgra ();
				}
				else {//1 left
					gr.StartColor = PintaCore.Palette.PrimaryColor.ToColorBgra ();
	            	gr.EndColor = PintaCore.Palette.SecondaryColor.ToColorBgra ();
				}
						
	            gr.StartPoint = startpoint;
	            gr.EndPoint = point;
				gr.AlphaBlending = UseAlphaBlending;
        
				gr.BeforeRender ();
				gr.Render (PintaCore.Layers.CurrentLayer.Surface, new Gdk.Rectangle[] {PintaCore.Layers.SelectionPath.GetBounds ()});
				PintaCore.Workspace.Invalidate ();
			}
		}
		#endregion

		#region toolbar
		private ToolBarToggleButton linear_gradient_btn;
		private ToolBarToggleButton linear_reflected_gradient_btn;
		private ToolBarToggleButton diamond_gradient_btn;
		private ToolBarToggleButton radial_gradient_btn;
		private ToolBarToggleButton conical_gradient_btn;
		
		private ToolBarToggleButton color_mode_gradient_btn;
		private ToolBarToggleButton transparency_mode_gradient_btn;

		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);
			
			if (linear_gradient_btn == null) {
				linear_gradient_btn = new ToolBarToggleButton ("Toolbar.LinearGradient.png", "Linear Gradient", "Linear gradient");
				linear_gradient_btn.Active = true;
				linear_gradient_btn.Toggled += HandleGradientTypeButtonToggled;;
			}
			
			tb.AppendItem (linear_gradient_btn);
			
			if (linear_reflected_gradient_btn == null) {
				linear_reflected_gradient_btn = new ToolBarToggleButton ("Toolbar.LinearReflectedGradient.png", "Linear Reflected Gradient", "Linear reflected gradient");
				linear_reflected_gradient_btn.Toggled += HandleGradientTypeButtonToggled;;
			}
			
			tb.AppendItem (linear_reflected_gradient_btn);
			
			if (diamond_gradient_btn == null) {
				diamond_gradient_btn = new ToolBarToggleButton ("Toolbar.DiamondGradient.png", "Linear Diamond Gradient", "Linear diamond gradient");
				diamond_gradient_btn.Toggled += HandleGradientTypeButtonToggled;;
			}
			
			tb.AppendItem (diamond_gradient_btn);
			
			if (radial_gradient_btn == null) {
				radial_gradient_btn = new ToolBarToggleButton ("Toolbar.RadialGradient.png", "Radial Gradient", "Radial gradient");
				radial_gradient_btn.Toggled += HandleGradientTypeButtonToggled;;
			}
			
			tb.AppendItem (radial_gradient_btn);
			
			if (conical_gradient_btn == null) {
				conical_gradient_btn = new ToolBarToggleButton ("Toolbar.ConicalGradient.png", "Conical Gradient", "Conical gradient");
				conical_gradient_btn.Toggled += HandleGradientTypeButtonToggled;;
			}
			
			tb.AppendItem (conical_gradient_btn);
			
			tb.AppendItem (new Gtk.SeparatorToolItem ());
			
			/***** ColorBgra mode *****/
			//TODO icons!
			if (color_mode_gradient_btn == null) {
				color_mode_gradient_btn = new ToolBarToggleButton ("ColorPalette.SwapIcon.png", "Color Mode", "Color mode");
				color_mode_gradient_btn.Active = true;
				color_mode_gradient_btn.Toggled += HandleGradientColorModeButtonToggled;;
			}
			
			tb.AppendItem (color_mode_gradient_btn);
			
			if (transparency_mode_gradient_btn == null) {
				transparency_mode_gradient_btn = new ToolBarToggleButton ("ColorPalette.SwapIcon.png", "Transparency Mode", "Transparency mode");
				transparency_mode_gradient_btn.Toggled += HandleGradientColorModeButtonToggled;;
			}
			
			tb.AppendItem (transparency_mode_gradient_btn);
		}

		void HandleGradientTypeButtonToggled (object sender, EventArgs e)
		{
			if (((ToolBarToggleButton)sender).Active) {
				if ((ToolBarToggleButton)sender != linear_gradient_btn && linear_gradient_btn.Active)
					linear_gradient_btn.Active = false;
				if ((ToolBarToggleButton)sender != linear_reflected_gradient_btn && linear_reflected_gradient_btn.Active)
					linear_reflected_gradient_btn.Active = false;
				if ((ToolBarToggleButton)sender != diamond_gradient_btn && diamond_gradient_btn.Active)
					diamond_gradient_btn.Active = false;
				if ((ToolBarToggleButton)sender != radial_gradient_btn && radial_gradient_btn.Active)
					radial_gradient_btn.Active = false;
				if ((ToolBarToggleButton)sender != conical_gradient_btn && conical_gradient_btn.Active)
					conical_gradient_btn.Active = false;
			}
			else if (!linear_gradient_btn.Active && !linear_reflected_gradient_btn.Active && !diamond_gradient_btn.Active && !radial_gradient_btn.Active && !conical_gradient_btn.Active)
				((ToolBarToggleButton)sender).Active = true;
		}
		
		void HandleGradientColorModeButtonToggled (object sender, EventArgs e)
		{
			if (((ToolBarToggleButton)sender).Active) {
				if ((ToolBarToggleButton)sender != color_mode_gradient_btn && color_mode_gradient_btn.Active)
					color_mode_gradient_btn.Active = false;
				if ((ToolBarToggleButton)sender != transparency_mode_gradient_btn && transparency_mode_gradient_btn.Active)
					transparency_mode_gradient_btn.Active = false;
			}
			else if (!transparency_mode_gradient_btn.Active && !color_mode_gradient_btn.Active)
				((ToolBarToggleButton)sender).Active = true;
		}
		
		public eGradientType GradientType {
			get {
				if (linear_gradient_btn.Active)
					return eGradientType.Linear; 
				else if (linear_reflected_gradient_btn.Active)
					return eGradientType.LinearReflected;
				else if (diamond_gradient_btn.Active)
					return eGradientType.Diamond;
				else if (radial_gradient_btn.Active)
					return eGradientType.Radial;
				else if (conical_gradient_btn.Active)
					return eGradientType.Conical;
				else
					return eGradientType.Linear;
			}
		}
	
		public eGradientColorMode GradientColorMode {
			get {
				if (color_mode_gradient_btn.Active)
					return eGradientColorMode.Color;
				else if (transparency_mode_gradient_btn.Active)
					return eGradientColorMode.Transparency;
				else
					return eGradientColorMode.Color;
			}
		}
		#endregion
	}
}
