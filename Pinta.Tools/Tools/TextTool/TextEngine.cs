/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
//                     Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using Gdk;
using Pinta.Core;

namespace Pinta.Tools
{
	class TextEngine
	{
		private Point origin;
		private Pango.Layout layout;

		private List<string> lines;
		private int linePos;
		private int textPos;
		bool underline;

		public TextEngine ()
		{
			lines = new List<string> ();

			layout = new Pango.Layout (PintaCore.Chrome.Canvas.PangoContext);
		}

		#region Public Properties
		public Position CurrentPosition {
			get { return new Position (linePos, textPos); }
		}

		public EditingMode EditMode {
			get {
				if (lines.Count == 1 && lines[0] == string.Empty)
					return EditingMode.EmptyEdit;

				return EditingMode.Editing;
			}
		}

		public int FontHeight { get { return GetCursorLocation ().Height; } }
		public Pango.Layout Layout { get { return layout; } }
		public int LineCount { get { return lines.Count; } }

		public Point Origin {
			get { return origin; }
			set { origin = value; }
		}
		#endregion

		#region Public Methods
		public void Clear ()
		{
			lines.Clear ();
			lines.Add (string.Empty);

			linePos = 0;
			textPos = 0;
			origin = Point.Zero;

			Recalculate ();
		}

		public Rectangle GetCursorLocation ()
		{
			Pango.Rectangle weak, strong;

			int index = PositionToIndex (CurrentPosition);

			layout.GetCursorPos (index, out strong, out weak);

			int x = Pango.Units.ToPixels (strong.X) + origin.X;
			int y = Pango.Units.ToPixels (strong.Y) + origin.Y;
			int w = Pango.Units.ToPixels (strong.Width);
			int h = Pango.Units.ToPixels (strong.Height);

			return new Rectangle (x, y, w, h);
		}

		public Rectangle GetLayoutBounds ()
		{
			Pango.Rectangle ink, logical;
			layout.GetPixelExtents (out ink, out logical);

			Rectangle r = new Rectangle (ink.X + origin.X, ink.Y + origin.Y, ink.Width, ink.Height);
			return r;
		}

		public Position PointToTextPosition (Point point)
		{
			int index, trailing;
			int x = Pango.Units.FromPixels (point.X - origin.X);
			int y = Pango.Units.FromPixels (point.Y - origin.Y);

			layout.XyToIndex (x, y, out index, out trailing);

			return IndexToPosition (index + trailing);
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();

			foreach (string s in lines)
				sb.AppendLine (s);

			return sb.ToString ();
		}

		public void SetAlignment (TextAlignment alignment)
		{
			switch (alignment) {
				case TextAlignment.Right:
					layout.Alignment = Pango.Alignment.Right;
					break;
				case TextAlignment.Center:
					layout.Alignment = Pango.Alignment.Center;
					break;
				case TextAlignment.Left:
					layout.Alignment = Pango.Alignment.Left;
					break;
			}
		}

		public void SetCursorPosition (Position position)
		{
			linePos = position.Line;
			textPos = position.Offset;
		}

		public void SetFont (string face, int size, bool bold, bool italic, bool underline)
		{
			var font = Pango.FontDescription.FromString (string.Format ("{0} {1}", face, size));

			font.Weight = bold ? Pango.Weight.Bold : Pango.Weight.Normal;
			font.Style = italic ? Pango.Style.Italic : Pango.Style.Normal;

			layout.FontDescription = font;

			this.underline = underline;
			Recalculate ();
		}

		public Point TextPositionToPoint (Position p)
		{
			int index = PositionToIndex (p);

			var rect = layout.IndexToPos (index);

			int x = Pango.Units.ToPixels (rect.X) + origin.X;
			int y = Pango.Units.ToPixels (rect.Y) + origin.Y;

			return new Point (x, y);
		}

		#endregion

		#region Key Handlers
		public void InsertCharIntoString (uint c)
		{
			byte[] bytes = { (byte)c, (byte)(c >> 8), (byte)(c >> 16), (byte)(c >> 24) };
			string unicodeChar = System.Text.Encoding.UTF32.GetString (bytes);

			lines[linePos] = lines[linePos].Insert (textPos, unicodeChar);
			textPos++;
			Recalculate ();
		}

		public void PerformEnter ()
		{
			string currentLine = lines[linePos];

			if (textPos == currentLine.Length) {
				// If we are at the end of a line, insert an empty line at the next line
				lines.Insert (linePos + 1, string.Empty);
			} else {
				lines.Insert (linePos + 1, currentLine.Substring (textPos, currentLine.Length - textPos));
				lines[linePos] = lines[linePos].Substring (0, textPos);
			}

			linePos++;
			textPos = 0;
			Recalculate ();
		}

		public void PerformBackspace ()
		{
			// We're at the beginning of a line and there's
			// a line above us, go to the end of the prior line
			if (textPos == 0 && linePos > 0) {
				int ntp = lines[linePos - 1].Length;

				lines[linePos - 1] = lines[linePos - 1] + lines[linePos];
				lines.RemoveAt (linePos);
				linePos--;
				textPos = ntp;
				Recalculate ();
			} else if (textPos > 0) {
				// We're in the middle of a line, delete the previous character
				string ln = lines[linePos];

				// If we are at the end of a line, we don't need to place a compound string
				if (textPos == ln.Length)
					lines[linePos] = ln.Substring (0, ln.Length - 1);
				else
					lines[linePos] = ln.Substring (0, textPos - 1) + ln.Substring (textPos);

				textPos--;
				Recalculate ();
			}
		}

		public void PerformDelete ()
		{
			// Where are we?!
			if ((linePos == lines.Count - 1) && (textPos == lines[lines.Count - 1].Length)) {
				// The cursor is at the end of the text block
				return;
			} else if (textPos == lines[linePos].Length) {
				// End of a line, must merge strings
				lines[linePos] = lines[linePos] + lines[linePos + 1];
				lines.RemoveAt (linePos + 1);
			} else {
				// Middle of a line somewhere
				lines[linePos] = lines[linePos].Substring (0, textPos) + (lines[linePos]).Substring (textPos + 1);
			}

			Recalculate ();
		}

		public void PerformLeft (bool control)
		{
			if (control) {
				PerformControlLeft ();
				return;
			}

			// Move caret to the left, or to the previous line
			if (textPos > 0)
				textPos--;
			else if (textPos == 0 && linePos > 0) {
				linePos--;
				textPos = lines[linePos].Length;
			}
		}

		public void PerformControlLeft ()
		{
			// Move caret to the left to the beginning of the word/space/etc.
			if (textPos > 0) {
				int ntp = textPos;
				string currentLine = lines[linePos];

				if (System.Char.IsLetterOrDigit (currentLine[ntp - 1])) {
					while (ntp > 0 && (System.Char.IsLetterOrDigit (currentLine[ntp - 1])))
						ntp--;

				} else if (System.Char.IsWhiteSpace (currentLine[ntp - 1])) {
					while (ntp > 0 && (System.Char.IsWhiteSpace (currentLine[ntp - 1])))
						ntp--;

				} else if (ntp > 0 && System.Char.IsPunctuation (currentLine[ntp - 1])) {
					while (ntp > 0 && System.Char.IsPunctuation (currentLine[ntp - 1]))
						ntp--;

				} else {
					ntp--;
				}

				textPos = ntp;
			} else if (textPos == 0 && linePos > 0) {
				linePos--;
				textPos = lines[linePos].Length;
			}
		}

		public void PerformRight (bool control)
		{
			if (control) {
				PerformControlRight ();
				return;
			}

			// Move caret to the right, or to the next line
			if (textPos < lines[linePos].Length) {
				textPos++;
			} else if (textPos == lines[linePos].Length && linePos < lines.Count - 1) {
				linePos++;
				textPos = 0;
			}
		}

		public void PerformControlRight ()
		{
			// Move caret to the right to the end of the word/space/etc.
			if (textPos < lines[linePos].Length) {
				int ntp = textPos;
				string currentLine = lines[linePos];

				if (System.Char.IsLetterOrDigit (currentLine[ntp])) {
					while (ntp < currentLine.Length && (System.Char.IsLetterOrDigit (currentLine[ntp])))
						ntp++;

				} else if (System.Char.IsWhiteSpace (currentLine[ntp])) {
					while (ntp < currentLine.Length && (System.Char.IsWhiteSpace (currentLine[ntp])))
						ntp++;

				} else if (ntp > 0 && System.Char.IsPunctuation (currentLine[ntp])) {
					while (ntp < currentLine.Length && System.Char.IsPunctuation (currentLine[ntp]))
						ntp++;

				} else {
					ntp++;
				}

				textPos = ntp;
			} else if (textPos == lines[linePos].Length && linePos < lines.Count - 1) {
				linePos++;
				textPos = 0;
			}
		}

		public void PerformHome (bool control)
		{
			// For Ctrl-Home, we go to the top line
			if (control)
				linePos = 0;

			// Go to the beginning of the line
			textPos = 0;
		}

		public void PerformEnd (bool control)
		{
			// For Ctrl-End, we go to the last line
			if (control)
				linePos = lines.Count - 1;

			// Go to the end of the line
			textPos = lines[linePos].Length;
		}

		public void PerformUp ()
		{
			// Move to the letter above this one
			Point point = TextPositionToPoint (CurrentPosition);

			point.Y -= FontHeight;

			Position pos = PointToTextPosition (point);
			SetCursorPosition (pos);
		}

		public void PerformDown ()
		{
			if (CurrentPosition.Line == LineCount - 1) {
				// Last line -> don't do squat
			} else {
				// Move to the letter below this one
				Point point = TextPositionToPoint (CurrentPosition);

				point.Y += FontHeight;

				Position pos = PointToTextPosition (point);
				SetCursorPosition (pos);
			}
		}
		#endregion

		#region Private Methods
		private Position IndexToPosition (int index)
		{
			int current = 0;
			int line = 0;
			int offset = 0;

			foreach (string s in lines) {
				// It's past this line, move along
				if (current + s.Length < index) {
					current += s.Length + 1;
					line++;
					continue;
				}

				// It's in this line
				offset = index - current;
				return new Position (line, offset);
			}

			// It's below all of our lines, return the end of the last line
			return new Position (lines.Count - 1, lines[lines.Count - 1].Length);
		}

		private int PositionToIndex (Position p)
		{
			int index = 0;

			for (int i = 0; i < p.Line; i++)
				index += lines[i].Length + 1;

			index += p.Offset;

			return index;
		}

		private void Recalculate ()
		{
			string markup = ToString ();

			if (underline)
				markup = string.Format ("<u>{0}</u>", markup);

			layout.SetMarkup (markup);
		}
		#endregion
	}
}
