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
using System.Security;

namespace Pinta.Core
{
	public class TextLayout
	{
		private TextEngine engine = null!; // NRT - Not sure how this is set, but all callers assume it is not-null

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

		public Pango.Layout Layout { get; }
		public int FontHeight { get { return GetCursorLocation ().Height; } }

		public TextLayout ()
		{
			Layout = Pango.Layout.New (PintaCore.Chrome.MainWindow.GetPangoContext ());
		}

		public RectangleI[] SelectionRectangles {
			get {
				var regions = engine.SelectionRegions;
				var rects = new List<RectangleI> ();

				foreach (var region in regions) {
					PointI p1 = TextPositionToPoint (region.Key);
					PointI p2 = TextPositionToPoint (region.Value);
					rects.Add (new RectangleI (p1, new Size (p2.X - p1.X, FontHeight)));
				}

				return rects.ToArray ();
			}
		}

		public RectangleI GetCursorLocation ()
		{
			int index = engine.PositionToUTF8Index (engine.CurrentPosition);

			Layout.GetCursorPos (index, out RectangleI strong, out RectangleI weak);

			int x = PangoExtensions.UnitsToPixels (strong.X) + engine.Origin.X;
			int y = PangoExtensions.UnitsToPixels (strong.Y) + engine.Origin.Y;
			int w = PangoExtensions.UnitsToPixels (strong.Width);
			int h = PangoExtensions.UnitsToPixels (strong.Height);

			return new RectangleI (x, y, w, h);
		}

		public RectangleI GetLayoutBounds ()
		{
			Layout.GetPixelExtents (out RectangleI ink, out RectangleI logical);
			var cursor = GetCursorLocation ();

			// GetPixelExtents() doesn't really return a very sensible height.
			// Instead of doing some hacky arithmetic to correct it, the height will just
			// be the cursor's height times the number of lines.
			return new RectangleI (engine.Origin.X, engine.Origin.Y,
					      ink.Width, cursor.Height * engine.LineCount);
		}

		public TextPosition PointToTextPosition (PointI point)
		{
			int index, trailing;
			int x = PangoExtensions.UnitsFromPixels (point.X - engine.Origin.X);
			int y = PangoExtensions.UnitsFromPixels (point.Y - engine.Origin.Y);

			Layout.XyToIndex (x, y, out index, out trailing);

			return engine.UTF8IndexToPosition (index + trailing);
		}

		public PointI TextPositionToPoint (TextPosition p)
		{
			int index = engine.PositionToUTF8Index (p);

			Layout.IndexToPos (index, out RectangleI rect);

			int x = PangoExtensions.UnitsToPixels (rect.X) + engine.Origin.X;
			int y = PangoExtensions.UnitsToPixels (rect.Y) + engine.Origin.Y;

			return new PointI (x, y);
		}

		private void OnEngineModified (object? sender, EventArgs e)
		{
			string? markup = SecurityElement.Escape (engine.ToString ());

			if (engine.Underline)
				markup = $"<u>{markup}</u>";

			switch (engine.Alignment) {
				case TextAlignment.Right:
					Layout.SetAlignment (Pango.Alignment.Right);
					break;
				case TextAlignment.Center:
					Layout.SetAlignment (Pango.Alignment.Center);
					break;
				case TextAlignment.Left:
					Layout.SetAlignment (Pango.Alignment.Left);
					break;
			}

			Layout.SetFontDescription (engine.Font);

			Layout.SetMarkup (markup, -1);
		}
	}
}
