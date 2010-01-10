using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pinta.Core;
using Cairo;

namespace Pinta
{
	class ColorPickerTool : BaseTool
	{
		private int button_down = 0;
		
		public override string Name
		{
			get { return "Color Picker"; }
		}
		public override string Icon
		{
			get { return "Tools.ColorPicker.png"; }
		}
		public override string StatusBarText
		{
			get { return "Left click to set primary color. Right click to set secondary color."; }
		}
		public override bool Enabled
		{
			get { return true; }
		}

		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			if (args.Event.Button == 1)
				button_down = 1;
			else if (args.Event.Button == 3)
				button_down = 3;

			if (!PintaCore.Workspace.PointInCanvas (point))
				return;
							
			Color color = PintaCore.Layers.CurrentLayer.Surface.GetPixel ((int)point.X, (int)point.Y);

			if (button_down == 1)
				PintaCore.Palette.PrimaryColor = color;
			else if (button_down == 3)
				PintaCore.Palette.SecondaryColor = color;
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, PointD point)
		{
			if (button_down == 0)
				return;
				
			if (!PintaCore.Workspace.PointInCanvas (point))
				return;

			Color color = PintaCore.Layers.CurrentLayer.Surface.GetPixel ((int)point.X, (int)point.Y);

			if (button_down == 1)
				PintaCore.Palette.PrimaryColor = color;
			else if (button_down == 3)
				PintaCore.Palette.SecondaryColor = color;
		}
		
		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, PointD point)
		{
			button_down = 0;
		}
	}
}
