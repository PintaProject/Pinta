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
using Cairo;

namespace Pinta.Core;

public sealed partial class TextEngine
{
	private List<string> lines;

	private TextPosition current_pos;
	private TextPosition selection_start;

	public Pango.FontDescription Font { get; private set; } = Pango.FontDescription.New ();

	private Color primary_color = new (0, 0, 0);

	private Color secondary_color = new (0, 0, 0);
	public Color PrimaryColor {
		get => primary_color;
		set {
			primary_color = new (value.R, value.G, value.B, value.A);
			OnModified ();
		}
	}

	public Color SecondaryColor {
		get => secondary_color;
		set {
			secondary_color = new (value.R, value.G, value.B, value.A);
			OnModified ();
		}
	}

	public TextAlignment Alignment { get; private set; }
	public bool Underline { get; private set; }

	public TextPosition CurrentPosition => current_pos;

	public int LineCount => lines.Count;

	public TextMode State { get; set; }
	public PointI Origin { get; set; }

	public event EventHandler? Modified;

	public TextEngine ()
		: this ([string.Empty])
	{ }

	public TextEngine (IEnumerable<string> lines)
	{
		this.lines = new (lines);
		State = TextMode.Unchanged;
	}

	public void Clear ()
	{
		lines.Clear ();
		lines.Add (string.Empty);
		State = TextMode.Unchanged;

		current_pos = new TextPosition (0, 0);
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
		TextEngine clonedTE = new () {
			lines = [.. lines],
			State = State,
			current_pos = current_pos,
			selection_start = selection_start,
			Font = Font.Copy ()!, // NRT: pango_font_description_copy only returns null when given nullptr
			PrimaryColor = primary_color,
			SecondaryColor = secondary_color,
			Alignment = Alignment,
			Underline = Underline,
			Origin = new PointI (Origin.X, Origin.Y)
		};

		//The rest of the variables are calculated on the spot.

		return clonedTE;
	}

	public bool IsEmpty ()
	{
		return (lines.Count == 0 || (lines.Count == 1 && lines[0] == string.Empty));
	}

	public IReadOnlyList<string> Lines
		=> lines;

	public override string ToString ()
		=> string.Join (Environment.NewLine, lines);

	public KeyValuePair<TextPosition, TextPosition>[] SelectionRegions {
		get {
			List<KeyValuePair<TextPosition, TextPosition>> regions = [];

			TextPosition start = TextPosition.Min (current_pos, selection_start);
			TextPosition end = TextPosition.Max (current_pos, selection_start);

			TextPosition p1, p2;

			p1 = start;
			ForeachLine (start, end, (currentLinePos, strpos, endpos) => {
				p2 = new TextPosition (currentLinePos, endpos);
				regions.Add (new KeyValuePair<TextPosition, TextPosition> (p1, p2));
				if (currentLinePos + 1 < lines.Count)
					p1 = new TextPosition (currentLinePos + 1, 0);
			});

			return [.. regions];
		}
	}

	public void SetCursorPosition (TextPosition position, bool clearSelection)
	{
		current_pos = position;

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

	public void InsertText (string str)
	{
		if (HasSelection ())
			DeleteSelection ();

		lines[current_pos.Line] = lines[current_pos.Line].Insert (current_pos.Offset, str);
		State = TextMode.Uncommitted;
		current_pos = current_pos.WithOffset (current_pos.Offset + str.Length);
		selection_start = current_pos;

		OnModified ();
	}

	public void PerformEnter ()
	{
		if (HasSelection ())
			DeleteSelection ();

		string currentLine = lines[current_pos.Line];

		if (current_pos.Offset == currentLine.Length) {
			// If we are at the end of a line, insert an empty line at the next line
			lines.Insert (current_pos.Line + 1, string.Empty);
		} else {
			lines.Insert (current_pos.Line + 1, currentLine[current_pos.Offset..]);
			lines[current_pos.Line] = lines[current_pos.Line][..current_pos.Offset];
		}

		State = TextMode.Uncommitted;

		current_pos = new (
			line: current_pos.Line + 1,
			offset: 0);
		selection_start = current_pos;
		OnModified ();
	}

	private int FindPreviousWordOffset (string currentLine)
	{
		return FindWords (currentLine)
			.Where (i => i < current_pos.Offset)
			.DefaultIfEmpty (0)
			.Last ();
	}

	private int FindNextWordOffset (string currentLine)
	{
		return FindWords (currentLine)
			.Where (i => i > current_pos.Offset)
			.DefaultIfEmpty (currentLine.Length)
			.First ();
	}

	public void PerformBackspace (bool control)
	{
		if (HasSelection ()) {
			DeleteSelection ();
			return;
		}

		if (current_pos.Offset == 0 && current_pos.Line > 0) {
			// We're at the beginning of a line and there's
			// a line above us, go to the end of the prior line
			int prevLength = lines[current_pos.Line - 1].Length;
			lines[current_pos.Line - 1] += lines[current_pos.Line];
			lines.RemoveAt (current_pos.Line);
			current_pos = new (
				line: current_pos.Line - 1,
				offset: prevLength);
		} else if (current_pos.Offset > 0) {
			// We're in the middle of a line
			if (control) {
				// Delete the previous word.
				string currentLine = lines[current_pos.Line];
				int newOffset = FindPreviousWordOffset (currentLine);
				while (current_pos.Offset > newOffset) {
					RemoveNextTextElement (-1);
				}
			} else {
				// Delete the previous text element.
				RemoveNextTextElement (-1);
			}
		}

		selection_start = current_pos;
		State = TextMode.Uncommitted;
		OnModified ();
	}

	public void PerformDelete ()
	{
		if (HasSelection ()) {
			DeleteSelection ();
			return;
		}

		string currentLine = lines[current_pos.Line];
		if (current_pos.Offset < currentLine.Length) {
			// In the middle of a line - delete the next text element.
			RemoveNextTextElement (0);
		} else if (current_pos.Line < lines.Count - 1) {
			// End of a line - merge strings.
			lines[current_pos.Line] += lines[current_pos.Line + 1];
			lines.RemoveAt (current_pos.Line + 1);
		}

		State = TextMode.Uncommitted;
		OnModified ();
	}

	public void PerformLeft (bool control, bool shift)
	{
		// Move caret to the left, or to the previous line
		if (current_pos.Offset > 0) {
			string currentLine = lines[current_pos.Line];
			if (control) {
				// Move to the beginning of the previous word.
				int newOffset = FindPreviousWordOffset (currentLine);
				current_pos = current_pos.WithOffset (newOffset);
			} else {
				(var elements, var elementIndex) = FindTextElementIndex (currentLine, current_pos.Offset);
				current_pos = current_pos.WithOffset (elements[elementIndex - 1]);
			}
		} else if (current_pos.Offset == 0 && current_pos.Line > 0) {
			current_pos = new (
				line: current_pos.Line - 1,
				offset: lines[current_pos.Line].Length);
		}

		if (!shift)
			ClearSelection ();
	}

	public void PerformRight (bool control, bool shift)
	{
		string currentLine = lines[current_pos.Line];
		if (current_pos.Offset < currentLine.Length) {
			if (control) {
				// Move to the beginning of the next word.
				int newOffset = FindNextWordOffset (currentLine);
				current_pos = current_pos.WithOffset (newOffset);
			} else {
				// Move to the next text element.
				(var elements, var elementIndex) = FindTextElementIndex (currentLine, current_pos.Offset);
				int newOffset =
					elementIndex < elements.Length - 1
					? elements[elementIndex + 1]
					: currentLine.Length;
				current_pos = current_pos.WithOffset (newOffset);
			}

		} else if (current_pos.Offset == currentLine.Length && current_pos.Line < lines.Count - 1) {
			current_pos = new (
				line: current_pos.Line + 1,
				offset: 0);
		}

		if (!shift)
			ClearSelection ();
	}

	public void PerformHome (bool control, bool shift)
	{
		// For Ctrl-Home, we go to the top line
		if (control)
			current_pos = current_pos.WithLine (0);

		// Go to the beginning of the line
		current_pos = current_pos.WithOffset (0);

		if (!shift)
			ClearSelection ();
	}

	public void PerformEnd (bool control, bool shift)
	{
		// For Ctrl-End, we go to the last line
		if (control)
			current_pos = current_pos.WithLine (lines.Count - 1);

		// Go to the end of the line
		current_pos = current_pos.WithOffset (lines[current_pos.Line].Length);

		if (!shift)
			ClearSelection ();
	}

	public void PerformUp (bool shift)
	{
		if (current_pos.Line == 0)
			return;

		MoveToAdjacentLine (-1);

		if (!shift)
			ClearSelection ();

	}

	public void PerformDown (bool shift)
	{
		if (current_pos.Line >= LineCount - 1)
			return;

		MoveToAdjacentLine (1);

		if (!shift)
			ClearSelection ();
	}

	public void PerformCopy (Gdk.Clipboard clipboard)
	{
		// Note we could set the clipboard text to empty if nothing is selected,
		// but this seems to crash on Windows (bug 2070035)
		if (!HasSelection ())
			return;

		StringBuilder strbld = new ();

		TextPosition start = TextPosition.Min (current_pos, selection_start);
		TextPosition end = TextPosition.Max (current_pos, selection_start);
		ForeachLine (start, end, (currentLinePos, strpos, endpos) => {
			if (endpos - strpos > 0)
				strbld.AppendLine (lines[currentLinePos][strpos..endpos]);
			else if (endpos == strpos)
				strbld.AppendLine ();
		});
		strbld.Remove (strbld.Length - Environment.NewLine.Length, Environment.NewLine.Length);

		clipboard.SetText (strbld.ToString ());
	}

	public void PerformCut (Gdk.Clipboard clipboard)
	{
		PerformCopy (clipboard);
		if (HasSelection ())
			DeleteSelection ();
	}

	/// <summary>
	/// Pastes text from the clipboard.
	/// </summary>
	public async Task<bool> PerformPaste (Gdk.Clipboard clipboard)
	{
		string? txt;
		try {
			txt = await clipboard.ReadTextAsync ();
		} catch (GLib.GException) {
			// The clipboard probably contained an image.
			return false;
		}

		if (string.IsNullOrEmpty (txt))
			return false;

		if (HasSelection ())
			DeleteSelection ();

		IReadOnlyList<string> ins_lines = txt.Split (
			Environment.NewLine.ToCharArray (),
			StringSplitOptions.RemoveEmptyEntries);
		string endline = lines[current_pos.Line][current_pos.Offset..];
		lines[current_pos.Line] = lines[current_pos.Line][..current_pos.Offset];
		bool first = true;
		foreach (string ins_txt in ins_lines) {
			if (!first) {
				int newLine = current_pos.Line + 1;
				int newOffset = ins_txt.Length;
				current_pos = new (
					line: newLine,
					offset: newOffset);
				lines.Insert (newLine, ins_txt);
			} else {
				first = false;
				lines[current_pos.Line] += ins_txt;
				current_pos = current_pos.WithOffset (current_pos.Offset + ins_txt.Length);
			}
		}
		lines[current_pos.Line] += endline;

		selection_start = current_pos;
		State = TextMode.Uncommitted;

		OnModified ();

		return true;
	}

	delegate void Action (int currentLine, int strartPosition, int endPosition);

	private void ForeachLine (TextPosition start, TextPosition end, Action action)
	{
		if (start.CompareTo (end) > 0)
			throw new ArgumentException ("Invalid start position", nameof (start));

		while (start.Line < end.Line) {
			action (start.Line, start.Offset, lines[start.Line].Length);
			start = new (
				line: start.Line + 1,
				offset: 0);
		}

		action (start.Line, start.Offset, end.Offset);
	}

	public TextPosition UTF8IndexToPosition (int index)
	{
		int current = 0;
		int line = 0;
		foreach (string s in lines) {

			// It's past this line, move along
			if (current + StringToUTF8Size (s) < index) {
				current += StringToUTF8Size (s) + 1;
				line++;
				continue;
			}

			// It's in this line
			int offset = index - current;
			offset = UTF8OffsetToCharacterOffset (lines[line], offset);
			return new (line, offset);
		}

		// It's below all of our lines, return the end of the last line
		return new (
			line: lines.Count - 1,
			offset: lines[^1].Length);
	}

	public int PositionToUTF8Index (TextPosition p)
	{
		int index = 0;

		for (int i = 0; i < p.Line; i++)
			index += StringToUTF8Size (lines[i]) + 1;

		index += StringToUTF8Size (lines[p.Line][..p.Offset]);

		return index;
	}

	private static int StringToUTF8Size (string s)
	{
		UTF8Encoding enc = new ();
		return (enc.GetBytes (s)).Length;
	}

	private static int UTF8OffsetToCharacterOffset (string s, int offset)
	{
		int i;
		for (i = 0; i < offset; i++) {
			if (StringToUTF8Size (s[..i]) >= offset) break;
		}
		return i;
	}

	private void OnModified ()
	{
		Modified?.Invoke (this, EventArgs.Empty);
	}

	private void DeleteSelection ()
	{
		TextPosition start = TextPosition.Min (current_pos, selection_start);
		TextPosition end = TextPosition.Max (current_pos, selection_start);

		DeleteText (start, end);

		current_pos = start;

		ClearSelection ();
	}

	private void DeleteText (TextPosition start, TextPosition end)
	{
		if (start.CompareTo (end) >= 0)
			throw new ArgumentException ("Invalid start position", nameof (start));

		lines[start.Line] =
			lines[start.Line][..start.Offset]
			+ lines[end.Line][end.Offset..];

		// If this was a multi-line delete, remove all lines in between,
		// including the end line.
		lines.RemoveRange (
			start.Line + 1,
			end.Line - start.Line);

		State = TextMode.Uncommitted;

		OnModified ();
	}

	private bool HasSelection ()
	{
		return selection_start != current_pos;
	}

	private void ClearSelection ()
	{
		selection_start = current_pos;
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
		string line = lines[current_pos.Line];
		StringInfo lineInfo = new (line);

		(var elements, var elementIndex) = FindTextElementIndex (line, current_pos.Offset);
		elementIndex += elementOffset;
		string before = lineInfo.SubstringByTextElements (0, elementIndex);
		string after = (elementIndex < elements.Length - 1) ? lineInfo.SubstringByTextElements (elementIndex + 1) : string.Empty;

		lines[current_pos.Line] = before + after;
		current_pos = current_pos.WithOffset (before.Length);
	}

	private void MoveToAdjacentLine (int lineOffset)
	{
		string currentLine = lines[current_pos.Line];
		string nextLine = lines[current_pos.Line + lineOffset];

		// Remain at the same text element index (if possible) when changing lines.
		(_, var elementIndex) = FindTextElementIndex (currentLine, current_pos.Offset);
		var nextLineElements = StringInfo.ParseCombiningCharacters (nextLine);

		int newOffset =
			elementIndex < nextLineElements.Length
			? nextLineElements[elementIndex]
			: nextLine.Length;

		current_pos = new (
			line: current_pos.Line + lineOffset,
			offset: newOffset);
	}

	/// <summary>
	/// Returns the index of the first character of each word in the line.
	/// </summary>
	private static IEnumerable<int> FindWords (string s)
		=> FindWordsRegex ().Matches (s).Select (m => m.Index);

	[GeneratedRegex (@"\b\w")] private static partial Regex FindWordsRegex ();
}
