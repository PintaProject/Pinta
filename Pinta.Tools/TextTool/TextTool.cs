/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using Gdk;
using Gtk;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	[System.ComponentModel.Composition.Export (typeof (BaseTool))]
	public class TextTool : BaseTool
	{
		private Cairo.PointD startMouseXY;
		private Point startClickPoint;
		private bool tracking;
		private Gdk.Cursor cursor_hand;

		private List<string> lines;
		private Point[] uls;
		private Size[] sizes;
		private int linePos;
		private int textPos;

		private int ignoreRedraw;
		private EditingMode mode;
		private Point clickPoint;

		private IrregularSurface saved;
		private CompoundHistoryItem currentHA;

		public override string Name { get { return Catalog.GetString ("Text"); } }
		public override string Icon { get { return "Tools.Text.png"; } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.T; } }
		public override int Priority { get { return 37; } }

		public override string StatusBarText {
			get { return Catalog.GetString ("Left click to place cursor, then type desired text. Text color is primary color."); }
		}

		#region Constructor
		public TextTool ()
		{
			cursor_hand = new Gdk.Cursor (PintaCore.Chrome.DrawingArea.Display, PintaCore.Resources.GetIcon ("Tools.Pan.png"), 0, 0);
		}
		#endregion

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
		private ToolBarLabel spacer_label;

		protected void RenderFont (Gtk.CellLayout layout, Gtk.CellRenderer renderer, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			string fontName = (string)model.GetValue (iter, 0);
			Gtk.CellRendererText cell = renderer as Gtk.CellRendererText;
			cell.Text = fontName;
			cell.Font = string.Format ("{0} 10", fontName);
			cell.Family = fontName;
		}

		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);
			
			if (font_label == null)
				font_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Font")));
			
			tb.AppendItem (font_label);

			if (font_combo == null) {
				var fonts = PintaCore.System.Fonts.GetInstalledFonts ();
				fonts.Sort ();

				// Default to Arial or first in list
				int index = Math.Max (fonts.IndexOf ("Arial"), 0);

				font_combo = new ToolBarComboBox (150, index, false, fonts.ToArray ());
				font_combo.ComboBox.Changed += HandleFontChanged;
				font_combo.ComboBox.SetCellDataFunc (font_combo.CellRendererText, new CellLayoutDataFunc (RenderFont));
			}

			tb.AppendItem (font_combo);

			if (spacer_label == null)
				spacer_label = new ToolBarLabel (" ");

			tb.AppendItem (spacer_label);

			if (size_combo == null) {
				size_combo = new ToolBarComboBox (65, 0, true);

				size_combo.ComboBox.Changed += HandleSizeChanged;
				(size_combo.ComboBox as Gtk.ComboBoxEntry).Entry.FocusOutEvent += new Gtk.FocusOutEventHandler (HandleFontSizeFocusOut);
				(size_combo.ComboBox as Gtk.ComboBoxEntry).Entry.FocusInEvent += new Gtk.FocusInEventHandler (HandleFontSizeFocusIn);
			}

			tb.AppendItem (size_combo);

			UpdateFontSizes ();

			tb.AppendItem (new SeparatorToolItem ());
			
			if (bold_btn == null) {
				bold_btn = new ToolBarToggleButton ("Toolbar.Bold.png", Catalog.GetString ("Bold"), Catalog.GetString ("Bold"));
				bold_btn.Toggled += HandleBoldButtonToggled;
			}
			
			tb.AppendItem (bold_btn);
			
			if (italic_btn == null) {
				italic_btn = new ToolBarToggleButton ("Toolbar.Italic.png", Catalog.GetString ("Italic"), Catalog.GetString ("Italic"));
				italic_btn.Toggled += HandleItalicButtonToggled;
			}
			
			tb.AppendItem (italic_btn);
			
			if (underscore_btn == null) {
				underscore_btn = new ToolBarToggleButton ("Toolbar.Underline.png", Catalog.GetString ("Underline"), Catalog.GetString ("Underline"));
				underscore_btn.Toggled += HandleUnderscoreButtonToggled;
			}
			
			tb.AppendItem (underscore_btn);
			
			tb.AppendItem (new SeparatorToolItem ());
			
			if (left_alignment_btn == null) {
				left_alignment_btn = new ToolBarToggleButton ("Toolbar.LeftAlignment.png", Catalog.GetString ("Left Align"), Catalog.GetString ("Left Align"));
				left_alignment_btn.Active = true;
				left_alignment_btn.Toggled += HandleLeftAlignmentButtonToggled;
			}
			
			tb.AppendItem (left_alignment_btn);
			
			if (center_alignment_btn == null) {
				center_alignment_btn = new ToolBarToggleButton ("Toolbar.CenterAlignment.png", Catalog.GetString ("Center Align"), Catalog.GetString ("Center Align"));
				center_alignment_btn.Toggled += HandleCenterAlignmentButtonToggled;
			}
			
			tb.AppendItem (center_alignment_btn);
			
			if (Right_alignment_btn == null) {
				Right_alignment_btn = new ToolBarToggleButton ("Toolbar.RightAlignment.png", Catalog.GetString ("Right Align"), Catalog.GetString ("Right Align"));
				Right_alignment_btn.Toggled += HandleRightAlignmentButtonToggled;
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
				sizes = null;
				RedrawText (true);
			}
			size_combo.ComboBox.Changed += HandleSizeChanged;
		}

		void HandleFontChanged (object sender, EventArgs e)
		{
			PintaCore.Chrome.DrawingArea.GrabFocus ();
			UpdateFontSizes ();
			if (mode != EditingMode.NotEditing) {
				sizes = null;
				RedrawText (true);
			}
		}

		void UpdateFontSizes ()
		{
			string oldval = size_combo.ComboBox.ActiveText;

			ListStore model = (ListStore)size_combo.ComboBox.Model;
			model.Clear ();

			List<int> sizes = PintaCore.System.Fonts.GetSizes (FontFamily);

			foreach (int i in sizes)
				size_combo.ComboBox.AppendText (i.ToString ());
			
			int index;
			
			if (string.IsNullOrEmpty (oldval))
				index = sizes.IndexOf (12);
			else
				index = sizes.IndexOf (int.Parse (oldval));

			if (index == -1)
				index = 0;
			
			size_combo.ComboBox.Active = index;
		}

		void HandleSizeChanged (object sender, EventArgs e)
		{
			PintaCore.Chrome.DrawingArea.GrabFocus ();
			if (mode != EditingMode.NotEditing) {
				sizes = null;
				RedrawText (true);
			}
		}

		private Pango.FontFamily FontFamily {
			get { return PintaCore.System.Fonts.GetFamily (font_combo.ComboBox.ActiveText); }
		}


		private int FontSize {
			get { return int.Parse (size_combo.ComboBox.ActiveText); }
		}

		private TextAlignment Alignment {
			get {
				if (Right_alignment_btn.Active)
					return TextAlignment.Right; 
				else if (center_alignment_btn.Active)
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
				sizes = null;
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
				sizes = null;
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
				sizes = null;
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
		#endregion

		#region Activation/Deactivation
		protected override void OnActivated ()
		{
			base.OnActivated ();
			
			// We may need to redraw our text when the color changes
			PintaCore.Palette.PrimaryColorChanged += HandlePintaCorePalettePrimaryColorChanged;
			
			// We always start off not in edit mode
			mode = EditingMode.NotEditing;
		}

		protected override void OnDeactivated ()
		{
			base.OnDeactivated ();

			// Stop listening for color change events
			PintaCore.Palette.PrimaryColorChanged -= HandlePintaCorePalettePrimaryColorChanged;
			
			switch (mode) {
				case EditingMode.Editing:
					SaveHistoryMemento ();
					break;
			
				case EditingMode.EmptyEdit:
					RedrawText (false);
					break;
			}
			
			if (saved != null) {
				saved.Dispose ();
				saved = null;
			}
			
			StopEditing ();
		}
		#endregion

		#region Mouse Handlers
		protected override void OnMouseDown (DrawingArea canvas, ButtonPressEventArgs args, Cairo.PointD point)
		{
			// If we're in editing mode, a right click
			// allows you to move the text around
			if (mode != EditingMode.NotEditing && (args.Event.Button == 3)) {
				tracking = true;
				startMouseXY = point;
				startClickPoint = clickPoint;

				SetCursor (cursor_hand);
				return;
			} 
			
			// The user clicked the left mouse button			
			if (args.Event.Button == 1) {
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
					// We were editing, save and stop
					case EditingMode.Editing:
						SaveHistoryMemento ();
						StopEditing ();
						break;

					// We were editing, but nothing had been
					// keyed. Stop editing.
					case EditingMode.EmptyEdit:
						RedrawText (false);
						StopEditing ();
						break;
				}

				// Start editing at the cursor location
				clickPoint = new Point ((int)point.X, (int)point.Y);
				StartEditing ();
				RedrawText (true);
			}
		}

		protected override void OnMouseMove (object o, MotionNotifyEventArgs args, Cairo.PointD point)
		{
			// If we're dragging the text around, do that
			if (tracking) {
				Cairo.PointD delta = new Cairo.PointD (point.X - startMouseXY.X, point.Y - startMouseXY.Y);

				clickPoint = new Point ((int)(startClickPoint.X + delta.X), (int)(startClickPoint.Y + delta.Y));

				RedrawText (false);
			}
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			// If we were dragging the text around, finish that up
			if (tracking) {
				Cairo.PointD delta = new Cairo.PointD (point.X - startMouseXY.X, point.Y - startMouseXY.Y);
				
				clickPoint = new Point ((int)(startClickPoint.X + delta.X), (int)(startClickPoint.Y + delta.Y));

				RedrawText (false);
				tracking = false;
				SetCursor (null);
			}
		}
		#endregion

		#region Keyboard Handlers
		protected override void OnKeyDown (DrawingArea canvas, KeyPressEventArgs args)
		{
			// Give OnKeyPress a chance to handle this event
			bool flag = OnKeyPress (args.Event.Key, args.Event.State);

			if (flag) {
				args.RetVal = flag;
				return;
			}

			switch (args.Event.Key) {

				// Make sure these are not used to scroll the document around
				case Gdk.Key.Home:
				case Gdk.Key.End:
				case Gdk.Key.Next:
				case Gdk.Key.Prior:
					if (mode != EditingMode.NotEditing)
						args.RetVal = OnKeyPress (args.Event.Key, args.Event.State);

					break;

				case Gdk.Key.Tab:
					if ((args.Event.State & Gdk.ModifierType.ControlMask) == 0) {
						if (mode != EditingMode.NotEditing)
							args.RetVal = OnKeyPress (args.Event.Key, args.Event.State);
					}

					break;

				case Gdk.Key.BackSpace:
				case Gdk.Key.Delete:
					if (mode != EditingMode.NotEditing)
						args.RetVal = OnKeyPress (args.Event.Key, args.Event.State);

					break;
			}

			// Replace base.OnKeyDown () with:
			OnKeyPress (canvas, args);
		}

		protected bool OnKeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			bool keyHandled = true;

			if (tracking) {
				// If we are dragging the text, don't process keys
				keyHandled = false;
			} else if ((modifier & Gdk.ModifierType.Mod1Mask) != 0) {
				// Ignore so they can use Alt+#### to type special characters
			} else if (mode != EditingMode.NotEditing) {
				switch (key) {

					case Gdk.Key.BackSpace:
						PerformBackspace ();
						break;

					case Gdk.Key.Delete:
						PerformDelete ();
						break;

					case Gdk.Key.KP_Enter:
					case Gdk.Key.Return:
						PerformEnter ();
						break;

					case Gdk.Key.Left:
						if ((modifier & Gdk.ModifierType.ControlMask) != 0)
							PerformControlLeft ();
						else
							PerformLeft ();

						break;

					case Gdk.Key.Right:
						if ((modifier & Gdk.ModifierType.ControlMask) != 0)
							PerformControlRight ();
						else
							PerformRight ();

						break;

					case Gdk.Key.Up:
						PerformUp ();
						break;

					case Gdk.Key.Down:
						PerformDown ();
						break;

					case Gdk.Key.Home:
						// Go to the top line
						if ((modifier & Gdk.ModifierType.ControlMask) != 0)
							linePos = 0;

						// Go to the beginning of the line
						textPos = 0;
						break;

					case Gdk.Key.End:
						// Go to the last line
						if ((modifier & Gdk.ModifierType.ControlMask) != 0)
							linePos = lines.Count - 1;

						// Go to the end of the line
						textPos = lines[linePos].Length;
						break;

					default:
						// We didn't handle the key
						keyHandled = false;
						break;
				}

				// If we processed a key, update the display
				if (mode != EditingMode.NotEditing && keyHandled)
					RedrawText (true);
			}

			return keyHandled;
		}

		protected void OnKeyPress (DrawingArea canvas, KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
				case Gdk.Key.KP_Enter:
				case Gdk.Key.Return:
					if (tracking)
						args.RetVal = true;

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
					currentHA = cha;
					PintaCore.History.PushNewItem (cha);
				}

				if ((args.Event.State & ModifierType.ControlMask) == 0 && args.Event.Key != Gdk.Key.Control_L && args.Event.Key != Gdk.Key.Control_R) {
					uint ch = Gdk.Keyval.ToUnicode (args.Event.KeyValue);

					if (ch != 0) {
						InsertCharIntoString (ch);
						textPos++;
						RedrawText (true);
					}
				}
			}
		}
		#endregion

		#region Key Methods
		private void PerformEnter ()
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
			sizes = null;
		}

		private void PerformBackspace ()
		{
			// We're at the beginning of a line and there's
			// a line above us, go to the end of the prior line
			if (textPos == 0 && linePos > 0) {
				int ntp = lines[linePos - 1].Length;

				lines[linePos - 1] = lines[linePos - 1] + lines[linePos];
				lines.RemoveAt (linePos);
				linePos--;
				textPos = ntp;
				sizes = null;
			} else if (textPos > 0) {
				// We're in the middle of a line, delete the previous character
				string ln = lines[linePos];

				// If we are at the end of a line, we don't need to place a compound string
				if (textPos == ln.Length)
					lines[linePos] = ln.Substring (0, ln.Length - 1);
				else
					lines[linePos] = ln.Substring (0, textPos - 1) + ln.Substring (textPos);

				textPos--;
				sizes = null;
			}
		}

		private void PerformDelete ()
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

			// Check for state change
			if (lines.Count == 1 && lines[0] == string.Empty)
				mode = EditingMode.EmptyEdit;

			sizes = null;
		}

		private void PerformLeft ()
		{
			// Move caret to the left, or to the previous line
			if (textPos > 0)
				textPos--;
			else if (textPos == 0 && linePos > 0) {
				linePos--;
				textPos = lines[linePos].Length;
			}
		}

		private void PerformControlLeft ()
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

		private void PerformRight ()
		{
			// Move caret to the right, or to the next line
			if (textPos < lines[linePos].Length) {
				textPos++;
			} else if (textPos == lines[linePos].Length && linePos < lines.Count - 1) {
				linePos++;
				textPos = 0;
			}
		}

		private void PerformControlRight ()
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

		private void PerformUp ()
		{
			// Move to the letter above this one
			Point p = TextPositionToPoint (new Position (linePos, textPos));
			p.Y -= sizes[0].Height;

			Position np = PointToTextPosition (p);
			linePos = np.Line;
			textPos = np.Offset;
		}

		private void PerformDown ()
		{
			if (linePos == lines.Count - 1) {
				// last line -> don't do squat
			} else {
				// Move to the letter below this one
				Point p = TextPositionToPoint (new Position (linePos, textPos));
				p.Y += sizes[0].Height;

				Position np = PointToTextPosition (p);
				linePos = np.Line;
				textPos = np.Offset;
			}
		}
		#endregion

		#region String Measuring Functions
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
			// Grab any Cairo surface we can make a context from
			Cairo.ImageSurface surf = PintaCore.Layers.ToolLayer.Surface;
			Cairo.TextExtents te;

			using (Cairo.Context g = new Cairo.Context (surf))
				te = TextExtents (g, s);

			return new Size ((int)te.Width, (int)te.Height);
		}

		private Point TextPositionToPoint (Position p)
		{
			Point pf = Point.Zero;

			Size sz = StringSize (lines[p.Line].Substring (0, p.Offset));
			Size fullSz = StringSize (lines[p.Line]);

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
			}

			return pf;
		}

		private Position PointToTextPosition (Point pf)
		{
			int dx = pf.X - clickPoint.X;
			int dy = pf.Y - clickPoint.Y;
			int line = (int)Math.Floor (dy / (float)sizes[0].Height);

			if (line < 0)
				line = 0;
			else if (line >= lines.Count)
				line = lines.Count - 1;

			int offset = FindOffsetPosition (dx, lines[line], line);
			Position p = new Position (line, offset);

			if (p.Offset >= lines[p.Line].Length)
				p.Offset = lines[p.Line].Length;

			return p;
		}

		private int FindOffsetPosition (float offset, string line, int lno)
		{
			for (int i = 0; i < line.Length; i++) {
				Point pf = TextPositionToPoint (new Position (lno, i));
				float dx = pf.X - clickPoint.X;

				if (dx >= offset)
					return i;
			}

			return line.Length;
		}

		private void InsertCharIntoString (uint c)
		{
			byte[] bytes = { (byte)c, (byte)(c >> 8), (byte)(c >> 16), (byte)(c >> 24) };
			string unicodeChar = System.Text.Encoding.UTF32.GetString (bytes);

			lines[linePos] = lines[linePos].Insert (textPos, unicodeChar);
			sizes = null;
		}
		#endregion

		#region Start/Stop Editing
		private void StopEditing ()
		{
			// If we don't have an open document, these will crash
			if (PintaCore.Workspace.HasOpenDocuments) {
				PintaCore.Layers.ToolLayer.Clear ();
				PintaCore.Layers.ToolLayer.Hidden = true;
			}

			mode = EditingMode.NotEditing;
			lines = null;
		}

		private void StartEditing ()
		{
			linePos = 0;
			textPos = 0;
			lines = new List<string> ();
			sizes = null;
			lines.Add (string.Empty);
			mode = EditingMode.EmptyEdit;

			PintaCore.Layers.ToolLayer.Hidden = false;
		}
		#endregion

		#region Text Drawing Methods
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
				if (ignoreRedraw > 0)
					return;

				if (saved != null) {
					saved.Draw (surf);
					PintaCore.Workspace.Invalidate (saved.Region.Clipbox);
					saved.Dispose ();
					saved = null;
				}

				// Save the Space behind the lines
				Rectangle[] rects = new Rectangle[lines.Count + 1];
				Point[] localUls = new Point[lines.Count];

				// We need to measure each line
				if (sizes == null) {
					sizes = new Size[lines.Count + 1];

					for (int i = 0; i < lines.Count; ++i)
						sizes[i] = StringSize (lines[i]);
				}

				// Generate boundary rectangles for each line
				for (int i = 0; i < lines.Count; ++i) {
					Point upperLeft = GetUpperLeft (sizes[i], i);
					Rectangle rect = new Rectangle (upperLeft, sizes[i]);

					localUls[i] = upperLeft;
					rects[i] = rect;
				}

				// The Cursor Line
				string cursorLine = lines[linePos].Substring (0, textPos);
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
				} else if (cursorLine.Length == lines[linePos].Length) {
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
				uls = localUls;

				for (int i = 0; i < lines.Count; i++)
					RenderText (surf, i);

				// Draw the Cursor
				if (cursorOn) {
					using (Cairo.Context toolctx = new Cairo.Context (PintaCore.Layers.ToolLayer.Surface)) {
						if (emptyCursorLineFlag)
							toolctx.FillRectangle (cursorRect.ToCairoRectangle (), PintaCore.Palette.PrimaryColor);
						else
							toolctx.DrawLine (new Cairo.PointD (cursorRect.Right, cursorRect.Top), new Cairo.PointD (cursorRect.Right, cursorRect.Bottom), PintaCore.Palette.PrimaryColor, 1);
					}
				}

				PintaCore.Workspace.Invalidate (saved.Region.Clipbox);
			}
		}

		private void RenderText (Cairo.ImageSurface surf, int lineNumber)
		{
			DrawText (surf, Font, lines[lineNumber], uls[lineNumber], sizes[lineNumber], UseAlphaBlending, PintaCore.Palette.PrimaryColor);
		}

		private void DrawText (Cairo.ImageSurface dst, string textFont, string text, Point pt, Size measuredSize, bool antiAliasing, Cairo.Color color)
		{
			Rectangle dstRect = new Rectangle (pt, measuredSize);

			using (Cairo.ImageSurface surface = new Cairo.ImageSurface (Cairo.Format.Argb32, 8, 8)) {
				using (Cairo.Context context = new Cairo.Context (surface))
					context.FillRectangle (new Cairo.Rectangle (0, 0, surface.Width, surface.Height), color);

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

			if (dstRectClipped.Width == 0 || dstRectClipped.Height == 0)
				return;

			// We only use the first 8,8 of brush
			using (Cairo.Context toolctx = new Cairo.Context (PintaCore.Layers.ToolLayer.Surface)) {
				toolctx.FillRectangle (dstRect.ToCairoRectangle (), new Cairo.Color (1, 1, 1));
				
				// TODO find how create a surface a of a particular area of a bigger surface!
				// for moment work with the whole surface!
				if (measuredSize.Width > 0 && measuredSize.Height > 0) {
					using (Cairo.Context ctx = new Cairo.Context (PintaCore.Layers.ToolLayer.Surface)) {
						Cairo.TextExtents te = TextExtents (ctx, text);

						ctx.DrawText (new Cairo.PointD (dstRect.X + offset - te.XBearing, dstRect.Y - te.YBearing), textFont, FontSlant, FontWeight, FontSize, PintaCore.Palette.PrimaryColor, text, antiAliasing);

						if (underscore_btn.Active) {
							int lineSize = 1;
							Cairo.FontExtents fe = FontExtents (ctx, text);
							ctx.DrawLine (new Cairo.PointD (pt2.X, dstRect.Bottom + fe.Descent), new Cairo.PointD (dstRect.Right - offset, dstRect.Bottom + fe.Descent), PintaCore.Palette.PrimaryColor, lineSize);
						}
					}

					PintaCore.Workspace.Invalidate ();
				}

				// Mask out anything that isn't within the user's clip region (selected region)
				using (Region clip = Region.Rectangle (PintaCore.Layers.SelectionPath.GetBounds ())) {
					clip.Xor (Region.Rectangle (dstRectClipped));

					// Invert
					clip.Intersect (Region.Rectangle (new Rectangle (pt, measuredSize)));

					toolctx.FillRegion (clip, new Cairo.Color (1, 1, 1, 1));
				}

				int skipX;

				if (pt.X < 0)
					skipX = -pt.X;
				else
					skipX = 0;

				int xEnd = Math.Min (dst.Width, pt.X + measuredSize.Width);

				bool blending = true;
				dst.Flush ();

				for (int y = pt.Y; y < pt.Y + measuredSize.Height; ++y) {
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

				dst.MarkDirty ();
			}
		}
		#endregion

		#region History Methods
		private void SaveHistoryMemento ()
		{
			RedrawText (false);
			
			if (saved != null) {
				Region hitTest = Region.Rectangle (PintaCore.Layers.SelectionPath.GetBounds ());
				hitTest.Intersect (saved.Region);
				
				if (hitTest.Clipbox.Width != 0 && hitTest.Clipbox.Height != 0) {
					ClippedSurfaceHistoryItem bha = new ClippedSurfaceHistoryItem (Icon, Name, saved, PintaCore.Layers.CurrentLayerIndex);
					
					if (currentHA == null)
						PintaCore.History.PushNewItem (bha);
					else {
						currentHA.Push (bha);
						currentHA = null;
					}
				}
				
				hitTest.Dispose ();
				saved.Dispose ();
				saved = null;
			}
		}
		#endregion
	}
}
