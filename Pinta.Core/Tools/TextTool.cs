// 
// TextTool.cs
//  
// Author:
//       dufoli <>
// 
// Copyright (c) 2010 dufoli
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
//using Cairo;
using System.Collections.Generic;
using Gdk;
using Gtk;

namespace Pinta.Core
{
	public class TextTool : BaseTool
	{
		private enum EditingMode
		{
			NotEditing,
			EmptyEdit,
			Editing
		}

		private enum TextAlignment
		{
			Right,
			Center,
			Left
		}

		public override string Name {
			get { return "Text"; }
		}
		public override string Icon {
			get { return "Tools.Text.png"; }
		}

		public override string StatusBarText {
			get { return "Write text."; }
		}
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.T; } }


		//private string statusBarTextFormat = PdnResources.GetString("TextTool.StatusText.TextInfo.Format");
		private Cairo.PointD startMouseXY;
		private Point startClickPoint;
		private bool tracking;

		//private MoveNubRenderer moveNub;
		private int ignoreRedraw;
		private EditingMode mode;
		private List<string> lines;
		private int linePos;
		private int textPos;
		private Point clickPoint;
		private IrregularSurface saved;
		private const int cursorInterval = 300;
		private bool pulseEnabled;
		private System.DateTime startTime;
		private bool lastPulseCursorState;
		private bool enableNub = true;

		private CompoundHistoryItem currentHA;

		private bool controlKeyDown = false;
		private DateTime controlKeyDownTime = DateTime.MinValue;
		private readonly TimeSpan controlKeyDownThreshold = new TimeSpan (0, 0, 0, 0, 400);

		/*public override Gdk.Cursor DefaultCursor {
			get {
				return new Gdk.Cursor(;
			}
		}*/

		protected override void OnActivated ()
		{
			//PdnBaseForm.RegisterFormHotKey(Gdk.Key.Back, OnBackspaceTyped);
			
			base.OnActivated ();
			
			PintaCore.Palette.PrimaryColorChanged += HandlePintaCorePalettePrimaryColorChanged;
			
			//this.textToolCursor = new Gdk.Cursor (PintaCore.Chrome.DrawingArea.Display, PintaCore.Resources.GetIcon ("Tools.Text.png"), 0, 0);
			
			//this.Cursor = this.textToolCursor;
			
			
			mode = EditingMode.NotEditing;
			
			//font = AppEnvironment.FontInfo.CreateFont();
			//alignment = AppEnvironment.TextAlignment;
			
			
		}

		//this.threadPool = new System.Threading.ThreadPool ();

/*this.moveNub = new MoveNubRenderer(this.RendererList);
            this.moveNub.Shape = MoveNubShape.Compass;
            this.moveNub.Size = new SizeF(10, 10);
            this.moveNub.Visible = false;
            this.RendererList.Add(this.moveNub, false);
            */		
		
		#region ToolBar
		
		private ToolBarLabel font_label;
		private ToolBarComboBox font_combo;
		private ToolBarComboBox size_combo;
		private ToolBarToggleButton bold_btn;
		private ToolBarToggleButton italic_btn;
		private ToolBarToggleButton underscore_btn;
		private ToolBarToggleButton left_alignment_btn;
		private ToolBarToggleButton center_alignment_btn;
		private ToolBarToggleButton Right_alignment_btn;

		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			//TODO
			//fontSmoothing
			
			base.OnBuildToolBar (tb);
			
			if (font_label == null)
				font_label = new ToolBarLabel (" Font: ");
			
			tb.AppendItem (font_label);
			
			using (Pango.Context c = PangoHelper.ContextGet ()) {
				List<Pango.FontFamily> fonts = new List<Pango.FontFamily> (c.Families);
				
				
				List<string> entries = new List<string> ();
				fonts.ForEach (f => entries.Add (f.Name));
				entries.Sort ();
				
				//by default Arial!
				int index = entries.IndexOf ("Arial");
				if (index < 0)
					index = 0;
				//FIXME: I put a try to handle a bug when I am debugging on monodevelop there is a an exception
				//this exception do not occure when I put a try catch ;(
				try {
					if (font_combo == null) {
						font_combo = new ToolBarComboBox (100, index, false, entries.ToArray ());
						font_combo.ComboBox.Changed += HandleFontChanged;
					}
					
					tb.AppendItem (font_combo);
					
					//size depend on font and modifier (italic, bold,...)
					Pango.FontFamily fam = fonts.Find (f => f.Name == font_combo.ComboBox.ActiveText);
					
					entries = new List<string> ();
					foreach (int i in GetSizeList (fam.Faces[0])) {
						entries.Add (i.ToString ());
					}
					
					//by default 11!
					index = entries.IndexOf ("11");
					if (index < 0)
						index = 0;
					
					if (size_combo == null) {
						size_combo = new ToolBarComboBox (50, index, true, entries.ToArray ());
						
						size_combo.ComboBox.Changed += HandleSizeChanged;
						(size_combo.ComboBox as Gtk.ComboBoxEntry).Entry.FocusOutEvent += new Gtk.FocusOutEventHandler (HandleFontSizeFocusOut);
						(size_combo.ComboBox as Gtk.ComboBoxEntry).Entry.FocusInEvent += new Gtk.FocusInEventHandler (HandleFontSizeFocusIn);
					}
					
					tb.AppendItem (size_combo);
				} catch (Exception e) {
					Console.WriteLine (e.ToString ());
				}
			}
			tb.AppendItem (new SeparatorToolItem ());
			
			if (bold_btn == null) {
				bold_btn = new ToolBarToggleButton ("Toolbar.Bold.png", "Bold", "Bold the text");
				bold_btn.Toggled += HandleBoldButtonToggled;
			}
			
			tb.AppendItem (bold_btn);
			
			if (italic_btn == null) {
				italic_btn = new ToolBarToggleButton ("Toolbar.Italic.png", "Italic", "Italic the text");
				italic_btn.Toggled += HandleItalicButtonToggled;
				;
			}
			
			tb.AppendItem (italic_btn);
			
			if (underscore_btn == null) {
				underscore_btn = new ToolBarToggleButton ("Toolbar.Underline.png", "Uncerline", "Underline the text");
				underscore_btn.Toggled += HandleUnderscoreButtonToggled;
			}
			
			tb.AppendItem (underscore_btn);
			
			tb.AppendItem (new SeparatorToolItem ());
			
			if (left_alignment_btn == null) {
				left_alignment_btn = new ToolBarToggleButton ("Toolbar.LeftAlignment.png", "Align left", "Align text to left");
				left_alignment_btn.Active = true;
				left_alignment_btn.Toggled += HandleLeftAlignmentButtonToggled;
				;
			}
			
			tb.AppendItem (left_alignment_btn);
			
			if (center_alignment_btn == null) {
				center_alignment_btn = new ToolBarToggleButton ("Toolbar.CenterAlignment.png", "Align center", "Align text to center");
				center_alignment_btn.Toggled += HandleCenterAlignmentButtonToggled;
				;
			}
			
			tb.AppendItem (center_alignment_btn);
			
			if (Right_alignment_btn == null) {
				Right_alignment_btn = new ToolBarToggleButton ("Toolbar.RightAlignment.png", "Align right", "Align text to right");
				Right_alignment_btn.Toggled += HandleRightAlignmentButtonToggled;
				;
			}
			
			tb.AppendItem (Right_alignment_btn);
		}

		string temp_size;
		void HandleFontSizeFocusIn (object o, FocusInEventArgs args)
		{
			size_combo.ComboBox.Changed -= HandleSizeChanged;
			temp_size = size_combo.ComboBox.ActiveText;
		}

		void HandleFontSizeFocusOut (object o, FocusOutEventArgs args)
		{
			string text = size_combo.ComboBox.ActiveText;
			int size;

			if (!int.TryParse (text, out size)) {
				(size_combo.ComboBox as Gtk.ComboBoxEntry).Entry.Text = temp_size;
				return;
			}
			
			PintaCore.Chrome.DrawingArea.GrabFocus ();
			if (mode != EditingMode.NotEditing) {
				this.sizes = null;
				RedrawText (true);
			}
			size_combo.ComboBox.Changed += HandleSizeChanged;
		}

		void HandleFontChanged (object sender, EventArgs e)
		{
			PintaCore.Chrome.DrawingArea.GrabFocus ();
			UpdateFontSizes ();
			if (mode != EditingMode.NotEditing) {
				this.sizes = null;
				RedrawText (true);
			}
		}

		void UpdateFontSizes ()
		{
			List<int> lst;
			string oldval = size_combo.ComboBox.ActiveText;
			ListStore model = (ListStore)size_combo.ComboBox.Model;
			model.Clear ();
			using (Pango.Context c = PangoHelper.ContextGet ()) {
				List<Pango.FontFamily> fonts = new List<Pango.FontFamily> (c.Families);
				Pango.FontFamily fam = fonts.Find (f => f.Name == font_combo.ComboBox.ActiveText);
				lst = GetSizeList (fam.Faces[0]);
			}
			foreach (int i in lst)
				size_combo.ComboBox.AppendText (i.ToString ());
			
			int index = lst.IndexOf (int.Parse (oldval));
			if (index == -1)
				index = 0;
			
			size_combo.ComboBox.Active = index;
		}

		void HandleSizeChanged (object sender, EventArgs e)
		{
			PintaCore.Chrome.DrawingArea.GrabFocus ();
			if (mode != EditingMode.NotEditing) {
				this.sizes = null;
				RedrawText (true);
			}
		}

		private Pango.FontFamily FontFamily {
			get {
				List<Pango.FontFamily> fonts = new List<Pango.FontFamily> (PintaCore.Chrome.DrawingArea.PangoContext.Families);
				return fonts.Find (f => f.Name == font_combo.ComboBox.ActiveText);
			}
		}


		private int FontSize {
			get { return int.Parse (size_combo.ComboBox.ActiveText); }
		}

		private TextAlignment Alignment {
			get {
				if (Right_alignment_btn.Active)
					return TextAlignment.Right; else if (center_alignment_btn.Active)
					return TextAlignment.Center;
				else
					return TextAlignment.Left;
			}
		}

		private Cairo.FontSlant FontSlant {
			get {
				if (italic_btn.Active)
					return Cairo.FontSlant.Italic;
				else
					return Cairo.FontSlant.Normal;
			}
		}

		private Cairo.FontWeight FontWeight {
			get {
				if (bold_btn.Active)
					return Cairo.FontWeight.Bold;
				else
					return Cairo.FontWeight.Normal;
			}
		}

		private string Font {
			get { return font_combo.ComboBox.ActiveText; }
		}

		private Cairo.TextExtents TextExtents (Cairo.Context g, string str)
		{
			g.SelectFontFace (font_combo.ComboBox.ActiveText, FontSlant, FontWeight);
			g.SetFontSize (FontSize);
			
			return g.TextExtents (str);
		}

		private Cairo.FontExtents FontExtents (Cairo.Context g, string str)
		{
			g.SelectFontFace (font_combo.ComboBox.ActiveText, FontSlant, FontWeight);
			g.SetFontSize (FontSize);
			
			return g.FontExtents;
		}

		private int FontHeight {
			get { return StringSize ("a").Height; }
		}


		void HandlePintaCorePalettePrimaryColorChanged (object sender, EventArgs e)
		{
			if (mode != EditingMode.NotEditing) {
				RedrawText (true);
			}
		}

		void HandleLeftAlignmentButtonToggled (object sender, EventArgs e)
		{
			if (left_alignment_btn.Active) {
				Right_alignment_btn.Active = false;
				center_alignment_btn.Active = false;
			} else if (!Right_alignment_btn.Active && !center_alignment_btn.Active) {
				left_alignment_btn.Active = true;
			}
			if (mode != EditingMode.NotEditing) {
				this.sizes = null;
				RedrawText (true);
			}
		}

		void HandleCenterAlignmentButtonToggled (object sender, EventArgs e)
		{
			if (center_alignment_btn.Active) {
				Right_alignment_btn.Active = false;
				left_alignment_btn.Active = false;
			} else if (!Right_alignment_btn.Active && !left_alignment_btn.Active) {
				center_alignment_btn.Active = true;
			}
			if (mode != EditingMode.NotEditing) {
				this.sizes = null;
				RedrawText (true);
			}
		}

		void HandleRightAlignmentButtonToggled (object sender, EventArgs e)
		{
			if (Right_alignment_btn.Active) {
				center_alignment_btn.Active = false;
				left_alignment_btn.Active = false;
			} else if (!center_alignment_btn.Active && !left_alignment_btn.Active) {
				Right_alignment_btn.Active = true;
			}
			if (mode != EditingMode.NotEditing) {
				this.sizes = null;
				RedrawText (true);
			}
		}


		void HandleUnderscoreButtonToggled (object sender, EventArgs e)
		{
			if (mode != EditingMode.NotEditing) {
				RedrawText (true);
			}
		}

		void HandleItalicButtonToggled (object sender, EventArgs e)
		{
			if (mode != EditingMode.NotEditing) {
				RedrawText (true);
			}
		}

		void HandleBoldButtonToggled (object sender, EventArgs e)
		{
			if (mode != EditingMode.NotEditing) {
				RedrawText (true);
			}
		}

		unsafe private List<int> GetSizeList (Pango.FontFace fontFace)
		{
			List<int> result = new List<int> ();
			int sizes;
			int nsizes;
			fontFace.ListSizes (out sizes, out nsizes);
			if (nsizes == 0)
				result.AddRange (new int[] { 6, 7, 8, 9, 10, 11, 12, 14, 15, 16,
				18, 20, 22, 24, 26, 28, 32, 36, 40, 44,
				48, 54, 60, 66, 72, 80, 88, 96 });
			else {
				for (int i = 0; i < nsizes; i++) {
					result.Add (*(&sizes + 4 * i));
				}
			}
			return result;
		}

		#endregion

		protected override void OnDeactivated ()
		{
			//PdnBaseForm.UnregisterFormHotKey(Gdk.Key.Back, OnBackspaceTyped);
			
			base.OnDeactivated ();
			PintaCore.Palette.PrimaryColorChanged -= HandlePintaCorePalettePrimaryColorChanged;
			
			switch (mode) {
			case EditingMode.Editing:
				SaveHistoryMemento ();
				break;
			
			case EditingMode.EmptyEdit:
				RedrawText (false);
				break;
			
			case EditingMode.NotEditing:
				break;
			default:
				
				throw new System.ComponentModel.InvalidEnumArgumentException ("Invalid Editing Mode");
			}
			
			if (saved != null) {
				saved.Dispose ();
				saved = null;
			}
			
			StopEditing ();
			//this.threadPool = null;
		}
		/*
            this.RendererList.Remove(this.moveNub);
            this.moveNub.Dispose();
            this.moveNub = null;

            if (this.textToolCursor != null)
            {
                this.textToolCursor.Dispose();
                this.textToolCursor = null;
            }
            */

		private void StopEditing ()
		{
			PintaCore.Layers.ToolLayer.Clear ();
			PintaCore.Layers.ToolLayer.Hidden = true;
			mode = EditingMode.NotEditing;
			pulseEnabled = false;
			lines = null;
			//this.moveNub.Visible = false;
		}

		private void StartEditing ()
		{
			this.linePos = 0;
			this.textPos = 0;
			this.lines = new List<string> ();
			this.sizes = null;
			this.lines.Add (string.Empty);
			this.startTime = DateTime.Now;
			this.mode = EditingMode.EmptyEdit;
			this.pulseEnabled = true;
			PintaCore.Layers.ToolLayer.Hidden = false;
			//UpdateStatusText();
		}
		/*
        private void UpdateStatusText()
        {
            string text;
            ImageResource image;

            if (this.tracking)
            {
                text = GetStatusBarXYText();
                image = Image;
            }
            else
            {
                text = PdnResources.GetString("TextTool.StatusText.StartTyping");
                image = null;
            }

            SetStatus(image, text);
        }
		 */
		private void PerformEnter ()
		{
			string currentLine = (string)this.lines[this.linePos];
			
			if (this.textPos == currentLine.Length) {
				// If we are at the end of a line, insert an empty line at the next line
				this.lines.Insert (this.linePos + 1, string.Empty);
			} else {
				this.lines.Insert (this.linePos + 1, currentLine.Substring (textPos, currentLine.Length - this.textPos));
				this.lines[this.linePos] = ((string)this.lines[this.linePos]).Substring (0, this.textPos);
			}
			
			this.linePos++;
			this.textPos = 0;
			this.sizes = null;
			
		}

		private void PerformBackspace ()
		{
			if (textPos == 0 && linePos > 0) {
				int ntp = ((string)lines[linePos - 1]).Length;
				
				lines[linePos - 1] = ((string)lines[linePos - 1]) + ((string)lines[linePos]);
				lines.RemoveAt (linePos);
				linePos--;
				textPos = ntp;
				sizes = null;
			} else if (textPos > 0) {
				string ln = (string)lines[linePos];
				
				// If we are at the end of a line, we don't need to place a compound string
				if (textPos == ln.Length) {
					lines[linePos] = ln.Substring (0, ln.Length - 1);
				} else {
					lines[linePos] = ln.Substring (0, textPos - 1) + ln.Substring (textPos);
				}
				
				textPos--;
				sizes = null;
			}
		}

		private void PerformControlBackspace ()
		{
			if (textPos == 0 && linePos > 0) {
				PerformBackspace ();
			} else if (textPos > 0) {
				string currentLine = (string)lines[linePos];
				int ntp = textPos;
				
				if (System.Char.IsLetterOrDigit (currentLine[ntp - 1])) {
					while (ntp > 0 && (System.Char.IsLetterOrDigit (currentLine[ntp - 1]))) {
						ntp--;
					}
				} else if (System.Char.IsWhiteSpace (currentLine[ntp - 1])) {
					while (ntp > 0 && (System.Char.IsWhiteSpace (currentLine[ntp - 1]))) {
						ntp--;
					}
				} else if (System.Char.IsPunctuation (currentLine[ntp - 1])) {
					while (ntp > 0 && (System.Char.IsPunctuation (currentLine[ntp - 1]))) {
						ntp--;
					}
				} else {
					ntp--;
				}
				
				lines[linePos] = currentLine.Substring (0, ntp) + currentLine.Substring (textPos);
				textPos = ntp;
				sizes = null;
			}
		}

		private void PerformDelete ()
		{
			// Where are we?!
			if ((linePos == lines.Count - 1) && (textPos == ((string)lines[lines.Count - 1]).Length)) {
				// If the cursor is at the end of the text block
				return;
			} else if (textPos == ((string)lines[linePos]).Length) {
				// End of a line, must merge strings
				lines[linePos] = ((string)lines[linePos]) + ((string)lines[linePos + 1]);
				lines.RemoveAt (linePos + 1);
			} else {
				// Middle of a line somewhere
				lines[linePos] = ((string)lines[linePos]).Substring (0, textPos) + ((string)lines[linePos]).Substring (textPos + 1);
			}
			
			// Check for state change
			if (lines.Count == 1 && ((string)lines[0]) == "") {
				mode = EditingMode.EmptyEdit;
			}
			
			sizes = null;
		}

		private void PerformControlDelete ()
		{
			// where are we?!
			if ((linePos == lines.Count - 1) && (textPos == ((string)lines[lines.Count - 1]).Length)) {
				// If the cursor is at the end of the text block
				return;
			} else if (textPos == ((string)lines[linePos]).Length) {
				// End of a line, must merge strings
				lines[linePos] = ((string)lines[linePos]) + ((string)lines[linePos + 1]);
				lines.RemoveAt (linePos + 1);
			} else {
				// Middle of a line somewhere
				int ntp = textPos;
				string currentLine = (string)lines[linePos];
				
				if (System.Char.IsLetterOrDigit (currentLine[ntp])) {
					while (ntp < currentLine.Length && (System.Char.IsLetterOrDigit (currentLine[ntp]))) {
						currentLine = currentLine.Remove (ntp, 1);
					}
				} else if (System.Char.IsWhiteSpace (currentLine[ntp])) {
					while (ntp < currentLine.Length && (System.Char.IsWhiteSpace (currentLine[ntp]))) {
						currentLine = currentLine.Remove (ntp, 1);
					}
				} else if (System.Char.IsPunctuation (currentLine[ntp])) {
					while (ntp < currentLine.Length && (System.Char.IsPunctuation (currentLine[ntp]))) {
						currentLine = currentLine.Remove (ntp, 1);
					}
				} else {
					ntp--;
				}
				
				lines[linePos] = currentLine;
			}
			
			// Check for state change
			if (lines.Count == 1 && ((string)lines[0]) == "") {
				mode = EditingMode.EmptyEdit;
			}
			
			sizes = null;
		}

		private void PerformLeft ()
		{
			if (textPos > 0) {
				textPos--;
			} else if (textPos == 0 && linePos > 0) {
				linePos--;
				textPos = ((string)lines[linePos]).Length;
			}
		}

		private void PerformControlLeft ()
		{
			if (textPos > 0) {
				int ntp = textPos;
				string currentLine = (string)lines[linePos];
				
				if (System.Char.IsLetterOrDigit (currentLine[ntp - 1])) {
					while (ntp > 0 && (System.Char.IsLetterOrDigit (currentLine[ntp - 1]))) {
						ntp--;
					}
				} else if (System.Char.IsWhiteSpace (currentLine[ntp - 1])) {
					while (ntp > 0 && (System.Char.IsWhiteSpace (currentLine[ntp - 1]))) {
						ntp--;
					}
				} else if (ntp > 0 && System.Char.IsPunctuation (currentLine[ntp - 1])) {
					while (ntp > 0 && System.Char.IsPunctuation (currentLine[ntp - 1])) {
						ntp--;
					}
				} else {
					ntp--;
				}
				
				textPos = ntp;
			} else if (textPos == 0 && linePos > 0) {
				linePos--;
				textPos = ((string)lines[linePos]).Length;
			}
		}

		private void PerformRight ()
		{
			if (textPos < ((string)lines[linePos]).Length) {
				textPos++;
			} else if (textPos == ((string)lines[linePos]).Length && linePos < lines.Count - 1) {
				linePos++;
				textPos = 0;
			}
		}

		private void PerformControlRight ()
		{
			if (textPos < ((string)lines[linePos]).Length) {
				int ntp = textPos;
				string currentLine = (string)lines[linePos];
				
				if (System.Char.IsLetterOrDigit (currentLine[ntp])) {
					while (ntp < currentLine.Length && (System.Char.IsLetterOrDigit (currentLine[ntp]))) {
						ntp++;
					}
				} else if (System.Char.IsWhiteSpace (currentLine[ntp])) {
					while (ntp < currentLine.Length && (System.Char.IsWhiteSpace (currentLine[ntp]))) {
						ntp++;
					}
				} else if (ntp > 0 && System.Char.IsPunctuation (currentLine[ntp])) {
					while (ntp < currentLine.Length && System.Char.IsPunctuation (currentLine[ntp])) {
						ntp++;
					}
				} else {
					ntp++;
				}
				
				textPos = ntp;
			} else if (textPos == ((string)lines[linePos]).Length && linePos < lines.Count - 1) {
				linePos++;
				textPos = 0;
			}
		}

		private void PerformUp ()
		{
			Point p = TextPositionToPoint (new Position (linePos, textPos));
			p.Y -= this.sizes[0].Height;
			//font.Height;
			Position np = PointToTextPosition (p);
			linePos = np.Line;
			textPos = np.Offset;
		}

		private void PerformDown ()
		{
			if (linePos == lines.Count - 1) {
				// last line -> don't do squat
			} else {
				Point p = TextPositionToPoint (new Position (linePos, textPos));
				p.Y += this.sizes[0].Height;
				//font.Height;
				Position np = PointToTextPosition (p);
				linePos = np.Line;
				textPos = np.Offset;
			}
		}

		private Point GetUpperLeft (Size sz, int line)
		{
			Point p = clickPoint;
			p.Y = (int)(p.Y - (0.5 * sz.Height) + (line * sz.Height));
			
			switch (Alignment) {
			case TextAlignment.Center:
				p.X = (int)(p.X - (0.5) * sz.Width);
				break;
			
			case TextAlignment.Right:
				p.X = (int)(p.X - sz.Width);
				break;
			}
			
			return p;
		}

		private Size StringSize (string s)
		{
			// We measure using a 1x1 device context to avoid performance problems that arise otherwise with large images.
			Cairo.ImageSurface surf = PintaCore.Layers.ToolLayer.Surface;
			Cairo.TextExtents te;
			using (Cairo.Context g = new Cairo.Context (surf)) {
				te = TextExtents (g, s);
			}
			return new Size ((int)te.Width, (int)te.Height);
		}

		private sealed class Position
		{
			private int line;
			public int Line {
				get { return line; }

				set {
					if (value >= 0) {
						line = value;
					} else {
						line = 0;
					}
				}
			}

			private int offset;
			public int Offset {
				get { return offset; }

				set {
					if (value >= 0) {
						offset = value;
					} else {
						offset = 0;
					}
				}
			}

			public Position (int line, int offset)
			{
				this.line = line;
				this.offset = offset;
			}
		}

		private void SaveHistoryMemento ()
		{
			pulseEnabled = false;
			RedrawText (false);
			
			if (saved != null) {
				Region hitTest = Region.Rectangle (PintaCore.Layers.SelectionPath.GetBounds ());
				hitTest.Intersect (saved.Region);
				
				if (hitTest.Clipbox.Width != 0 && hitTest.Clipbox.Height != 0) {
					ClippedSurfaceHistoryItem bha = new ClippedSurfaceHistoryItem (Icon, Name, saved, PintaCore.Layers.CurrentLayerIndex);
					
					if (this.currentHA == null) {
						PintaCore.History.PushNewItem (bha);
					} else {
						this.currentHA.Push (bha);
						this.currentHA = null;
					}
				}
				
				hitTest.Dispose ();
				saved.Dispose ();
				saved = null;
			}
		}

		private void DrawText (Cairo.ImageSurface dst, string textFont, string text, Point pt, Size measuredSize, bool antiAliasing, Cairo.Color color)
		{
			Rectangle dstRect = new Rectangle (pt, measuredSize);
			//Rectangle dstRectClipped = Rectangle.Intersect(dstRect, ScratchSurface.Bounds);
						/*
            if (dstRectClipped.Width == 0 || dstRectClipped.Height == 0)
            {
                return;
            }
			 */
using (Cairo.ImageSurface surface = new Cairo.ImageSurface (Cairo.Format.Argb32, 8, 8)) {
				using (Cairo.Context context = new Cairo.Context (surface)) {
					context.FillRectangle (new Cairo.Rectangle (0, 0, surface.Width, surface.Height), color);
				}
				
				DrawText (dst, textFont, text, pt, measuredSize, antiAliasing, surface);
			}
		}

		unsafe private void DrawText (Cairo.ImageSurface dst, string textFont, string text, Point pt, Size measuredSize, bool antiAliasing, Cairo.ImageSurface brush8x8)
		{
			Point pt2 = pt;
			Size measuredSize2 = measuredSize;
			int offset = FontHeight;
			pt.X -= offset;
			measuredSize.Width += 2 * offset;
			Rectangle dstRect = new Rectangle (pt, measuredSize);
			Rectangle dstRectClipped = Rectangle.Intersect (dstRect, PintaCore.Layers.ToolLayer.Surface.GetBounds ());
			PintaCore.Layers.ToolLayer.Clear ();
			
			if (dstRectClipped.Width == 0 || dstRectClipped.Height == 0) {
				return;
			}
			
			// We only use the first 8,8 of brush
			using (Cairo.Context toolctx = new Cairo.Context (PintaCore.Layers.ToolLayer.Surface)) {
				//toolctx.FillRectangle (new Cairo.Rectangle(0, 0, 800, 600), new Cairo.Color (0, 0, 0));
				toolctx.FillRectangle (dstRect.ToCairoRectangle (), new Cairo.Color (1, 1, 1));
				Cairo.ImageSurface surf = PintaCore.Layers.ToolLayer.Surface;
				//TODO find how create a surface a of a particular area of a bigger surface!
				//for moment work with the whole surface!
				if (measuredSize.Width > 0 && measuredSize.Height > 0) {
					//dstRectClipped
					using (Cairo.Context ctx = new Cairo.Context (PintaCore.Layers.ToolLayer.Surface)) {
						Cairo.TextExtents te = TextExtents (ctx, text);
						//new Cairo.PointD(dstRect.X - dstRectClipped.X + offset, dstRect.Y - dstRectClipped.Y),
						ctx.DrawText (new Cairo.PointD (dstRect.X + offset - te.XBearing, dstRect.Y - te.YBearing), textFont, FontSlant, FontWeight, FontSize, PintaCore.Palette.PrimaryColor, text);
						
						if (underscore_btn.Active) {
							int lineSize = 1;
							Cairo.FontExtents fe = FontExtents (ctx, text);
							ctx.DrawLine (new Cairo.PointD (pt2.X, dstRect.Bottom + fe.Descent), new Cairo.PointD (dstRect.Right - offset, dstRect.Bottom + fe.Descent), PintaCore.Palette.PrimaryColor, lineSize);
						}
					}
					PintaCore.Workspace.Invalidate (dstRectClipped);
				}
				
				// Mask out anything that isn't within the user's clip region (selected region)
				using (Region clip = Region.Rectangle (PintaCore.Layers.SelectionPath.GetBounds ())) {
					clip.Xor (Region.Rectangle (dstRectClipped));
					// invert
					clip.Intersect (Region.Rectangle (new Rectangle (pt, measuredSize)));
					toolctx.FillRegion (clip, new Cairo.Color (1, 1, 1, 1));
				}
				
				int skipX;
				
				if (pt.X < 0) {
					skipX = -pt.X;
				} else {
					skipX = 0;
				}
				
				int xEnd = Math.Min (dst.Width, pt.X + measuredSize.Width);
				
				bool blending = false;
				//AppEnvironment.AlphaBlending;
				//if (dst.IsColumnVisible(pt.X + skipX))
				//{
				for (int y = pt.Y; y < pt.Y + measuredSize.Height; ++y) {
					//if (!dst.IsRowVisible(y))
					//{
					//    continue;
					//}
					
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (pt.X + skipX, y);
					ColorBgra* srcPtr = PintaCore.Layers.ToolLayer.Surface.GetPointAddress (pt.X + skipX, y);
					ColorBgra* brushPtr = brush8x8.GetRowAddressUnchecked (y & 7);
					
					for (int x = pt.X + skipX; x < xEnd; ++x) {
						ColorBgra srcPixel = *srcPtr;
						ColorBgra dstPixel = *dstPtr;
						ColorBgra brushPixel = brushPtr[x & 7];
						
						int alpha = ((255 - srcPixel.R) * brushPixel.A) / 255;
						// we could use srcPixel.R, .G, or .B -- the choice here is arbitrary
						brushPixel.A = (byte)alpha;
						
						// could use R, G, or B -- arbitrary choice
						if (srcPtr->R == 255) {
							// do nothing -- leave dst alone
						} else if (alpha == 255 || !blending) {
							// copy it straight over
							*dstPtr = brushPixel;
						} else {
							// do expensive blending
							*dstPtr = UserBlendOps.NormalBlendOp.ApplyStatic (dstPixel, brushPixel);
						}
						
						++dstPtr;
						++srcPtr;
					}
				}
				//}
				
			}
		}

		/// <summary>
		/// Redraws the Text on the screen
		/// </summary>
		/// <remarks>
		/// assumes that the <b>font</b> and the <b>alignment</b> are already set
		/// </remarks>
		/// <param name="cursorOn"></param>
		private void RedrawText (bool cursorOn)
		{
			Cairo.ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;
			using (Cairo.Context context = new Cairo.Context (surf)) {
				if (this.ignoreRedraw > 0) {
					return;
				}
				
				if (saved != null) {
					saved.Draw (surf);
					PintaCore.Workspace.Invalidate (saved.Region.Clipbox);
					saved.Dispose ();
					saved = null;
				}
				
				// Save the Space behind the lines
				Rectangle[] rects = new Rectangle[lines.Count + 1];
				Point[] localUls = new Point[lines.Count];
				
				// All Lines
				bool recalcSizes = false;
				
				if (this.sizes == null) {
					recalcSizes = true;
					this.sizes = new Size[lines.Count + 1];
				}
				
				if (recalcSizes) {
					for (int i = 0; i < lines.Count; ++i) {
						this.MeasureText (i);
					}
				}
				
				for (int i = 0; i < lines.Count; ++i) {
					Point upperLeft = GetUpperLeft (sizes[i], i);
					localUls[i] = upperLeft;
					Rectangle rect = new Rectangle (upperLeft, sizes[i]);
					rects[i] = rect;
				}
				
				// The Cursor Line
				string cursorLine = ((string)lines[linePos]).Substring (0, textPos);
				Size cursorLineSize;
				Point cursorUL;
				Rectangle cursorRect;
				bool emptyCursorLineFlag;
				
				if (cursorLine.Length == 0) {
					emptyCursorLineFlag = true;
					Size fullLineSize = sizes[linePos];
					cursorLineSize = new Size (2, FontHeight);
					cursorUL = GetUpperLeft (fullLineSize, linePos);
					cursorRect = new Rectangle (cursorUL, cursorLineSize);
				} else if (cursorLine.Length == ((string)lines[linePos]).Length) {
					emptyCursorLineFlag = false;
					cursorLineSize = sizes[linePos];
					cursorUL = localUls[linePos];
					cursorRect = new Rectangle (cursorUL, cursorLineSize);
				} else {
					emptyCursorLineFlag = false;
					cursorLineSize = StringSize (cursorLine);
					cursorUL = localUls[linePos];
					cursorRect = new Rectangle (cursorUL, cursorLineSize);
				}
				
				rects[lines.Count] = cursorRect;
				
				// Account for overhang on italic or fancy fonts
				int offset = FontHeight;
				for (int i = 0; i < rects.Length; ++i) {
					rects[i].X -= offset;
					rects[i].Width += 2 * offset;
				}
				
				// Set the saved region
				saved = new IrregularSurface (surf, Utility.InflateRectangles (rects, 3));
				
				// Draw the Lines
				this.uls = localUls;
				
				for (int i = 0; i < lines.Count; i++)
					this.RenderText (surf, i);
				
				// Draw the Cursor
				if (cursorOn) {
					using (Cairo.Context toolctx = new Cairo.Context (PintaCore.Layers.ToolLayer.Surface)) {
						if (emptyCursorLineFlag) {
							toolctx.FillRectangle (cursorRect.ToCairoRectangle (), PintaCore.Palette.PrimaryColor);
						} else {
							toolctx.DrawLine (new Cairo.PointD (cursorRect.Right, cursorRect.Top), new Cairo.PointD (cursorRect.Right, cursorRect.Bottom), PintaCore.Palette.PrimaryColor, 1);
						}
					}
				}
				
				//PlaceMoveNub();
				//UpdateStatusText();
				PintaCore.Workspace.Invalidate (saved.Region.Clipbox);
				//Update();
			}
		}
		/*
        private string GetStatusBarXYText()
        {
            string unitsAbbreviationXY;
            string xString;
            string yString;

            Document.CoordinatesToStrings(AppWorkspace.Units, this.uls[0].X, this.uls[0].Y, out xString, out yString, out unitsAbbreviationXY);

            string statusBarText = string.Format(
                this.statusBarTextFormat,
                xString,
                unitsAbbreviationXY,
                yString,
                unitsAbbreviationXY);

            return statusBarText;
        }
		*/
		// Only used when measuring via background threads
		private void MeasureText (int lineNumber)
		{
			this.sizes[lineNumber] = StringSize ((string)lines[lineNumber]);
		}

		// Only used when rendering via background threads
		private Point[] uls;

		private Size[] sizes;

		private void RenderText (Cairo.ImageSurface surf, int lineNumber)
		{
			DrawText (surf, this.Font, (string)this.lines[lineNumber], this.uls[lineNumber], this.sizes[lineNumber], false, PintaCore.Palette.PrimaryColor);
			//antialiasing hardcoded for moment
		}
		/*
        private void PlaceMoveNub()
        {
            if (this.uls != null && this.uls.Length > 0)
            {
                Point pt = this.uls[uls.Length - 1];
                pt.X += this.sizes[uls.Length - 1].Width;
                pt.Y += this.sizes[uls.Length - 1].Height;
                pt.X += (int)(10.0 / DocumentWorkspace.ScaleFactor.Ratio);
                pt.Y += (int)(10.0 / DocumentWorkspace.ScaleFactor.Ratio);

                pt.X = (int)Math.Round(Math.Min(this.ra.Surface.Width - this.moveNub.Size.Width, pt.X));
                pt.X = (int)Math.Round(Math.Max(this.moveNub.Size.Width, pt.X));
                pt.Y = (int)Math.Round(Math.Min(this.ra.Surface.Height - this.moveNub.Size.Height, pt.Y));
                pt.Y = (int)Math.Round(Math.Max(this.moveNub.Size.Height, pt.Y));

                this.moveNub.Location = pt;
            }
        }
		 */

		protected override void OnKeyDown (DrawingArea canvas, KeyPressEventArgs args)
		{
			bool flag = OnKeyPress (args.Event.Key, args.Event.State);
			if (flag) {
				args.RetVal = flag;
				return;
			}
			
			switch (args.Event.Key) {
			case 			/*case Gdk.Key.space:
                    if (mode != EditingMode.NotEditing)
                    {
                        // Prevent pan cursor from flicking to 'hand w/ the X' whenever use types a space in their text
                        args.RetVal = true;
                    }
                    break;
				*/Gdk.Key.Control_L:
			case Gdk.Key.Control_R:
				if (!this.controlKeyDown) {
					this.controlKeyDown = true;
					this.controlKeyDownTime = DateTime.Now;
				}
				break;
			
			// Make sure these are not used to scroll the document around
			case Gdk.Key.Home:
			case Gdk.Key.End:
			case Gdk.Key.Next:
			case Gdk.Key.Prior:
				if (this.mode != EditingMode.NotEditing) {
					args.RetVal = OnKeyPress (args.Event.Key, args.Event.State);
				}
				break;
			
			case Gdk.Key.Tab:
				if ((args.Event.State & Gdk.ModifierType.ControlMask) == 0) {
					if (this.mode != EditingMode.NotEditing) {
						args.RetVal = OnKeyPress (args.Event.Key, args.Event.State);
					}
				}
				break;
			
			case Gdk.Key.BackSpace:
			case Gdk.Key.Delete:
				if (this.mode != EditingMode.NotEditing) {
					args.RetVal = OnKeyPress (args.Event.Key, args.Event.State);
				}
				break;
			}
			
			// Ensure text is on screen when they are typing
			
						/*if (this.mode != EditingMode.NotEditing)
            {
                Point p = TextPositionToPoint(new Position(linePos, textPos));
                Point left = TextPositionToPoint(new Position(linePos, 0));
                Rectangle bounds = PintaCore.Chrome.DrawingArea.Allocation;
                bounds.Inflate (-FontHeight, -FontHeight);

                if (!bounds.Contains(p) || bounds.Contains(left))
                {
                    Point newCenterPt = bounds.Center();

                    // horizontally off
                    if (p.X > bounds.Right)
                    {
                        newCenterPt.X += p.X - bounds.Right;
                    }
                    else if (left.X < bounds.Left)
                    {
                        newCenterPt.X += left.X - bounds.Left;
                    }
                
                    // vertically off
                    if (p.Y > bounds.Bottom)
                    {
                        newCenterPt.Y += p.Y - bounds.Bottom;
                    }
                    else if (p.Y < bounds.Top) {
                        newCenterPt.Y += p.Y - bounds.Top;
                    }

                    PintaCore.Workspace.RecenterView(newCenterPt.X, newCenterPt.Y);
                }
            }*/

			//base.OnKeyDown (e);
			//replace with:
this.OnKeyPress (canvas, args);
		}

		protected override void OnKeyUp (DrawingArea canvas, KeyReleaseEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.Control_L:
			case Gdk.Key.Control_R:
				TimeSpan heldDuration = (DateTime.Now - this.controlKeyDownTime);
				
				// If the user taps Ctrl, then we should toggle the visiblity of the moveNub
				if (heldDuration < this.controlKeyDownThreshold) {
					this.enableNub = !this.enableNub;
				}
				
				this.controlKeyDown = false;
				break;
			}
			
			base.OnKeyUp (canvas, args);
		}

		protected void OnKeyPress (DrawingArea canvas, KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.KP_Enter:
			case Gdk.Key.Return:
				if (tracking) {
					args.RetVal = true;
				}
				break;
			
			case Gdk.Key.Escape:
				if (tracking) {
					args.RetVal = true;
				} else {
					if (mode == EditingMode.Editing) {
						SaveHistoryMemento ();
					} else if (mode == EditingMode.EmptyEdit) {
						RedrawText (false);
					}
					
					if (mode != EditingMode.NotEditing) {
						args.RetVal = true;
						StopEditing ();
					}
				}
				
				break;
			}
			bool handled = false;
			if (args.RetVal != null && args.RetVal is bool)
				handled = (bool)args.RetVal;
			
			if (!handled && mode != EditingMode.NotEditing && !tracking) {
				args.RetVal = true;
				
				if (mode == EditingMode.EmptyEdit) {
					mode = EditingMode.Editing;
					CompoundHistoryItem cha = new CompoundHistoryItem (Icon, Name);
					this.currentHA = cha;
					PintaCore.History.PushNewItem (cha);
				}
				
				if ((args.Event.State & ModifierType.ControlMask) == 0 && args.Event.Key != Gdk.Key.Control_L && args.Event.Key != Gdk.Key.Control_R) {
					char ch = (char)args.Event.Key;
					InsertCharIntoString (ch);
					textPos++;
					RedrawText (true);
				}
			}
			
			//base.OnKeyPress (args.Event.Key, args.Event.State);
		}

		protected bool OnKeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			bool keyHandled = true;
			
			if (tracking) {
				keyHandled = false;
			} else if ((modifier & Gdk.ModifierType.Mod1Mask) != 0) {
				// ignore so they can use Alt+#### to type special characters
			} else if (mode != EditingMode.NotEditing) {
				switch (key) {
				case Gdk.Key.BackSpace:
					if ((modifier & Gdk.ModifierType.ControlMask) != 0) {
						PerformControlBackspace ();
					} else {
						PerformBackspace ();
					}
					
					break;
				
				case Gdk.Key.Delete:
					if ((modifier & Gdk.ModifierType.ControlMask) != 0) {
						PerformControlDelete ();
					} else {
						PerformDelete ();
					}
					
					break;
				
				case Gdk.Key.Return:
					PerformEnter ();
					break;
				
				case Gdk.Key.Left:
					if ((modifier & Gdk.ModifierType.ControlMask) != 0) {
						PerformControlLeft ();
					} else {
						PerformLeft ();
					}
					
					break;
				
				case Gdk.Key.Right:
					if ((modifier & Gdk.ModifierType.ControlMask) != 0) {
						PerformControlRight ();
					} else {
						PerformRight ();
					}
					
					break;
				
				case Gdk.Key.Up:
					PerformUp ();
					break;
				
				case Gdk.Key.Down:
					PerformDown ();
					break;
				
				case Gdk.Key.Home:
					if ((modifier & Gdk.ModifierType.ControlMask) != 0) {
						linePos = 0;
					}
					
					textPos = 0;
					break;
				
				case Gdk.Key.End:
					if ((modifier & Gdk.ModifierType.ControlMask) != 0) {
						linePos = lines.Count - 1;
					}
					
					textPos = ((string)lines[linePos]).Length;
					break;
				default:
					
					keyHandled = false;
					break;
				}
				
				this.startTime = DateTime.Now;
				
				if (this.mode != EditingMode.NotEditing && keyHandled) {
					RedrawText (true);
				}
			}
			
			return keyHandled;
		}

		private Point TextPositionToPoint (Position p)
		{
			Point pf = new Point (0, 0);
			
			Size sz = StringSize (((string)lines[p.Line]).Substring (0, p.Offset));
			Size fullSz = StringSize ((string)lines[p.Line]);
			
			switch (Alignment) {
			case TextAlignment.Left:
				pf = new Point (clickPoint.X + sz.Width, clickPoint.Y + (sz.Height * p.Line));
				break;
			
			case TextAlignment.Center:
				pf = new Point (clickPoint.X + (sz.Width - (fullSz.Width / 2)), clickPoint.Y + (sz.Height * p.Line));
				break;
			
			case TextAlignment.Right:
				pf = new Point (clickPoint.X + (sz.Width - fullSz.Width), clickPoint.Y + (sz.Height * p.Line));
				break;
			default:
				
				throw new System.ComponentModel.InvalidEnumArgumentException ("Invalid Alignment");
			}
			
			return pf;
		}

		private int FindOffsetPosition (float offset, string line, int lno)
		{
			for (int i = 0; i < line.Length; i++) {
				Point pf = TextPositionToPoint (new Position (lno, i));
				float dx = pf.X - clickPoint.X;
				
				if (dx >= offset) {
					return i;
				}
			}
			
			return line.Length;
		}

		private Position PointToTextPosition (Point pf)
		{
			int dx = pf.X - clickPoint.X;
			int dy = pf.Y - clickPoint.Y;
			int line = (int)Math.Floor (dy / (float)this.sizes[0].Height);
			
			if (line < 0) {
				line = 0;
			} else if (line >= lines.Count) {
				line = lines.Count - 1;
			}
			
			int offset = FindOffsetPosition (dx, (string)lines[line], line);
			Position p = new Position (line, offset);
			
			if (p.Offset >= ((string)lines[p.Line]).Length) {
				p.Offset = ((string)lines[p.Line]).Length;
			}
			
			return p;
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (tracking) {
				Cairo.PointD delta = new Cairo.PointD (point.X - startMouseXY.X, point.Y - startMouseXY.Y);
				this.clickPoint = new Point ((int)(this.startClickPoint.X + delta.X), (int)(this.startClickPoint.Y + delta.Y));
				RedrawText (false);
				//UpdateStatusText();
			} else {
				/*bool touchingNub = this.moveNub.IsPointTouching(new Point(e.X, e.Y), false);

                if (touchingNub && this.moveNub.Visible)
                {
                    this.Cursor = this.handCursor;
                }
                else
                {
                    this.Cursor = this.textToolCursor;
                }*/				
			}			
			
			base.OnMouseMove (o, args, point);
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			if (tracking) {
				Cairo.PointD delta = new Cairo.PointD (point.X - startMouseXY.X, point.Y - startMouseXY.Y);
				this.clickPoint = new Point ((int)(this.startClickPoint.X + delta.X), (int)(this.startClickPoint.Y + delta.Y));
				RedrawText (false);
				tracking = false;
				//UpdateStatusText();
			}
			
			//base.OnMouseUp (e);
		}

		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			base.OnMouseDown (canvas, args, point);
			
			//bool touchingMoveNub = this.moveNub.IsPointTouching(new Point(e.X, e.Y), false);
			
			//|| touchingMoveNub)) // = right click
			if (this.mode != EditingMode.NotEditing && (args.Event.Button == 3)) {
				this.tracking = true;
				this.startMouseXY = point;
				this.startClickPoint = this.clickPoint;
				//this.Cursor = this.handCursorMouseDown;
				//UpdateStatusText();
			} else if (args.Event.Button == 1) {
				if (saved != null) {
					Rectangle[] rects = saved.Region.GetRectangles ();
					Rectangle bounds = Utility.GetRegionBounds (rects, 0, rects.Length);
					bounds.Inflate (FontHeight, FontHeight);
					
					if (lines != null && bounds.Contains ((int)point.X, (int)point.Y)) {
						Position p = PointToTextPosition (new Point ((int)point.X, (int)(point.Y + (FontHeight / 2))));
						linePos = p.Line;
						textPos = p.Offset;
						RedrawText (true);
						return;
					}
				}
				
				switch (mode) {
				case EditingMode.Editing:
					SaveHistoryMemento ();
					StopEditing ();
					break;
				
				case EditingMode.EmptyEdit:
					RedrawText (false);
					StopEditing ();
					break;
				}
				
				clickPoint = new Point ((int)point.X, (int)point.Y);
				StartEditing ();
				RedrawText (true);
			}
		}

		/*protected override void OnPulse()
        {
            base.OnPulse();

            if (!pulseEnabled)
            {
                return;
            }

            TimeSpan ts = (DateTime.Now - startTime);
            long ms = Utility.TicksToMs(ts.Ticks);
            
            bool pulseCursorState;

            if (0 == ((ms / cursorInterval) % 2))
            {
                pulseCursorState = true;
            }
            else
            {
                pulseCursorState = false;
            }

            pulseCursorState &= this.Focused;

            if (IsFormActive)
            {
                pulseCursorState &= ((ModifierKeys & Gdk.Key.Control) == 0);
            }

            if (pulseCursorState != lastPulseCursorState)
            {
                RedrawText(pulseCursorState);
                lastPulseCursorState = pulseCursorState;
            }

            if (IsFormActive && (ModifierKeys & Gdk.Key.Control) != 0) 
            {
                // hide the nub while Ctrl is held down
                this.moveNub.Visible = false;
            }
            else
            {
                this.moveNub.Visible = true;
            }

            // don't show the nub while the user is moving the text around
            this.moveNub.Visible &= !tracking;

            // don't show the nub when the user has tapped Ctrl
            this.moveNub.Visible &= this.enableNub;

            // Oscillate between 25% and 100% alpha over a period of 2 seconds
            // Alpha value of 100% is sustained for a large duration of this period
            const int period = 10000 * 2000; // 10000 ticks per ms, 2000ms per second
            long tick = ts.Ticks % period;
            double sin = Math.Sin(((double)tick / (double)period) * (2.0 * Math.PI));
            // sin is [-1, +1]

            sin = Math.Min(0.5, sin);
            // sin is [-1, +0.5]

            sin += 1.0;
            // sin is [0, 1.5]

            sin /= 2.0;
            // sin is [0, 0.75]

            sin += 0.25;
            // sin is [0.25, 1]

            if (this.moveNub != null)
            {
                int newAlpha = (int)(sin * 255.0);
                this.moveNub.Alpha = newAlpha;
            }

            PlaceMoveNub();
        }

        protected override void OnPasteQuery(IDataObject data, out bool canHandle)
        {
            base.OnPasteQuery(data, out canHandle);

            if (data.GetDataPresent(DataFormats.StringFormat, true) &&
                this.Active &&
                this.mode != EditingMode.NotEditing)
            {
                canHandle = true;
            }
        }	
		
        protected override void OnPaste(IDataObject data, out bool handled)
        {
            base.OnPaste (data, out handled);

            if (data.GetDataPresent(DataFormats.StringFormat, true) &&
                this.Active &&
                this.mode != EditingMode.NotEditing)
            {
                ++this.ignoreRedraw;
                string text = (string)data.GetData(DataFormats.StringFormat, true);

                foreach (char c in text)
                {
                    if (c == '\n')
                    {
                        this.PerformEnter();
                    }
                    else
                    {
                        this.PerformKeyPress(new KeyPressEventArgs(c));
                    }
                }

                handled = true;
                --this.ignoreRedraw;

                this.RedrawText(false);
            }
        }*/

		private void InsertCharIntoString (char c)
		{
			lines[linePos] = ((string)lines[linePos]).Insert (textPos, c.ToString ());
			this.sizes = null;
		}
		
	}
	/*public TextTool(DocumentWorkspace documentWorkspace)
            : base(documentWorkspace,
                   ImageResource.Get("Icons.TextToolIcon.png"),
                   PdnResources.GetString("TextTool.Name"),
                   PdnResources.GetString("TextTool.HelpText"),
                   't',
                   false,
                   ToolBarConfigItems.Brush | ToolBarConfigItems.Text | ToolBarConfigItems.AlphaBlending | ToolBarConfigItems.Antialiasing)
        {
        }*/	
}
