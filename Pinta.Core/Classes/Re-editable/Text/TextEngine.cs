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
using System.Reflection;
using System.Linq;

namespace Pinta.Core
{
	public class TextEngine
	{
		private Point origin;
		private Pango.Layout layout;

		private List<string> lines;

        private TextPosition currentPos;
		// Relative coordinate of selection.
		private int selectionOffset = 0;

		bool underline;
		Gtk.IMMulticontext imContext;

		public TextEngine()
		{
			lines = new List<string> ();
			lines.Add(string.Empty);
			textMode = TextMode.Unchanged;

			layout = new Pango.Layout (PintaCore.Chrome.Canvas.PangoContext);
			imContext = new Gtk.IMMulticontext ();
			imContext.Commit += OnCommit;
		}

		#region Public Properties
		public TextPosition CurrentPosition {
            get { return currentPos; }
		}

		public EditingMode EditMode {
			get
			{
				if (textMode == TextMode.Unchanged)
				{
					return EditingMode.NoChangeEditing;
				}

				if (IsEmpty())
				{
					return EditingMode.EmptyEdit;
				}

				return EditingMode.Editing;
			}
		}

		public int FontHeight { get { return GetCursorLocation ().Height; } }
		public Pango.Layout Layout { get { return layout; } }
		public int LineCount { get { return lines.Count; } }
		public TextMode textMode;

		//The position to draw the text at.
		public Point Origin {
			get { return origin; }
			set { origin = value; }
		}

		public Rectangle[] SelectionRectangles
		{
			get {
				List<Rectangle> rects = new List<Rectangle> ();
				Point p1, p2;

				if (selectionOffset > 0) {
					p1 = TextPositionToPoint (currentPos);

					ForeachLine (currentPos, selectionOffset, (currentLinePos, strpos, endpos) =>
						{
					        p2 = TextPositionToPoint (new TextPosition (currentLinePos , endpos));
							rects.Add (new Rectangle (p1, new Size (p2.X - p1.X, FontHeight)));
							if (currentLinePos + 1 < lines.Count)
								p1 = TextPositionToPoint (new TextPosition (currentLinePos + 1, 0));
					             });
					return rects.ToArray ();

				} else if (selectionOffset < 0) {
					TextPosition mypos = IndexToPosition (PositionToIndex (currentPos) + selectionOffset);
					p1 = TextPositionToPoint (mypos);
					ForeachLine (mypos, -selectionOffset, (currentLinePos, strpos, endpos) =>
						{
					        p2 = TextPositionToPoint (new TextPosition (currentLinePos , endpos));
							rects.Add (new Rectangle (p1, new Size (p2.X - p1.X, FontHeight)));
							if (currentLinePos + 1 < lines.Count)
								p1 = TextPositionToPoint (new TextPosition (currentLinePos + 1, 0));
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
			lines.Clear();
			lines.Add(string.Empty);
			textMode = TextMode.Unchanged;

            currentPos = new TextPosition (0, 0);

			origin = Point.Zero;
			selectionOffset = 0;

			Recalculate ();
		}

		/// <summary>
		/// Performs a deep clone of the TextEngine instance and returns the clone.
		/// </summary>
		/// <returns>A clone of this TextEngine instance.</returns>
		public TextEngine Clone()
		{
			TextEngine clonedTE = new TextEngine();

			clonedTE.layout = layout.Copy();
			clonedTE.lines = lines.ToList();
			clonedTE.textMode = textMode;
            clonedTE.currentPos = currentPos;
			clonedTE.selectionOffset = selectionOffset;
			clonedTE.underline = underline;
			clonedTE.Origin = new Point(Origin.X, Origin.Y);

			//The rest of the variables are calculated on the spot.

			return clonedTE;
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
			var cursor = GetCursorLocation ();

			// GetPixelExtents() doesn't really return a very sensible height.
			// Instead of doing some hacky arithmetic to correct it, the height will just
			// be the cursor's height times the number of lines.
			return new Rectangle (origin.X, origin.Y, ink.Width, cursor.Height * LineCount);
		}

		public bool IsEmpty()
		{
			return (lines.Count == 0 || (lines.Count == 1 && lines[0] == string.Empty));
		}

		public TextPosition PointToTextPosition (Point point)
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

		public void SetCursorPosition (TextPosition position, bool clearSelection)
		{
            currentPos = position;

			if (clearSelection)
				selectionOffset = 0;
		}

		public void SetFont (string face, int size, bool bold, bool italic, bool underline)
		{
			var font = Pango.FontDescription.FromString (string.Format ("{0} {1}", face, size));

			// Forces font variants to be rendered properly
			// (e.g. this will use "Ubuntu Condensed" instead of "Ubuntu").
			font.Family = face;

			font.Weight = bold ? Pango.Weight.Bold : Pango.Weight.Normal;
			font.Style = italic ? Pango.Style.Italic : Pango.Style.Normal;

			layout.FontDescription = font;

			this.underline = underline;
			Recalculate ();
		}

		public Point TextPositionToPoint (TextPosition p)
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
				if (selectionOffset != 0)
					DeleteSelection ();
				for (int i = 0; i < ca.Str.Length; i++) {
					char utf32Char;
					if (char.IsHighSurrogate (ca.Str, i)) {
						utf32Char = (char)char.ConvertToUtf32 (ca.Str, i);
						i++;
					} else {
						utf32Char = ca.Str[i];
					}
					lines[currentPos.Line] = lines[currentPos.Line].Insert (currentPos.Offset, utf32Char.ToString ());
					textMode = TextMode.Uncommitted;
					currentPos.Offset += utf32Char.ToString ().Length;
				}

				Recalculate ();
			} finally {
				imContext.Reset ();
			}
		}

		public void PerformEnter ()
		{
			if (selectionOffset != 0)
				DeleteSelection ();

			string currentLine = lines[currentPos.Line];

			if (currentPos.Offset == currentLine.Length) {
				// If we are at the end of a line, insert an empty line at the next line
				lines.Insert (currentPos.Line + 1, string.Empty);
			} else {
				lines.Insert (currentPos.Line + 1, currentLine.Substring (currentPos.Offset, currentLine.Length - currentPos.Offset));
				lines[currentPos.Line] = lines[currentPos.Line].Substring (0, currentPos.Offset);
			}

			textMode = TextMode.Uncommitted;

			currentPos.Line++;
			currentPos.Offset = 0;
			Recalculate ();
		}

		public void PerformBackspace ()
		{
			if (selectionOffset != 0) {
				DeleteSelection ();
				return;
			}

			// We're at the beginning of a line and there's
			// a line above us, go to the end of the prior line
			if (currentPos.Offset == 0 && currentPos.Line > 0) {
				int ntp = lines[currentPos.Line - 1].Length;

				lines[currentPos.Line - 1] = lines[currentPos.Line - 1] + lines[currentPos.Line];
				lines.RemoveAt (currentPos.Line);
				currentPos.Line--;
				currentPos.Offset = ntp;
				Recalculate ();
			} else if (currentPos.Offset > 0) {
				// We're in the middle of a line, delete the previous character
				string ln = lines[currentPos.Line];

				// If we are at the end of a line, we don't need to place a compound string
				if (currentPos.Offset == ln.Length)
					lines[currentPos.Line] = ln.Substring (0, ln.Length - 1);
				else
					lines[currentPos.Line] = ln.Substring (0, currentPos.Offset - 1) + ln.Substring (currentPos.Offset);

				currentPos.Offset--;
				Recalculate ();
			}

			textMode = TextMode.Uncommitted;
		}

		public void PerformDelete ()
		{
			if (selectionOffset != 0) {
				DeleteSelection ();
				return;
			}

			// Where are we?!
			if ((currentPos.Line == lines.Count - 1) && (currentPos.Offset == lines[lines.Count - 1].Length)) {
				// The cursor is at the end of the text block
				return;
			} else if (currentPos.Offset == lines[currentPos.Line].Length) {
				// End of a line, must merge strings
				lines[currentPos.Line] = lines[currentPos.Line] + lines[currentPos.Line + 1];
				lines.RemoveAt (currentPos.Line + 1);
			} else {
				// Middle of a line somewhere
				lines[currentPos.Line] = lines[currentPos.Line].Substring (0, currentPos.Offset) + (lines[currentPos.Line]).Substring (currentPos.Offset + 1);
			}

			textMode = TextMode.Uncommitted;

			Recalculate ();
		}

		public void PerformLeft (bool control, bool shift)
		{
			if (control) {
				PerformControlLeft (shift);
				return;
			}

			// Move caret to the left, or to the previous line
			if (currentPos.Offset > 0)
				currentPos.Offset--;
			else if (currentPos.Offset == 0 && currentPos.Line > 0) {
				currentPos.Line--;
				currentPos.Offset = lines[currentPos.Line].Length;
			} else
				return;
			if (shift)
				selectionOffset++;
			else
				selectionOffset = 0;
		}

		public void PerformControlLeft (bool shift)
		{
			// Move caret to the left to the beginning of the word/space/etc.
			if (currentPos.Offset > 0) {
				int ntp = currentPos.Offset;
				string currentLine = lines[currentPos.Line];

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
					selectionOffset += currentPos.Offset - ntp;
				else
					selectionOffset = 0;
				currentPos.Offset = ntp;
			} else if (currentPos.Offset == 0 && currentPos.Line > 0) {
				currentPos.Line--;
				currentPos.Offset = lines[currentPos.Line].Length;
				if (shift)
					selectionOffset++;
				else
					selectionOffset = 0;
			}
		}

		public void PerformRight (bool control, bool shift)
		{
			if (control) {
				PerformControlRight (shift);
				return;
			}

			// Move caret to the right, or to the next line
			if (currentPos.Offset < lines[currentPos.Line].Length) {
				currentPos.Offset++;
			} else if (currentPos.Offset == lines[currentPos.Line].Length && currentPos.Line < lines.Count - 1) {
				currentPos.Line++;
				currentPos.Offset = 0;
			} else
				return;

			if (shift)
				selectionOffset--;
			else
				selectionOffset = 0;
		}

		public void PerformControlRight (bool shift)
		{
			// Move caret to the right to the end of the word/space/etc.
			if (currentPos.Offset < lines[currentPos.Line].Length) {
				int ntp = currentPos.Offset;
				string currentLine = lines[currentPos.Line];

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
					selectionOffset -= ntp - currentPos.Offset;
				else
					selectionOffset = 0;
				currentPos.Offset = ntp;
			} else if (currentPos.Offset == lines[currentPos.Line].Length && currentPos.Line < lines.Count - 1) {
				currentPos.Line++;
				currentPos.Offset = 0;
				if (shift)
					selectionOffset--;
				else
					selectionOffset = 0;
			}
		}

		public void PerformHome (bool control, bool shift)
		{
			if (control && shift)
				selectionOffset += PositionToIndex (currentPos);
			else if (shift)
				selectionOffset += currentPos.Offset;
			else
				selectionOffset = 0;

			// For Ctrl-Home, we go to the top line
			if (control) {
				currentPos.Line = 0;
			}

			// Go to the beginning of the line
			currentPos.Offset = 0;
		}

		public void PerformEnd (bool control, bool shift)
		{
			if (control && shift)
				selectionOffset -= PositionToIndex (new TextPosition (lines.Count - 1, lines[lines.Count - 1].Length)) - PositionToIndex (currentPos);
			else if (shift)
				selectionOffset -= lines[currentPos.Line].Length - currentPos.Offset;
			else
				selectionOffset = 0;

			// For Ctrl-End, we go to the last line
			if (control)
				currentPos.Line = lines.Count - 1;

			// Go to the end of the line
			currentPos.Offset = lines[currentPos.Line].Length;
		}

		public void PerformUp (bool shift)
		{
			// Move to the letter above this one
			Point point = TextPositionToPoint (CurrentPosition);

			point.Y -= FontHeight;

			TextPosition pos = PointToTextPosition (point);

			if (shift)
				selectionOffset += PositionToIndex (currentPos) - PositionToIndex (pos);
			else
				selectionOffset = 0;

			SetCursorPosition (pos, false);
		}

		public void PerformDown (bool shift)
		{
			if (CurrentPosition.Line == LineCount - 1) {
				// Last line -> don't do squat
			} else {
				// Move to the letter below this one
				Point point = TextPositionToPoint (CurrentPosition);

				point.Y += FontHeight;

				TextPosition pos = PointToTextPosition (point);

				if (shift)
					selectionOffset -= PositionToIndex (pos) - PositionToIndex (currentPos);
				else
					selectionOffset = 0;

				SetCursorPosition (pos, false);
			}
		}

		public void PerformCopy (Gtk.Clipboard clipboard)
		{
			if (selectionOffset > 0) {
				clipboard.Text = GetText (currentPos, selectionOffset);
			} else if (selectionOffset < 0) {
				TextPosition p = IndexToPosition (PositionToIndex (currentPos) + selectionOffset);
				clipboard.Text = GetText (p, -selectionOffset);
			}
			else
				clipboard.Clear ();
		}

		public void PerformCut (Gtk.Clipboard clipboard)
		{
			PerformCopy (clipboard);
			DeleteSelection ();
		}

		/// <summary>
		/// Pastes text from the clipboard.
		/// </summary>
		/// <returns>
		/// <c>true</c>, if the paste was successfully performed, <c>false</c> otherwise.
		/// </returns>
		public bool PerformPaste (Gtk.Clipboard clipboard)
		{
			string txt = string.Empty;
			txt = clipboard.WaitForText ();
			if (String.IsNullOrEmpty (txt))
				return false;

			string[] ins_lines = txt.Split (Environment.NewLine.ToCharArray (), StringSplitOptions.RemoveEmptyEntries);
			string endline = lines [currentPos.Line].Substring (currentPos.Offset);
			lines [currentPos.Line] = lines [currentPos.Line].Substring (0, currentPos.Offset);
			bool first = true;
			foreach (string ins_txt in ins_lines) {
				if (!first) {
					currentPos.Line++;
					lines.Insert (currentPos.Line, ins_txt);
					currentPos.Offset = ins_txt.Length;
				} else {
					first = false;
					lines[currentPos.Line] += ins_txt;
					currentPos.Offset += ins_txt.Length;
				}
			}
			lines [currentPos.Line] += endline;

			textMode = TextMode.Uncommitted;

			Recalculate ();
			return true;
		}
		#endregion

		#region Private Methods

		delegate void Action(int currentLine, int strartPosition, int endPosition);

		private void ForeachLine (TextPosition startPos, int len, Action action)
		{
			int strTextPos = startPos.Offset;
			int TextPosLenght = len;
			int currentLinePos = startPos.Line;

			while (strTextPos + TextPosLenght > lines[currentLinePos].Length) {
				action (currentLinePos, strTextPos, lines[currentLinePos].Length);
				TextPosLenght -= lines[currentLinePos].Length - strTextPos + 1;
				currentLinePos++;
				strTextPos = 0;
			}
			action (currentLinePos, strTextPos, strTextPos + TextPosLenght);
		}

		private TextPosition IndexToPosition (int index)
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
				return new TextPosition (line, offset);
			}

			// It's below all of our lines, return the end of the last line
			return new TextPosition (lines.Count - 1, lines[lines.Count - 1].Length);
		}

		private int PositionToIndex (TextPosition p)
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

		private string GetText (TextPosition startPos, int len)
		{
			StringBuilder strbld = new StringBuilder ();
			ForeachLine (startPos, len, (currentLinePos, strpos, endpos) =>{
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
			if (selectionOffset > 0) {
				DeleteText (currentPos.Offset, currentPos.Line, selectionOffset);
			} else if (selectionOffset < 0) {
				TextPosition p = IndexToPosition (PositionToIndex (currentPos) + selectionOffset);
				DeleteText (p.Offset, p.Line, -selectionOffset);
				currentPos.Offset = p.Offset;
				currentPos.Line = p.Line;
			}
			selectionOffset = 0;
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

			textMode = TextMode.Uncommitted;

			Recalculate ();

		}

		//TODO video inverse for selected text
		#endregion
	}
}
