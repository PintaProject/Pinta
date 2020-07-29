//  AboutDialog.cs
//
// Author:
//   Todd Berman  <tberman@sevenl.net>
//   John Luke  <jluke@cfl.rr.com>
//   Lluis Sanchez Gual  <lluis@novell.com>
//   Viktoria Dudka  <viktoriad@remobjects.com>
//
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2004 John Luke
// Copyright (C) 2008 Novell, Inc.
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
//
//

using System;
using System.Text;

using Gdk;
using Gtk;
using GLib;
using Pango;
using System.IO;
using Mono.Unix;
using Pinta.Core;

namespace Pinta
{
	internal class ScrollBox : DrawingArea
	{
		Pixbuf image;
		Pixbuf image_top;
		Pixbuf monoPowered;
		int scroll;
		Pango.Layout layout;
		int monoLogoSpacing = 5;
		int textTop;
		int scrollPause;
		int scrollStart;
		Gdk.GC backGc;

		internal uint TimerHandle;

		string[] authors = new string[] {
            "Cameron White (@cameronwhite)",
            "Jonathan Pobst (@jpobst)",
            "Robert Nordan (@robpvn)",
            "A. Karon @akaro2424",
            "Alberto Fanjul (@albfan)",
            "Andrija Rajter (@rajter)",
            "André Veríssimo (@averissimo)",
            "Dan Dascalescu (@dandv)",
            "Don McComb (@don-mccomb)",
            "Jared Kells (@jkells)",
            "Jennifer Nguyen (@jeneira94)",
            "Jeremy Burns (@jaburns)",
            "Julian Ospald (@hasufell)",
            "Matthias Mailänder (@Mailaender)",
            "Miguel Fazenda (@miguelfazenda)",
            "Romain Racamier (@Shuunen)",
            "Stefan Moebius (@codeprof)",
            "@aivel",
            "@anadvu",
            "@scx",
            "@skkestrel",
            "@tdaffin"
		};

		string[] oldAuthors = new string[] {
			"Aaron Bockover",
			"Adam Doppelt",
			"Adolfo Jayme Barrientos",
			"Akshara Proddatoori",
			"Andrew Davis",
			"Anirudh Sanjeev",
			"Balló György",
			"Cameron White (@cameronwhite)",
			"Ciprian Mustiata",
			"David Nabraczky",
			"Don McComb (@don-mccomb)",
			"Elvis Alistar",
			"Felix Schmutz",
			"Greg Lowe",
			"Hanh Pham",
			"James Gifford",
			"Jean-Michel Bea",
			"Joe Hillenbrand",
			"John Burak",
			"Jon Rimmer",
			"Jonathan Bergknoff",
			"Jonathan Pobst (@jpobst)",
			"Juergen Obernolte",
			"Khairuddin Ni'am",
			"Krzysztof Marecki",
			"Maia Kozheva",
			"Manish Sinha",
			"Marco Rolappe",
            "Marius Ungureanu",
			"Martin Geier",
			"Mathias Fussenegger",
			"Mikhail Makarov",
			"Obinou Conseil",
			"Olivier Dufour",
			"Richard Cohn",
			"Robert Nordan (@robpvn)",
			"Tom Kadwill"
		};

		public ScrollBox ()
		{
			this.Realized += new EventHandler (OnRealized);
			this.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (49, 49, 74));

			image = PintaCore.Resources.GetIcon ("About.Image.png");
			image_top = PintaCore.Resources.GetIcon ("About.ImageTop.png");
			monoPowered = PintaCore.Resources.GetIcon ("About.MonoPowered.png");

			this.SetSizeRequest (400, image.Height - 1);

			TimerHandle = GLib.Timeout.Add (50, new TimeoutHandler (ScrollDown));
		}

		string CreditText {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.AppendFormat ("<b>{0}</b>\n\n", Catalog.GetString ("Contributors to this release:"));

				for (int n = 0; n < authors.Length; n++) {
					sb.Append (authors[n]);
					if (n % 2 == 1)
						sb.Append ("\n");
					else if (n < authors.Length - 1)
						sb.Append (", ");
				}

				sb.AppendLine ();

				sb.Append ("\n\n<b>" + Catalog.GetString ("Previous contributors:") + "</b>\n\n");
				for (int n = 0; n < oldAuthors.Length; n++) {
					sb.Append (oldAuthors[n]);
					if (n % 2 == 1)
						sb.Append ("\n");
					else if (n < oldAuthors.Length - 1)
						sb.Append (", ");
				}

				sb.AppendLine ();

				string trans = Catalog.GetString ("translator-credits");

				if (trans != "translator-credits") {
					sb.AppendFormat ("\n\n<b>{0}</b>\n\n", Catalog.GetString ("Translated by:"));
					sb.Append (trans);
				}

				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendFormat ("<b>{0}</b>\n", Catalog.GetString ("Based on the work of Paint.NET:"));
				sb.AppendLine ();
				sb.Append ("http://www.getpaint.net/");

				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendFormat ("<b>{0}</b>\n", Catalog.GetString ("Using some icons from:"));
				sb.AppendLine ();
				sb.AppendLine ("Silk - http://www.famfamfam.com/lab/icons/silk");
				sb.Append ("Fugue - http://pinvoke.com/");

				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendFormat ("<b>{0}</b>\n", Catalog.GetString ("Powered by Mono:"));

				return sb.ToString ();
			}
		}

		bool ScrollDown ()
		{
			//if (scrollPause > 0) {
			//        if (--scrollPause == 0)
			//                ++scroll;
			//} else
				++scroll;
			int w, h;
			this.GdkWindow.GetSize (out w, out h);
			this.QueueDrawArea (0, 0, w, image.Height);
			return true;
		}

		private void DrawImage ()
		{
			if (image != null) {
				int w, h;
				this.GdkWindow.GetSize (out w, out h);
				this.GdkWindow.DrawPixbuf (backGc, image, 0, 0, (w - image.Width) / 2, 0, -1, -1, RgbDither.Normal, 0,
				0);
			}
		}

		private void DrawImageTop ()
		{
			if (image_top != null) {
				int w, h;
				this.GdkWindow.GetSize (out w, out h);
				this.GdkWindow.DrawPixbuf (backGc, image_top, 0, 0, (w - image.Width) / 2, 0, -1, -1, RgbDither.Normal, 0,
				0);
			}
		}

		private void DrawText ()
		{
			int width, height;
			GdkWindow.GetSize (out width, out height);

			int widthPixel, heightPixel;
			layout.GetPixelSize (out widthPixel, out heightPixel);

			GdkWindow.DrawLayout (Style.WhiteGC, 0, textTop - scroll, layout);
			GdkWindow.DrawPixbuf (backGc, monoPowered, 0, 0, (width / 2) - (monoPowered.Width / 2), textTop - scroll + heightPixel + monoLogoSpacing, -1, -1, RgbDither.Normal, 0, 0);

			heightPixel = heightPixel - 80 + image.Height;

			if ((scroll == heightPixel) && (scrollPause == 0))
				scrollPause = 60;
			if (scroll > heightPixel + monoLogoSpacing + monoPowered.Height + 200)
				scroll = scrollStart;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			int w, h;

			this.GdkWindow.GetSize (out w, out h);
			this.DrawImage ();
			this.DrawText ();
			this.DrawImageTop ();
			
			return false;
		}

		protected void OnRealized (object o, EventArgs args)
		{
			int x, y;
			int w, h;
			GdkWindow.GetOrigin (out x, out y);
			GdkWindow.GetSize (out w, out h);

			textTop = y + image.Height - 30;
			scrollStart = -(image.Height - textTop);
			scroll = scrollStart;

			layout = new Pango.Layout (this.PangoContext);
			// FIXME: this seems wrong but works
			layout.Width = w * (int)Pango.Scale.PangoScale;
			layout.Wrap = Pango.WrapMode.Word;
			layout.Alignment = Pango.Alignment.Center;
			layout.SetMarkup (CreditText);

			backGc = new Gdk.GC (GdkWindow);
			backGc.RgbBgColor = new Gdk.Color (49, 49, 74);
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			backGc.Dispose ();
		}

	}

	internal class AboutDialog : Dialog
	{
		ScrollBox aboutPictureScrollBox;
		Pixbuf imageSep;

		public AboutDialog () : base (string.Empty, PintaCore.Chrome.MainWindow, DialogFlags.Modal)
		{
			Title = Catalog.GetString ("About Pinta");
			//TransientFor = IdeApp.Workbench.RootWindow;
			AllowGrow = false;
			HasSeparator = false;
			Icon = PintaCore.Resources.GetIcon ("Pinta.png");

			VBox.BorderWidth = 0;

			aboutPictureScrollBox = new ScrollBox ();

			VBox.PackStart (aboutPictureScrollBox, false, false, 0);
			imageSep = PintaCore.Resources.GetIcon ("About.ImageSep.png");

			VBox.PackStart (new Gtk.Image (imageSep), false, false, 0);

			Notebook notebook = new Notebook ();
			notebook.BorderWidth = 6;
			notebook.AppendPage (new AboutPintaTabPage (), new Label (Title));
			notebook.AppendPage (new VersionInformationTabPage (), new Label (Catalog.GetString ("Version Info")));
			
			VBox.PackStart (notebook, true, true, 4);

			AddButton (Gtk.Stock.Close, (int)ResponseType.Close);

			ShowAll ();
		}

		void ChangeColor (Gtk.Widget w)
		{
			w.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (69, 69, 94));
			w.ModifyBg (Gtk.StateType.Active, new Gdk.Color (69, 69, 94));
			w.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 255));
			w.ModifyFg (Gtk.StateType.Active, new Gdk.Color (255, 255, 255));
			w.ModifyFg (Gtk.StateType.Prelight, new Gdk.Color (255, 255, 255));

			Gtk.Container c = w as Gtk.Container;

			if (c != null) {
				foreach (Widget cw in c.Children)
					ChangeColor (cw);
			}
		}

		public new int Run ()
		{
			int tmp = base.Run ();
			GLib.Source.Remove (aboutPictureScrollBox.TimerHandle);
			return tmp;
		}
	}
}
