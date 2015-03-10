// 
// TextLayout.cs
//  
// Author:
//       Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2015 Cameron White
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
using System.Linq;
using System.Text;
using System.Security;
using Gdk;

namespace Pinta.Core
{
    public class TextLayout
    {
        private TextEngine engine;

        public TextEngine Engine {
            get { return engine; }
            set {
                if (engine != null)
                    engine.Modified -= OnEngineModified;
                engine = value;
                engine.Modified += OnEngineModified;
                OnEngineModified (this, EventArgs.Empty);
            }
        }

        public Pango.Layout Layout { get; private set; }
        public int FontHeight { get { return GetCursorLocation ().Height; } }

        public TextLayout ()
        {
            Layout = new Pango.Layout (PintaCore.Chrome.MainWindow.PangoContext);
        }

		public Rectangle[] SelectionRectangles
		{
			get {
                var regions = engine.SelectionRegions;
                List<Rectangle> rects = new List<Rectangle> ();

                foreach (var region in regions)
                {
                    Point p1 = TextPositionToPoint (region.Key);
                    Point p2 = TextPositionToPoint (region.Value);
                    rects.Add (new Rectangle (p1, new Size (p2.X - p1.X, FontHeight)));
                }

                return rects.ToArray ();
			}
		}

		public Rectangle GetCursorLocation ()
		{
			Pango.Rectangle weak, strong;

			int index = engine.PositionToIndex (engine.CurrentPosition);

			Layout.GetCursorPos (index, out strong, out weak);

			int x = Pango.Units.ToPixels (strong.X) + engine.Origin.X;
			int y = Pango.Units.ToPixels (strong.Y) + engine.Origin.Y;
			int w = Pango.Units.ToPixels (strong.Width);
			int h = Pango.Units.ToPixels (strong.Height);

			return new Rectangle (x, y, w, h);
		}

		public Rectangle GetLayoutBounds ()
		{
			Pango.Rectangle ink, logical;
			Layout.GetPixelExtents (out ink, out logical);
			var cursor = GetCursorLocation ();

			// GetPixelExtents() doesn't really return a very sensible height.
			// Instead of doing some hacky arithmetic to correct it, the height will just
			// be the cursor's height times the number of lines.
            return new Rectangle (engine.Origin.X, engine.Origin.Y,
                                  ink.Width, cursor.Height * engine.LineCount);
		}

		public TextPosition PointToTextPosition (Point point)
		{
			int index, trailing;
			int x = Pango.Units.FromPixels (point.X - engine.Origin.X);
			int y = Pango.Units.FromPixels (point.Y - engine.Origin.Y);

			Layout.XyToIndex (x, y, out index, out trailing);

			return engine.IndexToPosition (index + trailing);
		}

		public Point TextPositionToPoint (TextPosition p)
		{
			int index = engine.PositionToIndex (p);

			var rect = Layout.IndexToPos (index);

			int x = Pango.Units.ToPixels (rect.X) + engine.Origin.X;
			int y = Pango.Units.ToPixels (rect.Y) + engine.Origin.Y;

			return new Point (x, y);
		}

        private void OnEngineModified (object sender, EventArgs e)
        {
			string markup = SecurityElement.Escape (engine.ToString ());

			if (engine.Underline)
				markup = string.Format ("<u>{0}</u>", markup);

			switch (engine.Alignment) {
				case TextAlignment.Right:
					Layout.Alignment = Pango.Alignment.Right;
					break;
				case TextAlignment.Center:
					Layout.Alignment = Pango.Alignment.Center;
					break;
				case TextAlignment.Left:
					Layout.Alignment = Pango.Alignment.Left;
					break;
			}

			var font = Pango.FontDescription.FromString (
                string.Format ("{0} {1}", engine.FontFace, engine.FontSize));
			// Forces font variants to be rendered properly
			// (e.g. this will use "Ubuntu Condensed" instead of "Ubuntu").
			font.Family = engine.FontFace;
			font.Weight = engine.Bold ? Pango.Weight.Bold : Pango.Weight.Normal;
			font.Style = engine.Italic ? Pango.Style.Italic : Pango.Style.Normal;
			Layout.FontDescription = font;

			Layout.SetMarkup (markup);
        }
    }
}
