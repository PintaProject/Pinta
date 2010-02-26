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
		
		public Gtk.Action GaussianBlur { get; private set; }

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
			fact.AddDefault ();

			// Submenus
			Artistic = new Gtk.Action ("Artistic", Mono.Unix.Catalog.GetString ("Artistic"), null, null);
			Blurs = new Gtk.Action ("Blurs", Mono.Unix.Catalog.GetString ("Blurs"), null, null);
			Distort = new Gtk.Action ("Distort", Mono.Unix.Catalog.GetString ("Distort"), null, null);
			Noise = new Gtk.Action ("Noise", Mono.Unix.Catalog.GetString ("Noise"), null, null);
			Photo = new Gtk.Action ("Photo", Mono.Unix.Catalog.GetString ("Photo"), null, null);
			Render = new Gtk.Action ("Render", Mono.Unix.Catalog.GetString ("Render"), null, null);
			Stylize = new Gtk.Action ("Stylize", Mono.Unix.Catalog.GetString ("Stylize"), null, null);

			Artistic.Visible = false;
			Distort.Visible = false;
			Noise.Visible = false;
			Photo.Visible = false;
			Render.Visible = false;
			Stylize.Visible = false;
			
			// Menu items
			GaussianBlur = new Gtk.Action ("GaussianBlur", Mono.Unix.Catalog.GetString ("Gaussian Blur..."), null, "Menu.Effects.Blurs.GaussianBlur.png");

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
			blur_sub_menu.Append (GaussianBlur.CreateMenuItem ());
		}
		#endregion
	}
}
