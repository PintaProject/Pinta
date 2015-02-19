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
using System.Reflection;
using System.Linq;

namespace Pinta.Core
{
	public class TextEngine
	{
		private List<string> lines;

        private TextPosition currentPos;
        private TextPosition selectionStart;

        public TextAlignment Alignment { get; set; }
        public string FontFace { get; private set; }
        public int FontSize { get; private set; }
        public bool Bold { get; private set; }
        public bool Italic { get; private set; }
        public bool Underline { get; private set; }

		public TextPosition CurrentPosition { get { return currentPos; } }
		public int LineCount { get { return lines.Count; } }
		public TextMode State;
        public Point Origin { get; set; }

        public event EventHandler Modified;

        public TextEngine ()
            : this (new List<string> () { string.Empty })
        {
        }

		public TextEngine(List<string> lines)
		{
            this.lines = lines;
			State = TextMode.Unchanged;
		}

		#region Public Methods
		public void Clear ()
		{
			lines.Clear();
			lines.Add(string.Empty);
			State = TextMode.Unchanged;

            currentPos = new TextPosition (0, 0);
            ClearSelection ();

			Origin = Point.Zero;

            OnModified ();
		}

		/// <summary>
		/// Performs a deep clone of the TextEngine instance and returns the clone.
		/// </summary>
		/// <returns>A clone of this TextEngine instance.</returns>
		public TextEngine Clone()
		{
			TextEngine clonedTE = new TextEngine();

			clonedTE.lines = lines.ToList();
			clonedTE.State = State;
            clonedTE.currentPos = currentPos;
			clonedTE.selectionStart = selectionStart;
            clonedTE.FontFace = FontFace;
            clonedTE.FontSize = FontSize;
            clonedTE.Bold = Bold;
            clonedTE.Italic = Italic;
            clonedTE.Underline = Underline;
            clonedTE.Alignment = Alignment;
			clonedTE.Origin = new Point(Origin.X, Origin.Y);

			//The rest of the variables are calculated on the spot.

			return clonedTE;
		}

		public bool IsEmpty()
		{
			return (lines.Count == 0 || (lines.Count == 1 && lines[0] == string.Empty));
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();

			foreach (string s in lines)
				sb.AppendLine (s);

			return sb.ToString ();
		}

		public KeyValuePair<TextPosition, TextPosition>[] SelectionRegions
		{
			get {
                var regions = new List<KeyValuePair<TextPosition, TextPosition>> ();
                TextPosition p1, p2;

                TextPosition start = TextPosition.Min (currentPos, selectionStart);
                TextPosition end = TextPosition.Max (currentPos, selectionStart);

                p1 = start;
                ForeachLine (start, end, (currentLinePos, strpos, endpos) =>
                {
                    p2 = new TextPosition (currentLinePos, endpos);
                    regions.Add (new KeyValuePair<TextPosition, TextPosition> (p1, p2));
                    if (currentLinePos + 1 < lines.Count)
                        p1 = new TextPosition (currentLinePos + 1, 0);
                });

                return regions.ToArray ();
			}
		}

		public void SetCursorPosition (TextPosition position, bool clearSelection)
		{
            currentPos = position;

            if (clearSelection)
                ClearSelection ();
		}

		public void SetFont (string face, int size, bool bold, bool italic, bool underline)
		{
            FontFace = face;
            FontSize = size;
            Bold = bold;
            Italic = italic;
            Underline = underline;
			OnModified ();
		}

		#endregion

		#region Key Handlers
        public void InsertText (string str)
        {
            if (HasSelection ())
                DeleteSelection ();

            lines[currentPos.Line] = lines[currentPos.Line].Insert (currentPos.Offset, str);
            State = TextMode.Uncommitted;
            currentPos.Offset += str.Length;
            selectionStart = currentPos;

            OnModified ();
        }

		public void PerformEnter ()
		{
			if (HasSelection ())
				DeleteSelection ();

			string currentLine = lines[currentPos.Line];

			if (currentPos.Offset == currentLine.Length) {
				// If we are at the end of a line, insert an empty line at the next line
				lines.Insert (currentPos.Line + 1, string.Empty);
			} else {
				lines.Insert (currentPos.Line + 1, currentLine.Substring (currentPos.Offset, currentLine.Length - currentPos.Offset));
				lines[currentPos.Line] = lines[currentPos.Line].Substring (0, currentPos.Offset);
			}

			State = TextMode.Uncommitted;

			currentPos.Line++;
			currentPos.Offset = 0;
            selectionStart = currentPos;
			OnModified ();
		}

		public void PerformBackspace ()
		{
			if (HasSelection ()) {
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
			} else if (currentPos.Offset > 0) {
				// We're in the middle of a line, delete the previous character
				string ln = lines[currentPos.Line];

				// If we are at the end of a line, we don't need to place a compound string
				if (currentPos.Offset == ln.Length)
					lines[currentPos.Line] = ln.Substring (0, ln.Length - 1);
				else
					lines[currentPos.Line] = ln.Substring (0, currentPos.Offset - 1) + ln.Substring (currentPos.Offset);

				currentPos.Offset--;
			}

            selectionStart = currentPos;
			State = TextMode.Uncommitted;
            OnModified ();
		}

		public void PerformDelete ()
		{
			if (HasSelection ()) {
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

			State = TextMode.Uncommitted;

			OnModified ();
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

			if (!shift)
                ClearSelection();
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

                if (!shift)
                    ClearSelection ();

				currentPos.Offset = ntp;
			} else if (currentPos.Offset == 0 && currentPos.Line > 0) {
				currentPos.Line--;
				currentPos.Offset = lines[currentPos.Line].Length;

                if (!shift)
                    ClearSelection ();
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

            if (!shift)
                ClearSelection ();
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

				currentPos.Offset = ntp;

                if (!shift)
                    ClearSelection ();
			} else if (currentPos.Offset == lines[currentPos.Line].Length && currentPos.Line < lines.Count - 1) {
				currentPos.Line++;
				currentPos.Offset = 0;

                if (!shift)
                    ClearSelection ();
			}
		}

		public void PerformHome (bool control, bool shift)
		{
			// For Ctrl-Home, we go to the top line
			if (control) {
				currentPos.Line = 0;
			}

			// Go to the beginning of the line
			currentPos.Offset = 0;

            if (!shift)
                ClearSelection ();
		}

		public void PerformEnd (bool control, bool shift)
		{
			// For Ctrl-End, we go to the last line
			if (control)
				currentPos.Line = lines.Count - 1;

			// Go to the end of the line
			currentPos.Offset = lines[currentPos.Line].Length;

            if (!shift)
                ClearSelection ();
		}

		public void PerformUp (bool shift)
		{
            if (currentPos.Line > 0)
            {
                currentPos.Line--;
                currentPos.Offset = Math.Min (currentPos.Offset,
                                              lines[currentPos.Line].Length);

                if (!shift)
                    ClearSelection ();
            }
		}

		public void PerformDown (bool shift)
		{
            if (currentPos.Line < LineCount - 1)
            {
                currentPos.Line++;
                currentPos.Offset = Math.Min (currentPos.Offset,
                                              lines[currentPos.Line].Length);

                if (!shift)
                    ClearSelection ();
            }
		}

		public void PerformCopy (Gtk.Clipboard clipboard)
		{
            if (HasSelection ())
            {
                StringBuilder strbld = new StringBuilder ();

                TextPosition start = TextPosition.Min (currentPos, selectionStart);
                TextPosition end = TextPosition.Max (currentPos, selectionStart);
                ForeachLine (start, end, (currentLinePos, strpos, endpos) =>{
                    if (endpos - strpos > 0)
                        strbld.AppendLine (lines[currentLinePos].Substring (strpos, endpos - strpos));
                    else if (endpos == strpos)
                        strbld.AppendLine ();
                });
                strbld.Remove (strbld.Length - Environment.NewLine.Length, Environment.NewLine.Length);

                clipboard.Text = strbld.ToString ();
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

            if (HasSelection ())
                DeleteSelection ();

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

            selectionStart = currentPos;
			State = TextMode.Uncommitted;

			OnModified ();
			return true;
		}
		#endregion

		#region Private Methods

		delegate void Action(int currentLine, int strartPosition, int endPosition);

		private void ForeachLine (TextPosition start, TextPosition end, Action action)
		{
            if (start.CompareTo(end) > 0)
                throw new ArgumentException ("Invalid start position", "start");

            while (start.Line < end.Line)
            {
                action (start.Line, start.Offset, lines[start.Line].Length);
                ++start.Line;
                start.Offset = 0;
            }

            action (start.Line, start.Offset, end.Offset);
		}

		public TextPosition IndexToPosition (int index)
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

		public int PositionToIndex (TextPosition p)
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
		
        private void OnModified ()
        {
            EventHandler handler = Modified;
            if (handler != null)
                handler (this, EventArgs.Empty);
        }

		private string GetText (TextPosition startPos, int len)
		{
			StringBuilder strbld = new StringBuilder ();

            TextPosition start = TextPosition.Min (currentPos, selectionStart);
            TextPosition end = TextPosition.Max (currentPos, selectionStart);
			ForeachLine (start, end, (currentLinePos, strpos, endpos) =>{
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
            TextPosition start = TextPosition.Min (currentPos, selectionStart);
            TextPosition end = TextPosition.Max (currentPos, selectionStart);
            DeleteText (start, end);

            currentPos = start;
            ClearSelection ();
		}

		private void DeleteText (TextPosition start, TextPosition end)
		{
            if (start.CompareTo(end) >= 0)
                throw new ArgumentException ("Invalid start position", "start");

            lines[start.Line] = lines[start.Line].Substring (0, start.Offset) +
                                lines[end.Line].Substring (end.Offset);

            // If this was a multi-line delete, remove all lines in between,
            // including the end line.
            lines.RemoveRange (start.Line + 1, end.Line - start.Line);

			State = TextMode.Uncommitted;
            OnModified ();
		}

        private bool HasSelection ()
        {
            return selectionStart != currentPos;
        }

        private void ClearSelection ()
        {
            selectionStart = currentPos;
        }

		#endregion
	}
}
