// 
// AdjustmentsActions.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
using System.Threading;
using Cairo;

namespace Pinta.Core
{
	public class AdjustmentsActions
	{
		public Gtk.Action AutoLevel { get; private set; }
		public Gtk.Action BlackAndWhite { get; private set; }
		public Gtk.Action BrightnessContrast { get; private set; }
		public Gtk.Action Curves { get; private set; }
		public Gtk.Action HueSaturation { get; private set; }
		public Gtk.Action InvertColors { get; private set; }
		public Gtk.Action Levels { get; private set; }
		public Gtk.Action Posterize { get; private set; }
		public Gtk.Action Sepia { get; private set; }

		public AdjustmentsActions ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Menu.Adjustments.AutoLevel.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Adjustments.AutoLevel.png")));
			fact.Add ("Menu.Adjustments.BlackAndWhite.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Adjustments.BlackAndWhite.png")));
			fact.Add ("Menu.Adjustments.BrightnessAndContrast.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Adjustments.BrightnessAndContrast.png")));
			fact.Add ("Menu.Adjustments.Curves.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Adjustments.Curves.png")));
			fact.Add ("Menu.Adjustments.HueAndSaturation.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Adjustments.HueAndSaturation.png")));
			fact.Add ("Menu.Adjustments.InvertColors.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Adjustments.InvertColors.png")));
			fact.Add ("Menu.Adjustments.Levels.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Adjustments.Levels.png")));
			fact.Add ("Menu.Adjustments.Posterize.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Adjustments.Posterize.png")));
			fact.Add ("Menu.Adjustments.Sepia.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Adjustments.Sepia.png")));
			fact.AddDefault ();
			
			AutoLevel = new Gtk.Action ("AutoLevel", Mono.Unix.Catalog.GetString ("Auto Level"), null, "Menu.Adjustments.AutoLevel.png");
			BlackAndWhite = new Gtk.Action ("BlackAndWhite", Mono.Unix.Catalog.GetString ("Black and White"), null, "Menu.Adjustments.BlackAndWhite.png");
			BrightnessContrast = new Gtk.Action ("BrightnessContrast", Mono.Unix.Catalog.GetString ("Brightness / Contrast..."), null, "Menu.Adjustments.BrightnessAndContrast.png");
			Curves = new Gtk.Action ("Curves", Mono.Unix.Catalog.GetString ("Curves..."), null, "Menu.Adjustments.Curves.png");
			HueSaturation = new Gtk.Action ("HueSaturation", Mono.Unix.Catalog.GetString ("Hue / Saturation..."), null, "Menu.Adjustments.HueAndSaturation.png");
			InvertColors = new Gtk.Action ("InvertColors", Mono.Unix.Catalog.GetString ("Invert Colors"), null, "Menu.Adjustments.InvertColors.png");
			Levels = new Gtk.Action ("Levels", Mono.Unix.Catalog.GetString ("Levels..."), null, "Menu.Adjustments.Levels.png");
			Posterize = new Gtk.Action ("Posterize", Mono.Unix.Catalog.GetString ("Posterize..."), null, "Menu.Adjustments.Posterize.png");
			Sepia = new Gtk.Action ("Sepia", Mono.Unix.Catalog.GetString ("Sepia"), null, "Menu.Adjustments.Sepia.png");
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Remove (menu.Children[1]);
			
			menu.Append (AutoLevel.CreateAcceleratedMenuItem (Gdk.Key.L, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (BlackAndWhite.CreateAcceleratedMenuItem (Gdk.Key.G, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (BrightnessContrast.CreateAcceleratedMenuItem (Gdk.Key.C, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (Curves.CreateAcceleratedMenuItem (Gdk.Key.M, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (HueSaturation.CreateAcceleratedMenuItem (Gdk.Key.U, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (InvertColors.CreateAcceleratedMenuItem (Gdk.Key.I, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (Levels.CreateAcceleratedMenuItem (Gdk.Key.L, Gdk.ModifierType.ControlMask));
			menu.Append (Posterize.CreateAcceleratedMenuItem (Gdk.Key.P, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (Sepia.CreateAcceleratedMenuItem (Gdk.Key.E, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
		}

		public void RegisterHandlers ()
		{
			Sepia.Activated += HandleSepiaActivated;
			InvertColors.Activated += HandleInvertColorsActivated;
			BlackAndWhite.Activated += HandleBlackAndWhiteActivated;
			AutoLevel.Activated += HandleAutoLevelActivated;
		}
		#endregion

		#region Action Handlers
		private void HandleBlackAndWhiteActivated (object sender, EventArgs e)
		{
			PerformEffect (new BlackAndWhiteEffect ());
		}

		private void HandleInvertColorsActivated (object sender, EventArgs e)
		{
			PerformEffect (new InvertColorsEffect ());
		}

		private void HandleSepiaActivated (object sender, EventArgs e)
		{
			PerformEffect (new SepiaEffect ());
		}
		
		private void HandleAutoLevelActivated (object sender, EventArgs e)
		{
			PerformEffect (new AutoLevelEffect ());
		}
		#endregion

		#region Public Methods
		public bool PerformEffect (BaseEffect effect)
		{
			PintaCore.Layers.FinishSelection ();

			if (effect.IsConfigurable) {
				bool result = effect.LaunchConfiguration ();
				
				if (!result)
					return false;
			}
			
			SimpleHistoryItem hist = new SimpleHistoryItem (effect.Icon, effect.Text);
			hist.TakeSnapshotOfLayer (PintaCore.Layers.CurrentLayerIndex);

			// Use the existing ToolLayer instead of creating a new temp layer
			Layer tmp_layer = PintaCore.Layers.ToolLayer;
			tmp_layer.Clear ();
			
			ImageSurface dest = tmp_layer.Surface;

			Gdk.Rectangle roi = PintaCore.Layers.SelectionPath.GetBounds ();
			roi = PintaCore.Workspace.ClampToImageSize (roi);

			if (PintaCore.System.RenderThreads <= 1) {
				effect.RenderEffect (PintaCore.Layers.CurrentLayer.Surface, dest, new Gdk.Rectangle[] { roi });
			} else {
				List<Thread> threads = new List<Thread> ();

				foreach (Gdk.Rectangle rect in SplitRectangle (roi, PintaCore.System.RenderThreads)) {
					Thread t = new Thread (new ParameterizedThreadStart (ParallelRender));
					t.Start (new StateInfo (PintaCore.Layers.CurrentLayer.Surface, dest, effect, rect));
					threads.Add (t);
				}

				foreach (Thread t in threads)
					t.Join ();
			}

			using (Context g = new Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.SetSource (dest);
				g.Paint ();
			}

			PintaCore.Workspace.Invalidate ();
			PintaCore.History.PushNewItem (hist);
			
			return true;
		}
		#endregion

		#region Private Methods
		private void ParallelRender (object stateInfo)
		{
			StateInfo si = (StateInfo)stateInfo;
			si.Effect.RenderEffect (si.Source, si.Destination, new Gdk.Rectangle[] { si.Area });
		}
		
		// Split region of interest rectangle into multiple rectangles
		// to facilitate multi-threaded effects.  We use horizontal rectangles
		// for linear memory access
		private Gdk.Rectangle[] SplitRectangle (Gdk.Rectangle roi, int num)
		{
			if (num < 2)
				return new Gdk.Rectangle[] { roi };
				
			List<Gdk.Rectangle> rects = new List<Gdk.Rectangle> ();
			
			int height = roi.Height / num;
			int total_height = roi.Height;
			
			for (int i = 0; i < num - 1; i++) {
				rects.Add (new Gdk.Rectangle (roi.X, i * height + roi.Top, roi.Width, height));
				total_height -= height;
			}
			
			// Add any remaining height to the last rectangle
			rects.Add (new Gdk.Rectangle (roi.X, (num - 1) * height + roi.Top, roi.Width, total_height));
			
			return rects.ToArray ();
		}

		private class StateInfo
		{
			public ImageSurface Source { get; set; }
			public ImageSurface Destination { get; set; }
			public BaseEffect Effect { get; set; }
			public Gdk.Rectangle Area { get; set; }

			public StateInfo (ImageSurface src, ImageSurface dest, BaseEffect effect, Gdk.Rectangle roi)
			{
				Source = src;
				Destination = dest;
				Effect = effect;
				Area = roi;
			}
		}
		#endregion
	}
}
