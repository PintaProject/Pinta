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
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	public enum eGradientType
	{
		Linear,
		LinearReflected,
		Diamond,
		Radial,
		Conical
	}

	//[System.ComponentModel.Composition.Export (typeof (BaseTool))]
	public class GradientTool : BaseTool
	{
		Cairo.PointD startpoint;
		bool tracking;
		protected ImageSurface undo_surface;
		uint button;

		static GradientTool ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Toolbar.LinearGradient.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.LinearGradient.png")));
			fact.Add ("Toolbar.LinearReflectedGradient.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.LinearReflectedGradient.png")));
			fact.Add ("Toolbar.DiamondGradient.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.DiamondGradient.png")));
			fact.Add ("Toolbar.RadialGradient.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.RadialGradient.png")));
			fact.Add ("Toolbar.ConicalGradient.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.ConicalGradient.png")));
			fact.Add ("Toolbar.ColorMode.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.ColorMode.png")));
			fact.Add ("Toolbar.TransparentMode.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.TransparentMode.png")));
			fact.AddDefault ();
		}

		public override string Name {
			get { return Catalog.GetString ("Gradient"); }
		}

		public override string Icon {
			get { return "Tools.Gradient.png"; }
		}

		public override string StatusBarText {
			get { return Catalog.GetString ("Click and drag to draw gradient from primary to secondary color.  Right click to reverse."); }
		}
		
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.G; } }
		protected override bool ShowAlphaBlendingButton { get { return true; } }
		public override int Priority { get { return 23; } }

		#region mouse
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			// Protect against history corruption
			if (tracking)
				return;
		
			base.OnMouseDown (canvas, args, point);
			startpoint = point;
			tracking = true;
			button = args.Event.Button;
			undo_surface = doc.CurrentLayer.Surface.Clone ();
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			if (!tracking || args.Event.Button != button)
				return;
		
			base.OnMouseUp (canvas, args, point);
			tracking = false;
			doc.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, doc.CurrentLayerIndex));
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			base.OnMouseMove (o, args, point);
			if (tracking) {
				
				UserBlendOps.NormalBlendOp normalBlendOp = new UserBlendOps.NormalBlendOp();
				GradientRenderer gr = null;
				switch (GradientType) {
					case eGradientType.Linear:
						gr = new GradientRenderers.LinearClamped (GradientColorMode  == GradientColorMode.Transparency, normalBlendOp);
					break;
					case eGradientType.LinearReflected:
						gr = new GradientRenderers.LinearReflected (GradientColorMode  == GradientColorMode.Transparency, normalBlendOp);
					break;
					case eGradientType.Radial:
						gr = new GradientRenderers.Radial (GradientColorMode  == GradientColorMode.Transparency, normalBlendOp);
					break;
					case eGradientType.Diamond:
						gr = new GradientRenderers.LinearDiamond (GradientColorMode  == GradientColorMode.Transparency, normalBlendOp);
					break;
					case eGradientType.Conical:
						gr = new GradientRenderers.Conical (GradientColorMode  == GradientColorMode.Transparency, normalBlendOp);
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

				Gdk.Rectangle selection_bounds = doc.SelectionPath.GetBounds ();
				ImageSurface scratch_layer = doc.ToolLayer.Surface;

				gr.Render (scratch_layer, new Gdk.Rectangle[] { selection_bounds });

				using (var g = doc.CreateClippedContext ()) {
					g.SetSource (scratch_layer);
					g.Paint ();
				}

				selection_bounds.Inflate (5, 5);
				doc.Workspace.Invalidate (selection_bounds);
			}
		}
		#endregion

		#region toolbar
		private ToolBarLabel gradient_label;
		private ToolBarDropDownButton gradient_button;
		private ToolBarLabel mode_label;
		private ToolBarDropDownButton mode_button;
		
		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			if (gradient_label == null)
				gradient_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Gradient")));

			tb.AppendItem (gradient_label);

			if (gradient_button == null) {
				gradient_button = new ToolBarDropDownButton ();

				gradient_button.AddItem (Catalog.GetString ("Linear Gradient"), "Toolbar.LinearGradient.png", eGradientType.Linear);
				gradient_button.AddItem (Catalog.GetString ("Linear Reflected Gradient"), "Toolbar.LinearReflectedGradient.png", eGradientType.LinearReflected);
				gradient_button.AddItem (Catalog.GetString ("Linear Diamond Gradient"), "Toolbar.DiamondGradient.png", eGradientType.Diamond);
				gradient_button.AddItem (Catalog.GetString ("Radial Gradient"), "Toolbar.RadialGradient.png", eGradientType.Radial);
				gradient_button.AddItem (Catalog.GetString ("Conical Gradient"), "Toolbar.ConicalGradient.png", eGradientType.Conical);
			}

			tb.AppendItem (gradient_button);
			
			tb.AppendItem (new Gtk.SeparatorToolItem ());

			if (mode_label == null)
				mode_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Mode")));

			tb.AppendItem (mode_label);

			if (mode_button == null) {
				mode_button = new ToolBarDropDownButton ();

				mode_button.AddItem (Catalog.GetString ("Color Mode"), "Toolbar.ColorMode.png", GradientColorMode.Color);
				mode_button.AddItem (Catalog.GetString ("Transparency Mode"), "Toolbar.TransparentMode.png", GradientColorMode.Transparency);
			}

			tb.AppendItem (mode_button);
		}
		
		public eGradientType GradientType {
			get { return (eGradientType)gradient_button.SelectedItem.Tag; }
		}
	
		public GradientColorMode GradientColorMode {
			get { return (GradientColorMode)gradient_button.SelectedItem.Tag; }
		}
		#endregion
	}
}
