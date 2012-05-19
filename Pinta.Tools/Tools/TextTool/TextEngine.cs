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
using System.Security;

namespace Pinta.Tools
{
	class TextEngine
	{
		private Point origin;
		private Pango.Layout layout;

		private List<string> lines;
		private int linePos;
		private int textPos;
		// Relative coordonate of selection
		private int selectionRelativeIndex = 0;
		bool underline;
		Gtk.IMMulticontext imContext;

		public TextEngine ()
		{
			lines = new List<string> ();

			layout = new Pango.Layout (PintaCore.Chrome.Canvas.PangoContext);
			imContext = new Gtk.IMMulticontext ();
			imContext.Commit += OnCommit;

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

		public Rectangle[] SelectionRectangles
		{
			get {
				List<Rectangle> rects = new List<Rectangle> ();
				Point p1, p2;

				if (selectionRelativeIndex > 0) {
					p1 = TextPositionToPoint (new Position (linePos, textPos));

					ForeachLine (linePos, textPos, selectionRelativeIndex, (currentLinePos, strpos, endpos) =>
						{
					        p2 = TextPositionToPoint (new Position (currentLinePos , endpos));
							rects.Add (new Rectangle (p1, new Size (p2.X - p1.X, FontHeight)));
							if (currentLinePos + 1 < lines.Count)
								p1 = TextPositionToPoint (new Position (currentLinePos + 1, 0));
					             });
					return rects.ToArray ();

				} else if (selectionRelativeIndex < 0) {
					Position mypos = IndexToPosition (PositionToIndex (new Position (linePos, textPos)) + selectionRelativeIndex);
					p1 = TextPositionToPoint (mypos);
					ForeachLine (mypos.Line, mypos.Offset, -selectionRelativeIndex, (currentLinePos, strpos, endpos) =>
						{
					        p2 = TextPositionToPoint (new Position (currentLinePos , endpos));
							rects.Add (new Rectangle (p1, new Size (p2.X - p1.X, FontHeight)));
							if (currentLinePos + 1 < lines.Count)
								p1 = TextPositionToPoint (new Position (currentLinePos + 1, 0));
					             });
					return rects.ToArray ();
				}
				return new Rectangle[] {};
			}
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
			selectionRelativeIndex = 0;

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
		public bool HandleKeyPress (Gdk.EventKey evt)
		{
			return imContext.FilterKeypress (evt);
		}

		void OnCommit (object sender, Gtk.CommitArgs ca)
		{
			try {
				if (selectionRelativeIndex != 0)
					DeleteSelection ();
				for (int i = 0; i < ca.Str.Length; i++) {
					char utf32Char;
					if (char.IsHighSurrogate (ca.Str, i)) {
						utf32Char = (char)char.ConvertToUtf32 (ca.Str, i);
						i++;
					} else {
						utf32Char = ca.Str[i];
					}
					lines[linePos] = lines[linePos].Insert (textPos, utf32Char.ToString ());
					textPos += utf32Char.ToString ().Length;
				}

				Recalculate ();
			} finally {
				imContext.Reset ();
			}
		}

		public void PerformEnter ()
		{
			if (selectionRelativeIndex != 0)
				DeleteSelection ();

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
			if (selectionRelativeIndex != 0) {
				DeleteSelection ();
				return;
			}

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
			if (selectionRelativeIndex != 0) {
				DeleteSelection ();
				return;
			}

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

		public void PerformLeft (bool control, bool shift)
		{
			if (control) {
				PerformControlLeft (shift);
				return;
			}

			// Move caret to the left, or to the previous line
			if (textPos > 0)
				textPos--;
			else if (textPos == 0 && linePos > 0) {
				linePos--;
				textPos = lines[linePos].Length;
			} else
				return;
			if (shift)
				selectionRelativeIndex++;
			else
				selectionRelativeIndex = 0;
		}

		public void PerformControlLeft (bool shift)
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
				if (shift)
					selectionRelativeIndex += textPos - ntp;
				else
					selectionRelativeIndex = 0;
				textPos = ntp;
			} else if (textPos == 0 && linePos > 0) {
				linePos--;
				textPos = lines[linePos].Length;
				if (shift)
					selectionRelativeIndex++;
				else
					selectionRelativeIndex = 0;
			}
		}

		public void PerformRight (bool control, bool shift)
		{
			if (control) {
				PerformControlRight (shift);
				return;
			}

			// Move caret to the right, or to the next line
			if (textPos < lines[linePos].Length) {
				textPos++;
			} else if (textPos == lines[linePos].Length && linePos < lines.Count - 1) {
				linePos++;
				textPos = 0;
			} else
				return;

			if (shift)
				selectionRelativeIndex--;
			else
				selectionRelativeIndex = 0;
		}

		public void PerformControlRight (bool shift)
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
				if (shift)
					selectionRelativeIndex -= ntp - textPos;
				else
					selectionRelativeIndex = 0;
				textPos = ntp;
			} else if (textPos == lines[linePos].Length && linePos < lines.Count - 1) {
				linePos++;
				textPos = 0;
				if (shift)
					selectionRelativeIndex--;
				else
					selectionRelativeIndex = 0;
			}
		}

		public void PerformHome (bool control, bool shift)
		{
			if (control && shift)
				selectionRelativeIndex += PositionToIndex (new Position (linePos, textPos));
			else if (shift)
				selectionRelativeIndex += textPos;
			else
				selectionRelativeIndex = 0;

			// For Ctrl-Home, we go to the top line
			if (control) {
				linePos = 0;
			}

			// Go to the beginning of the line
			textPos = 0;
		}

		public void PerformEnd (bool control, bool shift)
		{
			if (control && shift)
				selectionRelativeIndex -= PositionToIndex (new Position (lines.Count - 1, lines[lines.Count - 1].Length)) - PositionToIndex (new Position (linePos, textPos));
			else if (shift)
				selectionRelativeIndex -= lines[linePos].Length - textPos;
			else
				selectionRelativeIndex = 0;

			// For Ctrl-End, we go to the last line
			if (control)
				linePos = lines.Count - 1;

			// Go to the end of the line
			textPos = lines[linePos].Length;
		}

		public void PerformUp (bool shift)
		{
			// Move to the letter above this one
			Point point = TextPositionToPoint (CurrentPosition);

			point.Y -= FontHeight;

			Position pos = PointToTextPosition (point);

			if (shift)
				selectionRelativeIndex += PositionToIndex (new Position (linePos, textPos)) - PositionToIndex (pos);
			else
				selectionRelativeIndex = 0;

			SetCursorPosition (pos);
		}

		public void PerformDown (bool shift)
		{
			if (CurrentPosition.Line == LineCount - 1) {
				// Last line -> don't do squat
			} else {
				// Move to the letter below this one
				Point point = TextPositionToPoint (CurrentPosition);

				point.Y += FontHeight;

				Position pos = PointToTextPosition (point);

				if (shift)
					selectionRelativeIndex -= PositionToIndex (pos) - PositionToIndex (new Position (linePos, textPos));
				else
					selectionRelativeIndex = 0;

				SetCursorPosition (pos);
			}
		}

		public void PerformCopy (Gtk.Clipboard clipboard)
		{
			if (selectionRelativeIndex > 0) {
				clipboard.Text = GetText (linePos, textPos, selectionRelativeIndex);
			} else if (selectionRelativeIndex < 0) {
				Position p = IndexToPosition (PositionToIndex (new Position (linePos, textPos)) + selectionRelativeIndex);
				clipboard.Text = GetText (p.Line, p.Offset, -selectionRelativeIndex);
			}
			else
				clipboard.Clear ();
		}

		public void PerformCut (Gtk.Clipboard clipboard)
		{
			PerformCopy (clipboard);
			DeleteSelection ();
		}

		public void PerformPaste (Gtk.Clipboard clipboard)
		{
			string txt = string.Empty;
			txt = clipboard.WaitForText ();
			if (String.IsNullOrEmpty (txt))
				return;
			string[] ins_lines = txt.Split (Environment.NewLine.ToCharArray (), StringSplitOptions.None);
			string endline = lines [linePos].Substring (textPos);
			lines [linePos] = lines [linePos].Substring (0, textPos);
			bool first = true;
			foreach (string ins_txt in ins_lines) {
				if (!first) {
					linePos++;
					lines.Insert (linePos, ins_txt);
					textPos = ins_txt.Length;
				} else {
					first = false;
					lines[linePos] += ins_txt;
					textPos += ins_txt.Length;
				}
			}
			lines [linePos] += endline;

			Recalculate ();
		}
		#endregion

		#region Private Methods

		delegate void Action(int currentLine, int strartPosition, int endPosition);

		private void ForeachLine (int startLine, int startPos, int len, Action action)
		{
			int strTextPos = startPos;
			int TextPosLenght = len;
			int currentLinePos = startLine;

			while (strTextPos + TextPosLenght > lines[currentLinePos].Length) {
				action (currentLinePos, strTextPos, lines[currentLinePos].Length);
				TextPosLenght -= lines[currentLinePos].Length - strTextPos + 1;
				currentLinePos++;
				strTextPos = 0;
			}
			action (currentLinePos, strTextPos, strTextPos + TextPosLenght);
		}

		private Position IndexToPosition (int index)
		{
			int current = 0;
			int line = 0;
			int offset = 0;

			foreach (string s in lines) {
				// It's past this line, move along
				if (current + StringToByteSize(s) < index) {
					current += StringToByteSize(s) + 1;
					line++;
					continue;
				}

				// It's in this line
				offset = index - current;
				offset = ByteOffsetToCharacterOffset(lines[line], offset);
				return new Position (line, offset);
			}

			// It's below all of our lines, return the end of the last line
			return new Position (lines.Count - 1, lines[lines.Count - 1].Length);
		}

		private int PositionToIndex (Position p)
		{
			int index = 0;

			for (int i = 0; i < p.Line; i++)
				index += StringToByteSize(lines[i]) + 1;
			
			index += StringToByteSize(lines[p.Line].Substring(0, p.Offset));
			return index;
		}
		
		private int StringToByteSize(string s)
		{
			System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
    			return (enc.GetBytes(s)).Length;
		}
		
		private int ByteOffsetToCharacterOffset(string s, int offset)
		{
			int i = 0;
			for(i = 0; i < offset; i++)
			{
				if(StringToByteSize(s.Substring(0, i)) >= offset) break;
			}
    			return i;
		}
		
		private void Recalculate ()
		{
			string markup = SecurityElement.Escape (ToString ());

			if (underline)
				markup = string.Format ("<u>{0}</u>", markup);

			layout.SetMarkup (markup);
		}

		private string GetText (int startLine, int startPos, int len)
		{
			StringBuilder strbld = new StringBuilder ();
			ForeachLine (startLine, startPos, len, (currentLinePos, strpos, endpos) =>{
				if (endpos - strpos > 0)
					strbld.AppendLine (lines[currentLinePos].Substring (strpos, endpos - strpos));
				else if (endpos == strpos)
					strbld.AppendLine ();
			});
			strbld.Remove (strbld.Length - Environment.NewLine.Length, Environment.NewLine.Length);
			return strbld.ToString ();
		}

		private void DeleteSelection ()
		{
			if (selectionRelativeIndex > 0) {
				DeleteText (textPos, linePos, selectionRelativeIndex);
			} else if (selectionRelativeIndex < 0) {
				Position p = IndexToPosition (PositionToIndex (new Position (linePos, textPos)) + selectionRelativeIndex);
				DeleteText (p.Offset, p.Line, -selectionRelativeIndex);
				textPos = p.Offset;
				linePos = p.Line;
			}
			selectionRelativeIndex = 0;
		}

		private void DeleteText (int startPos, int startLine, int len)
		{
			int TextPosLenght = len;
			int curlinepos = startLine;
			int startposition = startPos;
			if (startposition + len > lines[startLine].Length) {
				TextPosLenght -= lines[startLine].Length - startPos;
				lines[startLine] = lines[startLine].Substring (0, startposition);
				curlinepos++;
				startposition = 0;
			}

			while ((TextPosLenght != 0) && (TextPosLenght > lines[curlinepos].Length)) {
				TextPosLenght -= lines[curlinepos].Length + 1;
				lines.RemoveAt (curlinepos);
				startposition = 0;
			}
			if (TextPosLenght != 0) {
				if (startLine == curlinepos) {
					lines[startLine] = lines[startLine].Substring (0, startposition) + lines[curlinepos].Substring (startposition + TextPosLenght);
				} else {
					//lines[startLine] = lines[startLine].Substring (0, startposition) + lines[curlinepos].Substring (startposition + TextPosLenght - 1);
					lines[startLine] += lines[curlinepos].Substring (startposition + TextPosLenght - 1);
					lines.RemoveAt (curlinepos);
				}
			}

			Recalculate ();

		}

		//TODO video inverse for selected text
		#endregion
	}
}
