// 
// EffectsActions.cs
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
using Gtk;

namespace Pinta.Core
{
	public class EffectsActions
	{
		public Gtk.Action Artistic { get; private set; }
		public Gtk.Action Blurs { get; private set; }
		public Gtk.Action Distort { get; private set; }
		public Gtk.Action Noise { get; private set; }
		public Gtk.Action Photo { get; private set; }
		public Gtk.Action Render { get; private set; }
		public Gtk.Action Stylize { get; private set; }

		public Gtk.Action InkSketch { get; private set; }
		public Gtk.Action OilPainting { get; private set; }
		public Gtk.Action PencilSketch { get; private set; }
		public Gtk.Action Fragment { get; private set; }
		public Gtk.Action GaussianBlur { get; private set; }
		public Gtk.Action SurfaceBlur { get; private set; }
		public Gtk.Action ZoomBlur { get; private set; }
		public Gtk.Action Unfocus { get; private set; }
		public Gtk.Action RadialBlur { get; private set; }
		public Gtk.Action Bulge { get; private set; }
		public Gtk.Action Dents { get; private set; }
		public Gtk.Action PolarInversion { get; private set; }
		public Gtk.Action MotionBlur { get; private set; }
		public Gtk.Action Twist { get; private set; }
		public Gtk.Action Tile { get; private set; }
		public Gtk.Action FrostedGlass { get; private set; }
		public Gtk.Action Pixelate { get; private set; }
		public Gtk.Action Glow { get; private set; }
		public Gtk.Action RedEyeRemove { get; private set; }
		public Gtk.Action Sharpen { get; private set; }
		public Gtk.Action SoftenPortrait { get; private set; }
		public Gtk.Action Clouds { get; private set; }
		public Gtk.Action JuliaFractal { get; private set; }
		public Gtk.Action MandelbrotFractal { get; private set; }
		public Gtk.Action EdgeDetect { get; private set; }
		public Gtk.Action AddNoise { get; private set; }
		public Gtk.Action Median { get; private set; }
		public Gtk.Action ReduceNoise { get; private set; }
		public Gtk.Action Outline { get; private set; }
		public Gtk.Action Relief { get; private set; }
		public Gtk.Action Emboss { get; private set; }

		public EffectsActions ()
		{
			IconFactory fact = new IconFactory ();
			fact.Add ("Menu.Effects.Artistic.InkSketch.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Artistic.InkSketch.png")));
			fact.Add ("Menu.Effects.Artistic.OilPainting.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Artistic.OilPainting.png")));
			fact.Add ("Menu.Effects.Artistic.PencilSketch.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Artistic.PencilSketch.png")));
			fact.Add ("Menu.Effects.Blurs.Fragment.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Blurs.Fragment.png")));
			fact.Add ("Menu.Effects.Blurs.GaussianBlur.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Blurs.GaussianBlur.png")));
			fact.Add ("Menu.Effects.Blurs.MotionBlur.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Blurs.MotionBlur.png")));
			fact.Add ("Menu.Effects.Blurs.RadialBlur.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Blurs.RadialBlur.png")));
			fact.Add ("Menu.Effects.Blurs.SurfaceBlur.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Blurs.SurfaceBlur.png")));
			fact.Add ("Menu.Effects.Blurs.Unfocus.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Blurs.Unfocus.png")));
			fact.Add ("Menu.Effects.Blurs.ZoomBlur.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Blurs.ZoomBlur.png")));
			fact.Add ("Menu.Effects.Distort.Bulge.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Distort.Bulge.png")));
			fact.Add ("Menu.Effects.Distort.Dents.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Distort.Dents.png")));
			fact.Add ("Menu.Effects.Distort.PolarInversion.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Distort.PolarInversion.png")));
			fact.Add ("Menu.Effects.Distort.Twist.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Distort.Twist.png")));
			fact.Add ("Menu.Effects.Distort.Tile.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Distort.Tile.png")));
			fact.Add ("Menu.Effects.Distort.FrostedGlass.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Distort.FrostedGlass.png")));
			fact.Add ("Menu.Effects.Distort.Pixelate.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Distort.Pixelate.png")));
			fact.Add ("Menu.Effects.Photo.Glow.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Photo.Glow.png")));
			fact.Add ("Menu.Effects.Photo.RedEyeRemove.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Photo.RedEyeRemove.png")));
			fact.Add ("Menu.Effects.Photo.Sharpen.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Photo.Sharpen.png")));
			fact.Add ("Menu.Effects.Photo.SoftenPortrait.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Photo.SoftenPortrait.png")));
			fact.Add ("Menu.Effects.Render.Clouds.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Render.Clouds.png")));
			fact.Add ("Menu.Effects.Render.JuliaFractal.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Render.JuliaFractal.png")));
			fact.Add ("Menu.Effects.Render.MandelbrotFractal.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Render.MandelbrotFractal.png")));
			fact.Add ("Menu.Effects.Noise.AddNoise.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Noise.AddNoise.png")));
			fact.Add ("Menu.Effects.Noise.Median.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Noise.Median.png")));
			fact.Add ("Menu.Effects.Noise.ReduceNoise.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Noise.ReduceNoise.png")));
			fact.Add ("Menu.Effects.Stylize.EdgeDetect.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Stylize.EdgeDetect.png")));
			fact.Add ("Menu.Effects.Stylize.Relief.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Stylize.Relief.png")));
			fact.Add ("Menu.Effects.Stylize.Emboss.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Stylize.Emboss.png")));
			fact.Add ("Menu.Effects.Stylize.Outline.png", new IconSet (PintaCore.Resources.GetIcon ("Menu.Effects.Stylize.Outline.png")));
			
			fact.AddDefault ();
			
			// Submenus
			Artistic = new Gtk.Action ("Artistic", Mono.Unix.Catalog.GetString ("Artistic"), null, null);
			Blurs = new Gtk.Action ("Blurs", Mono.Unix.Catalog.GetString ("Blurs"), null, null);
			Distort = new Gtk.Action ("Distort", Mono.Unix.Catalog.GetString ("Distort"), null, null);
			Noise = new Gtk.Action ("Noise", Mono.Unix.Catalog.GetString ("Noise"), null, null);
			Photo = new Gtk.Action ("Photo", Mono.Unix.Catalog.GetString ("Photo"), null, null);
			Render = new Gtk.Action ("Render", Mono.Unix.Catalog.GetString ("Render"), null, null);
			Stylize = new Gtk.Action ("Stylize", Mono.Unix.Catalog.GetString ("Stylize"), null, null);
			
			// Menu items
			InkSketch = new Gtk.Action ("InkSketch", Mono.Unix.Catalog.GetString ("Ink Sketch..."), null, "Menu.Effects.Artistic.InkSketch.png");
			OilPainting = new Gtk.Action ("OilPainting", Mono.Unix.Catalog.GetString ("Oil Painting..."), null, "Menu.Effects.Artistic.OilPainting.png");
			PencilSketch = new Gtk.Action ("PencilSketch", Mono.Unix.Catalog.GetString ("Pencil Sketch..."), null, "Menu.Effects.Artistic.PencilSketch.png");
			Fragment = new Gtk.Action ("Fragment", Mono.Unix.Catalog.GetString ("Fragment..."), null, "Menu.Effects.Blurs.Fragment.png");
			GaussianBlur = new Gtk.Action ("GaussianBlur", Mono.Unix.Catalog.GetString ("Gaussian Blur..."), null, "Menu.Effects.Blurs.GaussianBlur.png");
			SurfaceBlur = new Gtk.Action ("SurfaceBlur", Mono.Unix.Catalog.GetString ("Surface Blur..."), null, "Menu.Effects.Blurs.SurfaceBlur.png");
			ZoomBlur = new Gtk.Action ("ZoomBlur", Mono.Unix.Catalog.GetString ("Zoom Blur..."), null, "Menu.Effects.Blurs.ZoomBlur.png");
			Unfocus = new Gtk.Action ("Unfocus", Mono.Unix.Catalog.GetString ("Unfocus..."), null, "Menu.Effects.Blurs.Unfocus.png");
			Glow = new Gtk.Action ("Glow", Mono.Unix.Catalog.GetString ("Glow..."), null, "Menu.Effects.Photo.Glow.png");
			RadialBlur = new Gtk.Action ("RadialBlur", Mono.Unix.Catalog.GetString ("Radial Blur..."), null, "Menu.Effects.Blurs.RadialBlur.png");
			Bulge = new Gtk.Action ("Bulge", Mono.Unix.Catalog.GetString ("Bulge..."), null, "Menu.Effects.Distort.Bulge.png");
			Twist = new Gtk.Action ("Twist", Mono.Unix.Catalog.GetString ("Twist..."), null, "Menu.Effects.Distort.Twist.png");
			Tile = new Gtk.Action ("Tile", Mono.Unix.Catalog.GetString ("Tile Reflection..."), null, "Menu.Effects.Distort.Tile.png");
			FrostedGlass = new Gtk.Action ("FrostedGlass", Mono.Unix.Catalog.GetString ("Frosted Glass..."), null, "Menu.Effects.Distort.FrostedGlass.png");
			Pixelate = new Gtk.Action ("Pixelate", Mono.Unix.Catalog.GetString ("Pixelate..."), null, "Menu.Effects.Distort.Pixelate.png");
			Dents = new Gtk.Action ("Dents", Mono.Unix.Catalog.GetString ("Dents..."), null, "Menu.Effects.Distort.Dents.png");
			PolarInversion = new Gtk.Action ("PolarInversion", Mono.Unix.Catalog.GetString ("Polar Inversion..."), null, "Menu.Effects.Distort.PolarInversion.png");
			MotionBlur = new Gtk.Action ("MotionBlur", Mono.Unix.Catalog.GetString ("Motion Blur..."), null, "Menu.Effects.Blurs.MotionBlur.png");
			Glow = new Gtk.Action ("Glow", Mono.Unix.Catalog.GetString ("Glow..."), null, "Menu.Effects.Photo.Glow.png");
			RedEyeRemove = new Gtk.Action ("RedEyeRemove", Mono.Unix.Catalog.GetString ("Red Eye Removal..."), null, "Menu.Effects.Photo.RedEyeRemove.png");
			Sharpen = new Gtk.Action ("Sharpen", Mono.Unix.Catalog.GetString ("Sharpen..."), null, "Menu.Effects.Photo.Sharpen.png");
			SoftenPortrait = new Gtk.Action ("Soften Portrait", Mono.Unix.Catalog.GetString ("Soften Portrait..."), null, "Menu.Effects.Photo.SoftenPortrait.png");
			Clouds = new Gtk.Action ("Clouds", Mono.Unix.Catalog.GetString ("Clouds..."), null, "Menu.Effects.Render.Clouds.png");
			JuliaFractal = new Gtk.Action ("Julia Fractal", Mono.Unix.Catalog.GetString ("Julia Fractal..."), null, "Menu.Effects.Render.JuliaFractal.png");
			MandelbrotFractal = new Gtk.Action ("Mandelbrot Fractal", Mono.Unix.Catalog.GetString ("Mandelbrot Factal..."), null, "Menu.Effects.Render.MandelbrotFractal.png");
			EdgeDetect = new Gtk.Action ("EdgeDetect", Mono.Unix.Catalog.GetString ("Edge Detect..."), null, "Menu.Effects.Stylize.EdgeDetect.png");
			AddNoise = new Gtk.Action ("AddNoise", Mono.Unix.Catalog.GetString ("Add Noise..."), null, "Menu.Effects.Noise.AddNoise.png");
			Median = new Gtk.Action ("Median", Mono.Unix.Catalog.GetString ("Median..."), null, "Menu.Effects.Noise.Median.png");
			Relief = new Gtk.Action ("Relief", Mono.Unix.Catalog.GetString ("Relief..."), null, "Menu.Effects.Stylize.Relief.png");
			Emboss = new Gtk.Action ("Emboss", Mono.Unix.Catalog.GetString ("Emboss..."), null, "Menu.Effects.Stylize.Emboss.png");
			ReduceNoise = new Gtk.Action ("ReduceNoise", Mono.Unix.Catalog.GetString ("Reduce Noise..."), null, "Menu.Effects.Noise.ReduceNoise.png");
			Outline = new Gtk.Action ("Outline", Mono.Unix.Catalog.GetString ("Outline..."), null, "Menu.Effects.Stylize.Outline.png");
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
		    menu.Remove (menu.Children[1]);
			
			// Create Submenus
			Menu artistic_sub_menu = (Menu)menu.AppendItem (Artistic.CreateSubMenuItem ()).Submenu;
			Menu blur_sub_menu = (Menu)menu.AppendItem (Blurs.CreateSubMenuItem ()).Submenu;
			Menu distort_sub_menu = (Menu)menu.AppendItem (Distort.CreateSubMenuItem ()).Submenu;
			Menu noise_sub_menu = (Menu)menu.AppendItem (Noise.CreateSubMenuItem ()).Submenu;
			Menu photo_sub_menu = (Menu)menu.AppendItem (Photo.CreateSubMenuItem ()).Submenu;
			Menu render_sub_menu = (Menu)menu.AppendItem (Render.CreateSubMenuItem ()).Submenu;
			Menu stylize_sub_menu = (Menu)menu.AppendItem (Stylize.CreateSubMenuItem ()).Submenu;
		 
			// Create menu items
			artistic_sub_menu.Append (InkSketch.CreateMenuItem ());
			artistic_sub_menu.Append (OilPainting.CreateMenuItem ());
			artistic_sub_menu.Append (PencilSketch.CreateMenuItem ());
		 
			blur_sub_menu.Append (Fragment.CreateMenuItem ());
			blur_sub_menu.Append (GaussianBlur.CreateMenuItem ());
			blur_sub_menu.Append (SurfaceBlur.CreateMenuItem ());
			blur_sub_menu.Append (Unfocus.CreateMenuItem ());
			blur_sub_menu.Append (ZoomBlur.CreateMenuItem ());
			blur_sub_menu.Append (RadialBlur.CreateMenuItem ());
			blur_sub_menu.Append (MotionBlur.CreateMenuItem ());
			
			distort_sub_menu.Append (Twist.CreateMenuItem ());
			distort_sub_menu.Append (Tile.CreateMenuItem ());
			distort_sub_menu.Append (Pixelate.CreateMenuItem ());
			distort_sub_menu.Append (FrostedGlass.CreateMenuItem ());
			distort_sub_menu.Append (Bulge.CreateMenuItem ());
			//distort_sub_menu.Append (Dents.CreateMenuItem ());
			distort_sub_menu.Append (PolarInversion.CreateMenuItem ());
			
			photo_sub_menu.Append (Glow.CreateMenuItem ());
			photo_sub_menu.Append (RedEyeRemove.CreateMenuItem ());
			photo_sub_menu.Append (Sharpen.CreateMenuItem ());
			photo_sub_menu.Append (SoftenPortrait.CreateMenuItem ());
			
			render_sub_menu.Append (Clouds.CreateMenuItem ());
			render_sub_menu.Append (JuliaFractal.CreateMenuItem ());
			render_sub_menu.Append (MandelbrotFractal.CreateMenuItem ());
			
			noise_sub_menu.Append (AddNoise.CreateMenuItem ());
			noise_sub_menu.Append (Median.CreateMenuItem ());
			noise_sub_menu.Append (ReduceNoise.CreateMenuItem ());

			stylize_sub_menu.Append (EdgeDetect.CreateMenuItem ());
			stylize_sub_menu.Append (Relief.CreateMenuItem ());
			stylize_sub_menu.Append (Emboss.CreateMenuItem ());
			stylize_sub_menu.Append (Outline.CreateMenuItem ());
		}
		#endregion
	}
}
