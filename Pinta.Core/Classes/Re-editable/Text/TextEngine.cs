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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pinta.Core
{
	public class TextEngine
	{
		private List<string> lines;

		private TextPosition currentPos;
		private TextPosition selectionStart;

		public Pango.FontDescription Font { get; private set; } = PangoExtensions.CreateFontDescription ();
		public TextAlignment Alignment { get; private set; }
		public bool Underline { get; private set; }

		public TextPosition CurrentPosition => currentPos;
		public int LineCount => lines.Count;
		public TextMode State;
		public PointI Origin { get; set; }

		public event EventHandler? Modified;

		public TextEngine ()
		    : this (new List<string> () { string.Empty })
		{
		}

		public TextEngine (List<string> lines)
		{
			this.lines = new (lines);
			State = TextMode.Unchanged;
		}

		#region Public Methods
		public void Clear ()
		{
			lines.Clear ();
			lines.Add (string.Empty);
			State = TextMode.Unchanged;

			currentPos = new TextPosition (0, 0);
			ClearSelection ();

			Origin = PointI.Zero;

			OnModified ();
		}

		/// <summary>
		/// Performs a deep clone of the TextEngine instance and returns the clone.
		/// </summary>
		/// <returns>A clone of this TextEngine instance.</returns>
		public TextEngine Clone ()
		{
			TextEngine clonedTE = new TextEngine ();

			clonedTE.lines = lines.ToList ();
			clonedTE.State = State;
			clonedTE.currentPos = currentPos;
			clonedTE.selectionStart = selectionStart;
			clonedTE.Font = Font.Copy ();
			clonedTE.Alignment = Alignment;
			clonedTE.Underline = Underline;
			clonedTE.Origin = new PointI (Origin.X, Origin.Y);

			//The rest of the variables are calculated on the spot.

			return clonedTE;
		}

		public bool IsEmpty ()
		{
			return (lines.Count == 0 || (lines.Count == 1 && lines[0] == string.Empty));
		}

		public IReadOnlyList<string> Lines => lines;

		public override string ToString () => string.Join (Environment.NewLine, lines);

		public KeyValuePair<TextPosition, TextPosition>[] SelectionRegions {
			get {
				var regions = new List<KeyValuePair<TextPosition, TextPosition>> ();
				TextPosition p1, p2;

				TextPosition start = TextPosition.Min (currentPos, selectionStart);
				TextPosition end = TextPosition.Max (currentPos, selectionStart);

				p1 = start;
				ForeachLine (start, end, (currentLinePos, strpos, endpos) => {
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

		public void SetFont (Pango.FontDescription font, TextAlignment alignment, bool underline)
		{
			Font = font;
			Alignment = alignment;
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

			if (currentPos.Offset == 0 && currentPos.Line > 0) {
				// We're at the beginning of a line and there's
				// a line above us, go to the end of the prior line
				int prevLength = lines[currentPos.Line - 1].Length;
				lines[currentPos.Line - 1] += lines[currentPos.Line];
				lines.RemoveAt (currentPos.Line);
				currentPos.Line--;
				currentPos.Offset = prevLength;
			} else if (currentPos.Offset > 0) {
				// We're in the middle of a line, delete the previous text element.
				RemoveNextTextElement (-1);
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

			var currentLine = lines[currentPos.Line];
			if (currentPos.Offset < currentLine.Length) {
				// In the middle of a line - delete the next text element.
				RemoveNextTextElement (0);
			} else if (currentPos.Line < lines.Count - 1) {
				// End of a line - merge strings.
				lines[currentPos.Line] += lines[currentPos.Line + 1];
				lines.RemoveAt (currentPos.Line + 1);
			}

			State = TextMode.Uncommitted;
			OnModified ();
		}

		public void PerformLeft (bool control, bool shift)
		{
			// Move caret to the left, or to the previous line
			if (currentPos.Offset > 0) {
				var currentLine = lines[currentPos.Line];
				if (control) {
					// Move to the beginning of the previous word.
					currentPos.Offset = FindWords (currentLine)
						.Where (i => i < currentPos.Offset)
						.DefaultIfEmpty (0)
						.Last ();
				} else {
					(var elements, var elementIndex) = FindTextElementIndex (currentLine, currentPos.Offset);
					currentPos.Offset = elements[elementIndex - 1];
				}
			} else if (currentPos.Offset == 0 && currentPos.Line > 0) {
				currentPos.Line--;
				currentPos.Offset = lines[currentPos.Line].Length;
			}

			if (!shift)
				ClearSelection ();
		}

		public void PerformRight (bool control, bool shift)
		{
			var currentLine = lines[currentPos.Line];
			if (currentPos.Offset < currentLine.Length) {
				if (control) {
					// Move to the beginning of the next word.
					currentPos.Offset = FindWords (currentLine)
						.Where (i => i > currentPos.Offset)
						.DefaultIfEmpty (currentLine.Length)
						.First ();
				} else {
					// Move to the next text element.
					(var elements, var elementIndex) = FindTextElementIndex (currentLine, currentPos.Offset);
					if (elementIndex < elements.Length - 1)
						currentPos.Offset = elements[elementIndex + 1];
					else
						currentPos.Offset = currentLine.Length;
				}

			} else if (currentPos.Offset == currentLine.Length && currentPos.Line < lines.Count - 1) {
				currentPos.Line++;
				currentPos.Offset = 0;

			}

			if (!shift)
				ClearSelection ();
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
			if (currentPos.Line == 0)
				return;

			MoveToAdjacentLine (-1);

			if (!shift)
				ClearSelection ();

		}

		public void PerformDown (bool shift)
		{
			if (currentPos.Line < LineCount - 1) {
				MoveToAdjacentLine (1);

				if (!shift)
					ClearSelection ();
			}
		}

		public void PerformCopy (Gdk.Clipboard clipboard)
		{
			if (HasSelection ()) {
				StringBuilder strbld = new StringBuilder ();

				TextPosition start = TextPosition.Min (currentPos, selectionStart);
				TextPosition end = TextPosition.Max (currentPos, selectionStart);
				ForeachLine (start, end, (currentLinePos, strpos, endpos) => {
					if (endpos - strpos > 0)
						strbld.AppendLine (lines[currentLinePos].Substring (strpos, endpos - strpos));
					else if (endpos == strpos)
						strbld.AppendLine ();
				});
				strbld.Remove (strbld.Length - Environment.NewLine.Length, Environment.NewLine.Length);

				clipboard.SetText (strbld.ToString ());
			} else
				clipboard.SetText (string.Empty);
		}

		public void PerformCut (Gdk.Clipboard clipboard)
		{
			PerformCopy (clipboard);
			if (HasSelection ()) {
				DeleteSelection ();
			}
		}

		/// <summary>
		/// Pastes text from the clipboard.
		/// </summary>
		public async Task<bool> PerformPaste (Gdk.Clipboard clipboard)
		{
			string? txt = await clipboard.ReadTextAsync ();
			if (String.IsNullOrEmpty (txt))
				return false;

			if (HasSelection ())
				DeleteSelection ();

			string[] ins_lines = txt.Split (Environment.NewLine.ToCharArray (), StringSplitOptions.RemoveEmptyEntries);
			string endline = lines[currentPos.Line].Substring (currentPos.Offset);
			lines[currentPos.Line] = lines[currentPos.Line].Substring (0, currentPos.Offset);
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
			lines[currentPos.Line] += endline;

			selectionStart = currentPos;
			State = TextMode.Uncommitted;

			OnModified ();

			return true;
		}
		#endregion

		#region Private Methods

		delegate void Action (int currentLine, int strartPosition, int endPosition);

		private void ForeachLine (TextPosition start, TextPosition end, Action action)
		{
			if (start.CompareTo (end) > 0)
				throw new ArgumentException ("Invalid start position", nameof (start));

			while (start.Line < end.Line) {
				action (start.Line, start.Offset, lines[start.Line].Length);
				++start.Line;
				start.Offset = 0;
			}

			action (start.Line, start.Offset, end.Offset);
		}

		public TextPosition UTF8IndexToPosition (int index)
		{
			int current = 0;
			int line = 0;
			int offset = 0;

			foreach (string s in lines) {
				// It's past this line, move along
				if (current + StringToUTF8Size (s) < index) {
					current += StringToUTF8Size (s) + 1;
					line++;
					continue;
				}

				// It's in this line
				offset = index - current;
				offset = UTF8OffsetToCharacterOffset (lines[line], offset);
				return new TextPosition (line, offset);
			}

			// It's below all of our lines, return the end of the last line
			return new TextPosition (lines.Count - 1, lines[lines.Count - 1].Length);
		}

		public int PositionToUTF8Index (TextPosition p)
		{
			int index = 0;

			for (int i = 0; i < p.Line; i++)
				index += StringToUTF8Size (lines[i]) + 1;

			index += StringToUTF8Size (lines[p.Line].Substring (0, p.Offset));
			return index;
		}

		private static int StringToUTF8Size (string s)
		{
			System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding ();
			return (enc.GetBytes (s)).Length;
		}

		private int UTF8OffsetToCharacterOffset (string s, int offset)
		{
			int i = 0;
			for (i = 0; i < offset; i++) {
				if (StringToUTF8Size (s.Substring (0, i)) >= offset) break;
			}
			return i;
		}

		private void OnModified ()
		{
			EventHandler? handler = Modified;
			if (handler != null)
				handler (this, EventArgs.Empty);
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
			if (start.CompareTo (end) >= 0)
				throw new ArgumentException ("Invalid start position", nameof (start));

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

		/// <summary>
		/// Returns a list of the char indices where each text element begins, along with
		/// the element index corresponding to the specified character.
		/// </summary>
		private static (int[], int) FindTextElementIndex (string s, int charIndex)
		{
			var elements = StringInfo.ParseCombiningCharacters (s);

			// It's valid to position the caret after the last character in the line.
			int elementIndex;
			if (charIndex == s.Length)
				elementIndex = elements.Length;
			else {
				elementIndex = Array.FindIndex (elements, i => i == charIndex);
				if (elementIndex < 0)
					throw new InvalidOperationException ("Text position is not at the beginning of a text element");
			}

			return (elements, elementIndex);
		}

		/// <summary>
		/// Remove the text element after the current position (plus the specified element offset, e.g. -1 for backspace).
		/// </summary>
		/// <param name="elementOffset"></param>
		private void RemoveNextTextElement (int elementOffset)
		{
			var line = lines[currentPos.Line];
			var lineInfo = new StringInfo (line);

			(var elements, var elementIndex) = FindTextElementIndex (line, currentPos.Offset);
			elementIndex += elementOffset;
			var before = lineInfo.SubstringByTextElements (0, elementIndex);
			var after = (elementIndex < elements.Length - 1) ? lineInfo.SubstringByTextElements (elementIndex + 1) : string.Empty;

			lines[currentPos.Line] = before + after;
			currentPos.Offset = before.Length;
		}

		private void MoveToAdjacentLine (int lineOffset)
		{
			var currentLine = lines[currentPos.Line];
			var nextLine = lines[currentPos.Line + lineOffset];

			// Remain at the same text element index (if possible) when changing lines.
			(_, var elementIndex) = FindTextElementIndex (currentLine, currentPos.Offset);
			var nextLineElements = StringInfo.ParseCombiningCharacters (nextLine);

			if (elementIndex < nextLineElements.Length) {
				currentPos.Offset = nextLineElements[elementIndex];
			} else {
				currentPos.Offset = nextLine.Length;
			}

			currentPos.Line += lineOffset;
		}

		/// <summary>
		/// Returns the index of the first character of each word in the line.
		/// </summary>
		private static IEnumerable<int> FindWords (string s) => Regex.Matches (s, @"\b\w").Select (m => m.Index);

		#endregion
	}
}
