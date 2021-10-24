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
using Pinta.Core;

namespace Pinta
{
	internal class ScrollBox : DrawingArea
	{
		Pixbuf image;
		Pixbuf image_top;
		Pixbuf monoPowered;
		int scroll;
		Pango.Layout layout = null!; // NRT - Set by OnRealized
		int monoLogoSpacing = 5;
		int textTop;
		int scrollPause;
		int scrollStart;

		internal uint TimerHandle;

		string[] authors = new string[] {
            "Cameron White (@cameronwhite)",
            "Jonathan Pobst (@jpobst)",
            "@darkdragon-001",
		};

		string[] oldAuthors = new string[] {
            "A. Karon @akaro2424",
			"Aaron Bockover",
			"Adam Doppelt",
			"Adolfo Jayme Barrientos",
			"Akshara Proddatoori",
            "Alberto Fanjul (@albfan)",
			"Anirudh Sanjeev",
            "Andrija Rajter (@rajter)",
            "André Veríssimo (@averissimo)",
			"Andrew Davis",
			"Balló György (@City-busz)",
			"Cameron White (@cameronwhite)",
			"Ciprian Mustiata",
            "Dan Dascalescu (@dandv)",
			"David Nabraczky",
			"Don McComb (@don-mccomb)",
			"Elvis Alistar",
			"Felix Schmutz",
			"Greg Lowe",
			"Hanh Pham",
			"James Gifford",
            "Jami Kettunen (@JamiKettunen)",
            "Jared Kells (@jkells)",
			"Jean-Michel Bea",
            "Jennifer Nguyen (@jeneira94)",
            "Jeremy Burns (@jaburns)",
			"Joe Hillenbrand",
			"John Burak",
			"Jon Rimmer",
			"Jonathan Bergknoff",
			"Jonathan Pobst (@jpobst)",
			"Juergen Obernolte",
            "Julian Ospald (@hasufell)",
			"Khairuddin Ni'am",
			"Krzysztof Marecki",
			"Maia Kozheva",
			"Manish Sinha",
			"Marco Rolappe",
            "Marius Ungureanu",
			"Martin Geier",
			"Mathias Fussenegger",
            "Matthias Mailänder (@Mailaender)",
            "Miguel Fazenda (@miguelfazenda)",
			"Mikhail Makarov",
            "Mykola Franchuk (@thekolian1996)",
			"Obinou Conseil",
			"Olivier Dufour",
			"Richard Cohn",
			"Robert Nordan (@robpvn)",
            "Romain Racamier (@Shuunen)",
            "Stefan Moebius (@codeprof)",
            "Timon de Groot (@tdgroot)",
			"Tom Kadwill",
            "@aivel",
            "@anadvu",
            "@jefetienne",
            "@nikita-yfh",
            "@pikachuiscool2",
            "@scx",
            "@skkestrel",
            "@tdaffin",
            "@yaminb",
		};

		public ScrollBox ()
		{
			this.Realized += new EventHandler (OnRealized);

			image = PintaCore.Resources.GetIcon ("About.Image.png");
			image_top = PintaCore.Resources.GetIcon ("About.ImageTop.png");
			monoPowered = PintaCore.Resources.GetIcon ("About.MonoPowered.png");

			this.SetSizeRequest (400, image.Height - 1);

			TimerHandle = GLib.Timeout.Add (50, new TimeoutHandler (ScrollDown));
		}

		string CreditText {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.AppendFormat ("<b>{0}</b>\n\n", Translations.GetString ("Contributors to this release:"));

				for (int n = 0; n < authors.Length; n++) {
					sb.Append (authors[n]);
					if (n % 2 == 1)
						sb.Append ("\n");
					else if (n < authors.Length - 1)
						sb.Append (", ");
				}

				sb.AppendLine ();

				sb.Append ("\n\n<b>" + Translations.GetString ("Previous contributors:") + "</b>\n\n");
				for (int n = 0; n < oldAuthors.Length; n++) {
					sb.Append (oldAuthors[n]);
					if (n % 2 == 1)
						sb.Append ("\n");
					else if (n < oldAuthors.Length - 1)
						sb.Append (", ");
				}

				sb.AppendLine ();

				string trans = Translations.GetString ("translator-credits");

				if (trans != "translator-credits") {
					sb.AppendFormat ("\n\n<b>{0}</b>\n\n", Translations.GetString ("Translated by:"));
					sb.Append (trans);
				}

				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendFormat ("<b>{0}</b>\n", Translations.GetString ("Based on the work of Paint.NET:"));
				sb.AppendLine ();
				sb.Append ("http://www.getpaint.net/");

				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendFormat ("<b>{0}</b>\n", Translations.GetString ("Using some icons from:"));
				sb.AppendLine ();
				sb.AppendLine ("Silk - http://www.famfamfam.com/lab/icons/silk");
				sb.Append ("Fugue - http://pinvoke.com/");

				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendFormat ("<b>{0}</b>\n", Translations.GetString ("Powered by Mono:"));

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
			
			this.QueueDrawArea (0, 0, Window.FrameExtents.Width, image.Height);
			return true;
		}

		private void DrawImage (Cairo.Context ctx)
		{
			if (image != null) {
				Gdk.CairoHelper.SetSourcePixbuf(ctx, image, 0, 0);
				ctx.Paint();
			}
		}

		private void DrawImageTop (Cairo.Context ctx)
		{
			if (image != null)
			{
				Gdk.CairoHelper.SetSourcePixbuf(ctx, image_top, 0, 0);
				ctx.Paint();
			}
		}

		private void DrawText (Cairo.Context ctx)
		{
			int width = Window.FrameExtents.Width;
			int height = Window.FrameExtents.Height;

			int widthPixel, heightPixel;
			layout.GetPixelSize(out widthPixel, out heightPixel);

			ctx.SetSourceColor(new Cairo.Color(1, 1, 1));
			ctx.MoveTo(0, textTop - scroll);
			Pango.CairoHelper.ShowLayout(ctx, layout);

			Gdk.CairoHelper.SetSourcePixbuf(ctx, monoPowered, (width / 2) - (monoPowered.Width / 2), textTop - scroll + heightPixel + monoLogoSpacing);
			ctx.Paint();

			heightPixel = heightPixel - 80 + image.Height;

			if ((scroll == heightPixel) && (scrollPause == 0))
				scrollPause = 60;
			if (scroll > heightPixel + monoLogoSpacing + monoPowered.Height + 200)
				scroll = scrollStart;
		}

        protected override bool OnDrawn(Cairo.Context ctx)
		{
			this.DrawImage (ctx);
			this.DrawText (ctx);
			this.DrawImageTop (ctx);
			
			return false;
		}

		protected void OnRealized (object? o, EventArgs args)
		{
			int x, y;
			int w, h;
			Window.GetOrigin (out x, out y);
			w = Window.FrameExtents.Width;
			h = Window.FrameExtents.Height;

			textTop = y + image.Height - 30;
			scrollStart = -(image.Height - textTop);
			scroll = scrollStart;

			layout = new Pango.Layout (this.PangoContext);
			// FIXME: this seems wrong but works
			layout.Width = w * (int)Pango.Scale.PangoScale;
			layout.Wrap = Pango.WrapMode.Word;
			layout.Alignment = Pango.Alignment.Center;
			layout.SetMarkup (CreditText);
		}

	}

	internal class AboutDialog : Dialog
	{
		ScrollBox aboutPictureScrollBox;
		Pixbuf imageSep;

		public AboutDialog () : base (string.Empty, PintaCore.Chrome.MainWindow, DialogFlags.Modal)
		{
			Title = Translations.GetString ("About Pinta");
			//TransientFor = IdeApp.Workbench.RootWindow;
			Resizable = false;
			IconName = Pinta.Resources.Icons.AboutPinta;

			ContentArea.BorderWidth = 0;

			aboutPictureScrollBox = new ScrollBox ();

			ContentArea.PackStart (aboutPictureScrollBox, false, false, 0);
			imageSep = PintaCore.Resources.GetIcon ("About.ImageSep.png");

			ContentArea.PackStart (new Gtk.Image (imageSep), false, false, 0);

			Notebook notebook = new Notebook ();
			notebook.BorderWidth = 6;
			notebook.AppendPage (new AboutPintaTabPage (), new Label (Title));
			notebook.AppendPage (new VersionInformationTabPage (), new Label (Translations.GetString ("Version Info")));

			ContentArea.PackStart (notebook, true, true, 4);

			AddButton (Gtk.Stock.Close, (int)ResponseType.Close);

			this.Resizable = true;

			ShowAll ();
		}

		public new int Run ()
		{
			int tmp = base.Run ();
			GLib.Source.Remove (aboutPictureScrollBox.TimerHandle);
			return tmp;
		}
	}
}
