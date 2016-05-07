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
using Gtk;
using Mono.Unix;
using Pinta.Core;

namespace Pinta.Tools
{
	public class TextTool : BaseTool
	{
		// Variables for dragging
		private Cairo.PointD startMouseXY;
		private Point startClickPoint;
		private bool tracking;
		private Gdk.Cursor cursor_hand;

		private Point clickPoint;
		private bool is_editing;
		private Rectangle old_cursor_bounds = Rectangle.Zero;

		//This is used to temporarily store the UserLayer's and TextLayer's previous ImageSurface states.
		private Cairo.ImageSurface text_undo_surface;
		private Cairo.ImageSurface user_undo_surface;
		private TextEngine undo_engine;
		// The selection from when editing started. This ensures that text doesn't suddenly disappear/appear
		// if the selection changes before the text is finalized.
		private DocumentSelection selection;

		private Gtk.IMMulticontext imContext;
		private TextLayout layout;

		private Rectangle CurrentTextBounds
		{
			get
			{
				return PintaCore.Workspace.ActiveDocument.CurrentUserLayer.textBounds;
			}

			set
			{
				PintaCore.Workspace.ActiveDocument.CurrentUserLayer.previousTextBounds = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.textBounds;

				PintaCore.Workspace.ActiveDocument.CurrentUserLayer.textBounds = value;
			}
		}

		private TextEngine CurrentTextEngine
		{
			get
			{
				return PintaCore.Workspace.HasOpenDocuments ?
					PintaCore.Workspace.ActiveDocument.CurrentUserLayer.tEngine : null;
			}

			set
			{
				PintaCore.Workspace.ActiveDocument.CurrentUserLayer.tEngine = value;
			}
		}

		private TextLayout CurrentTextLayout
		{
			get {
				if (layout.Engine != CurrentTextEngine)
					layout.Engine = CurrentTextEngine;
				return layout;
			}
		}

		//While this is true, text will not be finalized upon Surface.Clone calls.
		private bool ignoreCloneFinalizations = false;

		//Whether or not either (or both) of the Ctrl keys are pressed.
		private bool ctrlKey = false;

		//Store the most recent mouse position.
		private Point lastMousePosition = new Point(0, 0);

		//Whether or not the previous TextTool mouse cursor shown was the normal one.
		private bool previousMouseCursorNormal = true;

		public override string Name { get { return Catalog.GetString ("Text"); } }
		private string FinalizeName { get { return Catalog.GetString("Text - Finalize"); } }
		public override string Icon { get { return "Tools.Text.png"; } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.T; } }
		public override int Priority { get { return 37; } }

		public override string StatusBarText {
			get { return Catalog.GetString ("Left click to place cursor, then type desired text. Text color is primary color."); }
		}

		public override Gdk.Cursor DefaultCursor {
			get {
				return new Gdk.Cursor (Gdk.Display.Default,
						PintaCore.Resources.GetIcon ("Cursor.Text.png"),
						16, 16);
			}
		}
		public Gdk.Cursor InvalidEditCursor { get { return new Gdk.Cursor (Gdk.Display.Default, PintaCore.Resources.GetIcon ("Menu.Edit.EraseSelection.png"), 8, 0); } }

#region Constructor
		public TextTool ()
		{
			cursor_hand = new Gdk.Cursor (Gdk.Display.Default, PintaCore.Resources.GetIcon ("Cursor.Pan.png"), 8, 8);
			imContext = new Gtk.IMMulticontext ();
			imContext.Commit += OnIMCommit;
			layout = new TextLayout ();
		}

		static TextTool ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("ShapeTool.Outline.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ShapeTool.Outline.png")));
			fact.Add ("ShapeTool.Fill.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ShapeTool.Fill.png")));
			fact.Add ("ShapeTool.OutlineFill.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ShapeTool.OutlineFill.png")));
			fact.Add ("TextTool.FillBackground.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("TextTool.FillBackground.png")));
			fact.AddDefault ();
		}
#endregion

#region ToolBar
		private ToolBarLabel font_label;
		private ToolBarFontComboBox font_combo;
		private ToolBarComboBox size_combo;
		private ToolBarToggleButton bold_btn;
		private ToolBarToggleButton italic_btn;
		private ToolBarToggleButton underscore_btn;
		private ToolBarToggleButton left_alignment_btn;
		private ToolBarToggleButton center_alignment_btn;
		private ToolBarToggleButton Right_alignment_btn;
		private ToolBarLabel spacer_label;
		private ToolBarLabel fill_label;
		private ToolBarDropDownButton fill_button;
		private SeparatorToolItem fill_sep;
		private SeparatorToolItem outline_sep;
		private ToolBarComboBox outline_width;
		private ToolBarLabel outline_width_label;
		private ToolBarButton outline_width_minus;
		private ToolBarButton outline_width_plus;

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

				font_combo = new ToolBarFontComboBox (150, index, fonts.ToArray ());
				font_combo.ComboBox.Changed += HandleFontChanged;
			}

			tb.AppendItem (font_combo);

			if (spacer_label == null)
				spacer_label = new ToolBarLabel (" ");

			tb.AppendItem (spacer_label);

			if (size_combo == null) {
				size_combo = new ToolBarComboBox (65, 0, true);

				size_combo.ComboBox.Changed += HandleSizeChanged;
			}

			tb.AppendItem (size_combo);

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

			if (fill_sep == null)
				fill_sep = new Gtk.SeparatorToolItem ();

			tb.AppendItem (fill_sep);

			if (fill_label == null)
				fill_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Text Style")));

			tb.AppendItem (fill_label);

			if (fill_button == null) {
				fill_button = new ToolBarDropDownButton ();

				fill_button.AddItem (Catalog.GetString ("Normal"), "ShapeTool.Fill.png", 0);
				fill_button.AddItem (Catalog.GetString ("Normal and Outline"), "ShapeTool.OutlineFill.png", 1);
				fill_button.AddItem (Catalog.GetString ("Outline"), "ShapeTool.Outline.png", 2);
				fill_button.AddItem (Catalog.GetString ("Fill Background"), "TextTool.FillBackground.png", 3);

				fill_button.SelectedItemChanged += HandleBoldButtonToggled;
			}

			tb.AppendItem (fill_button);

			if (outline_sep == null)
				outline_sep = new SeparatorToolItem ();

			tb.AppendItem (outline_sep);

			if (outline_width_label == null)
				outline_width_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Outline width")));

			tb.AppendItem (outline_width_label);

			if (outline_width_minus == null) {
				outline_width_minus = new ToolBarButton ("Toolbar.MinusButton.png", "", Catalog.GetString ("Decrease outline size"));
				outline_width_minus.Clicked += MinusButtonClickedEvent;
			}

			tb.AppendItem (outline_width_minus);

			if (outline_width == null) {
				outline_width = new ToolBarComboBox (65, 1, true, "1", "2", "3", "4", "5", "6", "7", "8", "9",
						"10", "11", "12", "13", "14", "15", "20", "25", "30", "35",
						"40", "45", "50", "55");

				(outline_width.Child as ComboBoxEntry).Changed += HandleSizeChanged;
			}

			tb.AppendItem (outline_width);

			if (outline_width_plus == null) {
				outline_width_plus = new ToolBarButton ("Toolbar.PlusButton.png", "", Catalog.GetString ("Increase outline size"));
				outline_width_plus.Clicked += PlusButtonClickedEvent;
			}

			tb.AppendItem (outline_width_plus);

			outline_width_plus.Visible = outline_width_minus.Visible = outline_width.Visible
				= outline_width_label.Visible = outline_sep.Visible = StrokeText;

			UpdateFontSizes ();

			if (PintaCore.Workspace.HasOpenDocuments) {
				//Make sure the event handler is never added twice.
				PintaCore.Workspace.ActiveDocument.LayerCloned -= FinalizeText;

				//When an ImageSurface is Cloned, finalize the re-editable text (if applicable).
				PintaCore.Workspace.ActiveDocument.LayerCloned += FinalizeText;
			}
		}

		private void HandleFontChanged (object sender, EventArgs e)
		{
			if (PintaCore.Workspace.HasOpenDocuments)
				PintaCore.Workspace.ActiveDocument.Workspace.Canvas.GrabFocus ();

			UpdateFontSizes ();
			UpdateFont ();
		}

		private void UpdateFontSizes ()
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

		private void HandleSizeChanged (object sender, EventArgs e)
		{
			string text = size_combo.ComboBox.ActiveText;
			if (int.TryParse (text, out FontSize))
				UpdateFont ();
		}

		private Pango.FontFamily FontFamily {
			get { return PintaCore.System.Fonts.GetFamily (font_combo.ComboBox.ActiveText); }
		}

		private int FontSize;

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

		private string Font {
			get { return font_combo.ComboBox.ActiveText; }
		}

		private void HandlePintaCorePalettePrimaryColorChanged (object sender, EventArgs e)
		{
			if (is_editing)
				RedrawText (true, true);
		}

		private void HandleLeftAlignmentButtonToggled (object sender, EventArgs e)
		{
			if (left_alignment_btn.Active) {
				Right_alignment_btn.Active = false;
				center_alignment_btn.Active = false;
			} else if (!Right_alignment_btn.Active && !center_alignment_btn.Active) {
				left_alignment_btn.Active = true;
			}

			UpdateFont ();
		}

		private void HandleCenterAlignmentButtonToggled (object sender, EventArgs e)
		{
			if (center_alignment_btn.Active) {
				Right_alignment_btn.Active = false;
				left_alignment_btn.Active = false;
			} else if (!Right_alignment_btn.Active && !left_alignment_btn.Active) {
				center_alignment_btn.Active = true;
			}

			UpdateFont ();
		}

		private void HandleRightAlignmentButtonToggled (object sender, EventArgs e)
		{
			if (Right_alignment_btn.Active) {
				center_alignment_btn.Active = false;
				left_alignment_btn.Active = false;
			} else if (!center_alignment_btn.Active && !left_alignment_btn.Active) {
				Right_alignment_btn.Active = true;
			}

			UpdateFont ();
		}

		private void HandleUnderscoreButtonToggled (object sender, EventArgs e)
		{
			UpdateFont ();
		}

		private void HandleItalicButtonToggled (object sender, EventArgs e)
		{
			UpdateFont ();
		}

		private void HandleBoldButtonToggled (object sender, EventArgs e)
		{
			outline_width_plus.Visible = outline_width_minus.Visible = outline_width.Visible
				= outline_width_label.Visible = outline_sep.Visible = StrokeText;

			UpdateFont ();
		}

		private void HandleSelectedLayerChanged(object sender, EventArgs e)
		{
			UpdateFont();
		}

		private void UpdateFont ()
		{
			if (CurrentTextEngine != null) {
				CurrentTextEngine.Alignment = Alignment;
				CurrentTextEngine.SetFont (Font, FontSize, bold_btn.Active, italic_btn.Active, underscore_btn.Active);
			}

			if (is_editing)
				RedrawText (true, true);
		}

		protected virtual void MinusButtonClickedEvent (object o, EventArgs args)
		{
			if (OutlineWidth > 1)
				OutlineWidth--;
		}

		protected virtual void PlusButtonClickedEvent (object o, EventArgs args)
		{
			OutlineWidth++;
		}

		protected int OutlineWidth {
			get {
				int width;
				if (Int32.TryParse (outline_width.ComboBox.ActiveText, out width)) {
					if (width > 0) {
						(outline_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = width.ToString ();
						return width;
					}
				}
				(outline_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = "2";
				return 2;
			}
			set { (outline_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = value.ToString (); }
		}

		protected bool StrokeText { get { return ((int)fill_button.SelectedItem.Tag >= 1 && (int)fill_button.SelectedItem.Tag != 3); } }
		protected bool FillText { get { return (int)fill_button.SelectedItem.Tag <= 1 || (int)fill_button.SelectedItem.Tag == 3; } }
		protected bool BackgroundFill { get { return (int)fill_button.SelectedItem.Tag == 3; } }
#endregion

#region Activation/Deactivation
		protected override void OnActivated ()
		{
			base.OnActivated ();

			// We may need to redraw our text when the color changes
			PintaCore.Palette.PrimaryColorChanged += HandlePintaCorePalettePrimaryColorChanged;
			PintaCore.Palette.SecondaryColorChanged += HandlePintaCorePalettePrimaryColorChanged;

			PintaCore.Layers.LayerAdded += HandleSelectedLayerChanged;
			PintaCore.Layers.LayerRemoved += HandleSelectedLayerChanged;
			PintaCore.Layers.SelectedLayerChanged += HandleSelectedLayerChanged;

			// We always start off not in edit mode
			is_editing = false;
		}

		protected override void OnCommit ()
		{
			StopEditing(false);
		}

		protected override void OnDeactivated(BaseTool newTool)
		{
			base.OnDeactivated (newTool);

			// Stop listening for color change events
			PintaCore.Palette.PrimaryColorChanged -= HandlePintaCorePalettePrimaryColorChanged;
			PintaCore.Palette.SecondaryColorChanged -= HandlePintaCorePalettePrimaryColorChanged;

			PintaCore.Layers.LayerAdded -= HandleSelectedLayerChanged;
			PintaCore.Layers.LayerRemoved -= HandleSelectedLayerChanged;
			PintaCore.Layers.SelectedLayerChanged -= HandleSelectedLayerChanged;

			StopEditing(false);
		}
#endregion

#region Mouse Handlers
		protected override void OnMouseDown(DrawingArea canvas, ButtonPressEventArgs args, Cairo.PointD point)
		{
			ctrlKey = (args.Event.State & ModifierType.ControlMask) != 0;

			//Store the mouse position.
			Point pt = point.ToGdkPoint();

			// Grab focus so we can get keystrokes
			canvas.GrabFocus ();

			if (selection != null)
				selection.Dispose ();
			selection = PintaCore.Workspace.ActiveDocument.Selection.Clone ();

			// A right click allows you to move the text around
			if (args.Event.Button == 3)
			{
				//The user is dragging text with the right mouse button held down, so track the mouse as it moves.
				tracking = true;

				//Remember the position of the mouse before the text is dragged.
				startMouseXY = point;
				startClickPoint = clickPoint;

				//Change the cursor to indicate that the text is being dragged.
				SetCursor(cursor_hand);

				return;
			}

			// The user clicked the left mouse button			
			if (args.Event.Button == 1)
			{
				// If the user is [editing or holding down Ctrl] and clicked
				//within the text, move the cursor to the click location
				if ((is_editing || ctrlKey) && CurrentTextBounds.ContainsCorrect(pt))
				{
					StartEditing();

					//Change the position of the cursor to where the mouse clicked.
					TextPosition p = CurrentTextLayout.PointToTextPosition(pt);
					CurrentTextEngine.SetCursorPosition(p, true);

					//Redraw the text with the new cursor position.
					RedrawText(true, true);

					return;
				}

				// We're already editing and the user clicked outside the text,
				// commit the user's work, and start a new edit
				switch (CurrentTextEngine.State)
				{
					// We were editing, save and stop
					case TextMode.Uncommitted:
						StopEditing(true);
						break;

						// We were editing, but nothing had been
						// keyed. Stop editing.
					case TextMode.Unchanged:
						StopEditing(false);
						break;
				}

				if (ctrlKey)
				{
					//Go through every UserLayer.
					foreach (UserLayer ul in PintaCore.Workspace.ActiveDocument.UserLayers)
					{
						//Check each UserLayer's editable text boundaries to see if they contain the mouse position.
						if (ul.textBounds.ContainsCorrect(pt))
						{
							//The mouse clicked on editable text.

							//Change the current UserLayer to the Layer that contains the text that was clicked on.
							PintaCore.Workspace.ActiveDocument.SetCurrentUserLayer(ul);

							//The user is editing text now.
							is_editing = true;

							//Set the cursor in the editable text where the mouse was clicked.
							TextPosition p = CurrentTextLayout.PointToTextPosition(pt);
							CurrentTextEngine.SetCursorPosition(p, true);

							//Redraw the editable text with the cursor.
							RedrawText(true, true);

							//Don't check any more UserLayers - stop at the first UserLayer that has editable text containing the mouse position.
							return;
						}
					}
				}
				else
				{
					if (CurrentTextEngine.State == TextMode.NotFinalized)
					{
						//The user is making a new text and the old text hasn't been finalized yet.
						FinalizeText();
					}

					if (!is_editing)
					{
						// Start editing at the cursor location
						clickPoint = pt;
						CurrentTextEngine.Clear();
						clickPoint.Offset (0, -CurrentTextLayout.FontHeight/2);
						CurrentTextEngine.Origin = clickPoint;
						StartEditing();
						RedrawText(true, true);
					}
				}
			}
		}

		protected override void OnMouseMove (object o, MotionNotifyEventArgs args, Cairo.PointD point)
		{
			ctrlKey = (args.Event.State & ModifierType.ControlMask) != 0;

			lastMousePosition = point.ToGdkPoint();

			// If we're dragging the text around, do that
			if (tracking)
			{
				Cairo.PointD delta = new Cairo.PointD(point.X - startMouseXY.X, point.Y - startMouseXY.Y);

				clickPoint = new Point((int)(startClickPoint.X + delta.X), (int)(startClickPoint.Y + delta.Y));
				CurrentTextEngine.Origin = clickPoint;

				RedrawText(true, true);
			}
			else
			{
				UpdateMouseCursor();
			}
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			// If we were dragging the text around, finish that up
			if (tracking) {
				Cairo.PointD delta = new Cairo.PointD (point.X - startMouseXY.X, point.Y - startMouseXY.Y);

				clickPoint = new Point ((int)(startClickPoint.X + delta.X), (int)(startClickPoint.Y + delta.Y));
				CurrentTextEngine.Origin = clickPoint;

				RedrawText (false, true);
				tracking = false;
				SetCursor (null);
			}
		}

		private void UpdateMouseCursor()
		{
			//Whether or not to show the normal text cursor.
			bool showNormalCursor = false;

			if (ctrlKey && PintaCore.Workspace.HasOpenDocuments)
			{
				//Go through every UserLayer.
				foreach (UserLayer ul in PintaCore.Workspace.ActiveDocument.UserLayers)
				{
					//Check each UserLayer's editable text boundaries to see if they contain the mouse position.
					if (ul.textBounds.ContainsCorrect(lastMousePosition))
					{
						//The mouse is over editable text.
						showNormalCursor = true;
					}

				}
			}
			else
			{
				showNormalCursor = true;
			}

			if (showNormalCursor)
			{
				if (!previousMouseCursorNormal)
				{
					SetCursor(DefaultCursor);

					previousMouseCursorNormal = showNormalCursor;

					if (PintaCore.Workspace.HasOpenDocuments)
						RedrawText(is_editing, true);
				}
			}
			else
			{
				if (previousMouseCursorNormal)
				{
					SetCursor(InvalidEditCursor);

					previousMouseCursorNormal = showNormalCursor;

					RedrawText(is_editing, true);
				}
			}
		}
#endregion

#region Keyboard Handlers
		protected override void OnKeyDown (DrawingArea canvas, KeyPressEventArgs args)
		{
			if (!PintaCore.Workspace.HasOpenDocuments) {
				args.RetVal = false;
				return;
			}

			Gdk.ModifierType modifier = args.Event.State;

			// If we are dragging the text, we
			// aren't going to handle key presses
			if (tracking)
				return;

			// Ignore anything with Alt pressed
			if ((modifier & Gdk.ModifierType.Mod1Mask) != 0)
				return;

			ctrlKey = (args.Event.Key == Gdk.Key.Control_L || args.Event.Key == Gdk.Key.Control_R);
			UpdateMouseCursor ();

			// Assume that we are going to handle the key
			bool keyHandled = true;

			if (is_editing)
			{
				switch (args.Event.Key)
				{
					case Gdk.Key.BackSpace:
						CurrentTextEngine.PerformBackspace();
						break;

					case Gdk.Key.Delete:
						CurrentTextEngine.PerformDelete();
						break;

					case Gdk.Key.KP_Enter:
					case Gdk.Key.Return:
						CurrentTextEngine.PerformEnter();
						break;

					case Gdk.Key.Left:
						CurrentTextEngine.PerformLeft((modifier & Gdk.ModifierType.ControlMask) != 0, (modifier & Gdk.ModifierType.ShiftMask) != 0);
						break;

					case Gdk.Key.Right:
						CurrentTextEngine.PerformRight((modifier & Gdk.ModifierType.ControlMask) != 0, (modifier & Gdk.ModifierType.ShiftMask) != 0);
						break;

					case Gdk.Key.Up:
						CurrentTextEngine.PerformUp((modifier & Gdk.ModifierType.ShiftMask) != 0);
						break;

					case Gdk.Key.Down:
						CurrentTextEngine.PerformDown((modifier & Gdk.ModifierType.ShiftMask) != 0);
						break;

					case Gdk.Key.Home:
						CurrentTextEngine.PerformHome((modifier & Gdk.ModifierType.ControlMask) != 0, (modifier & Gdk.ModifierType.ShiftMask) != 0);
						break;

					case Gdk.Key.End:
						CurrentTextEngine.PerformEnd((modifier & Gdk.ModifierType.ControlMask) != 0, (modifier & Gdk.ModifierType.ShiftMask) != 0);
						break;

					case Gdk.Key.Next:
					case Gdk.Key.Prior:
						break;

					case Gdk.Key.Escape:
						StopEditing(false);
						return;
					case Gdk.Key.Insert:
						if (modifier.IsShiftPressed ())
						{
							Gtk.Clipboard cb = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
							CurrentTextEngine.PerformPaste(cb);
						}
						else if (modifier.IsControlPressed ())
						{
							Gtk.Clipboard cb = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
							CurrentTextEngine.PerformCopy(cb);
						}
						break;
					default:
						if (modifier.IsControlPressed ())
						{
							if (args.Event.Key == Gdk.Key.z)
							{
								//Ctrl + Z for undo while editing.
								TryHandleUndo();

								if (PintaCore.Workspace.ActiveDocument.History.CanUndo)
									PintaCore.Workspace.ActiveDocument.History.Undo();

								return;
							}
							else if (args.Event.Key == Gdk.Key.i)
							{
								italic_btn.Toggle ();
								UpdateFont ();
							}
							else if (args.Event.Key == Gdk.Key.b)
							{
								bold_btn.Toggle ();
								UpdateFont ();
							}
							else if (args.Event.Key == Gdk.Key.u)
							{
								underscore_btn.Toggle ();
								UpdateFont ();
							}
							else if (args.Event.Key == Gdk.Key.a)
							{
								// Select all of the text.
								CurrentTextEngine.PerformHome (false, false);
								CurrentTextEngine.PerformEnd (true, true);
							}
							else
							{
								//Ignore command shortcut.
								return;
							}
						}
						else
						{
							keyHandled = TryHandleChar(args.Event);
						}

						break;
				}

				// If we processed a key, update the display
				if (keyHandled)
				{
					RedrawText(true, true);
				}
			}
			else
			{
				// If we're not editing, allow the key press to be handled elsewhere (e.g. for selecting another tool).
				keyHandled = false;
			}

			args.RetVal = keyHandled;
		}

		protected override void OnKeyUp(DrawingArea canvas, KeyReleaseEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Control_L || args.Event.Key == Gdk.Key.Control_R ||
					(args.Event.State & ModifierType.ControlMask) != 0)
			{
				ctrlKey = false;

				UpdateMouseCursor();
			}
		}

		private bool TryHandleChar(EventKey eventKey)
		{
			// Try to handle it as a character
			if (imContext.FilterKeypress (eventKey)) {
				return true;
			}

			// We didn't handle the key
			return false;
		}

		private void OnIMCommit (object o, CommitArgs args)
		{
			try {
				var str = new StringBuilder ();

				for (int i = 0; i < args.Str.Length; i++) {
					char utf32Char;
					if (char.IsHighSurrogate (args.Str, i)) {
						utf32Char = (char)char.ConvertToUtf32 (args.Str, i);
						i++;
					} else {
						utf32Char = args.Str[i];
					}

					str.Append (utf32Char.ToString ());
				}

				CurrentTextEngine.InsertText (str.ToString ());
			} finally {
				imContext.Reset ();
			}
		}
#endregion

#region Start/Stop Editing
		private void StartEditing ()
		{
			is_editing = true;

            imContext.ClientWindow = PintaCore.Workspace.ActiveWorkspace.Canvas.GdkWindow;

			if (selection == null)
				selection = PintaCore.Workspace.ActiveDocument.Selection.Clone ();

			//Start ignoring any Surface.Clone calls from this point on (so that it doesn't start to loop).
			ignoreCloneFinalizations = true;

			//Store the previous state of the current UserLayer's and TextLayer's ImageSurfaces.
			user_undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();
			text_undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.TextLayer.Layer.Surface.Clone();

			//Store the previous state of the Text Engine.
			undo_engine = CurrentTextEngine.Clone();

			//Stop ignoring any Surface.Clone calls from this point on.
			ignoreCloneFinalizations = false;
		}

		private void StopEditing(bool finalize)
		{
            imContext.ClientWindow = null;

			if (!PintaCore.Workspace.HasOpenDocuments)
				return;

			if (!is_editing)
				return;

			is_editing = false;

			//Make sure that neither undo surface is null, the user is editing, and there are uncommitted changes.
			if (text_undo_surface != null && user_undo_surface != null && CurrentTextEngine.State == TextMode.Uncommitted)
			{
				Document doc = PintaCore.Workspace.ActiveDocument;

				RedrawText(false, true);

				//Start ignoring any Surface.Clone calls from this point on (so that it doesn't start to loop).
				ignoreCloneFinalizations = true;

				//Create a new TextHistoryItem so that the committing of text can be undone.
				doc.History.PushNewItem(new TextHistoryItem(Icon, Name,
							text_undo_surface.Clone(), user_undo_surface.Clone(),
							undo_engine.Clone(), doc.CurrentUserLayer));

				//Stop ignoring any Surface.Clone calls from this point on.
				ignoreCloneFinalizations = false;

				//Now that the text has been committed, change its state.
				CurrentTextEngine.State = TextMode.NotFinalized;
			}

			RedrawText(false, true);

			if (finalize)
			{
				FinalizeText();
			}
		}
#endregion

#region Text Drawing Methods
		/// <summary>
		/// Clears the entire TextLayer and redraw the previous text boundary.
		/// </summary>
		private void ClearTextLayer()
		{
			//Clear the TextLayer.
			PintaCore.Workspace.ActiveDocument.CurrentUserLayer.TextLayer.Layer.Surface.Clear();

			//Redraw the previous text boundary.
			InflateAndInvalidate(PintaCore.Workspace.ActiveDocument.CurrentUserLayer.previousTextBounds);
		}

		/// <summary>
		/// Draws the text.
		/// </summary>
		/// <param name="showCursor">Whether or not to show the mouse cursor in the drawing.</param>
		/// <param name="useTextLayer">Whether or not to use the TextLayer (as opposed to the Userlayer).</param>
		private void RedrawText (bool showCursor, bool useTextLayer)
		{
			Rectangle r = CurrentTextLayout.GetLayoutBounds();
			r.Inflate(10 + OutlineWidth, 10 + OutlineWidth);
			InflateAndInvalidate(r);
			CurrentTextBounds = r;

			Rectangle cursorBounds = Rectangle.Zero;

			Cairo.ImageSurface surf;

			if (!useTextLayer)
			{
				//Draw text on the current UserLayer's surface as finalized text.
				surf = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface;
			}
			else
			{
				//Draw text on the current UserLayer's TextLayer's surface as re-editable text.
				surf = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.TextLayer.Layer.Surface;

				ClearTextLayer();
			}

			using (var g = new Cairo.Context (surf)) {
				g.Save ();

				// Show selection if on text layer
				if (useTextLayer) {
					// Selected Text
					Cairo.Color c = new Cairo.Color (0.7, 0.8, 0.9, 0.5);
					foreach (Rectangle rect in CurrentTextLayout.SelectionRectangles)
						g.FillRectangle (rect.ToCairoRectangle (), c);
				}

				if (selection != null) {
					g.AppendPath (selection.SelectionPath);
					g.FillRule = Cairo.FillRule.EvenOdd;
					g.Clip ();
				}

				g.MoveTo (new Cairo.PointD (CurrentTextEngine.Origin.X, CurrentTextEngine.Origin.Y));

				g.SetSourceColor (PintaCore.Palette.PrimaryColor);

				//Fill in background
				if (BackgroundFill) {
					using (var g2 = new Cairo.Context (surf)) {
						g2.FillRectangle(CurrentTextLayout.GetLayoutBounds().ToCairoRectangle(), PintaCore.Palette.SecondaryColor);
					}
				}

				// Draw the text
				if (FillText)
					Pango.CairoHelper.ShowLayout (g, CurrentTextLayout.Layout);

				if (FillText && StrokeText) {
					g.SetSourceColor (PintaCore.Palette.SecondaryColor);
					g.LineWidth = OutlineWidth;

					Pango.CairoHelper.LayoutPath (g, CurrentTextLayout.Layout);
					g.Stroke ();
				} else if (StrokeText) {
					g.SetSourceColor (PintaCore.Palette.PrimaryColor);
					g.LineWidth = OutlineWidth;

					Pango.CairoHelper.LayoutPath (g, CurrentTextLayout.Layout);
					g.Stroke ();
				}

				if (showCursor) {
					var loc = CurrentTextLayout.GetCursorLocation ();
					var color = PintaCore.Palette.PrimaryColor;

					g.Antialias = Cairo.Antialias.None;
					g.DrawLine (new Cairo.PointD (loc.X, loc.Y),
							new Cairo.PointD (loc.X, loc.Y + loc.Height),
							color, 1);

					cursorBounds = Rectangle.Inflate (loc, 2, 10);
				}

				g.Restore ();


				if (useTextLayer && (is_editing || ctrlKey) && !CurrentTextEngine.IsEmpty())
				{
					//Draw the text edit rectangle.

					g.Save();

					g.Translate(.5, .5);

					using (Cairo.Path p = g.CreateRectanglePath(CurrentTextBounds.ToCairoRectangle ()))
					{
						g.AppendPath(p);
					}

					g.LineWidth = 1;

					g.SetSourceColor (new Cairo.Color (1, 1, 1));
					g.StrokePreserve();

					g.SetDash(new double[] { 2, 4 }, 0);
					g.SetSourceColor (new Cairo.Color(1, .1, .2));

					g.Stroke();

					g.Restore();
				}
			}

			InflateAndInvalidate(PintaCore.Workspace.ActiveDocument.CurrentUserLayer.previousTextBounds);
			PintaCore.Workspace.Invalidate(old_cursor_bounds);
			InflateAndInvalidate (r);
			PintaCore.Workspace.Invalidate(cursorBounds);

			old_cursor_bounds = cursorBounds;
		}

		/// <summary>
		/// Finalize re-editable text (if applicable).
		/// </summary>
		public void FinalizeText()
		{
			//If this is true, don't finalize any text - this is used to prevent the code from looping recursively.
			if (!ignoreCloneFinalizations)
			{
				//Only bother finalizing text if editing.
				if (CurrentTextEngine.State != TextMode.Unchanged)
				{
					//Start ignoring any Surface.Clone calls from this point on (so that it doesn't start to loop).
					ignoreCloneFinalizations = true;
					Document doc = PintaCore.Workspace.ActiveDocument;

					//Create a backup of everything before redrawing the text and etc.
					Cairo.ImageSurface oldTextSurface = doc.CurrentUserLayer.TextLayer.Layer.Surface.Clone();
					Cairo.ImageSurface oldUserSurface = doc.CurrentUserLayer.Surface.Clone();
					TextEngine oldTextEngine = CurrentTextEngine.Clone();

					//Draw the text onto the UserLayer (without the cursor) rather than the TextLayer.
					RedrawText(false, false);

					//Clear the TextLayer.
					doc.CurrentUserLayer.TextLayer.Layer.Clear();

					//Clear the text and its boundaries.
					CurrentTextEngine.Clear();
					CurrentTextBounds = Gdk.Rectangle.Zero;

					//Create a new TextHistoryItem so that the finalization of the text can be undone. Construct
					//it on the spot so that it is more memory efficient if the changes are small.
					TextHistoryItem hist = new TextHistoryItem(Icon, FinalizeName, oldTextSurface, oldUserSurface,
							oldTextEngine, doc.CurrentUserLayer);

					//Add the new TextHistoryItem.
					doc.History.PushNewItem(hist);

					//Stop ignoring any Surface.Clone calls from this point on.
					ignoreCloneFinalizations = false;

					//Now that the text has been finalized, change its state.
					CurrentTextEngine.State = TextMode.Unchanged;

					if (selection != null)
					{
						selection.Dispose ();
						selection = null;
					}
				}
			}
		}

		private void InflateAndInvalidate(Rectangle passedRectangle)
		{
			//Create a new instance to preserve the passed Rectangle.
			Rectangle r = new Rectangle(passedRectangle.Location, passedRectangle.Size);

			r.Inflate(2, 2);
			PintaCore.Workspace.Invalidate(r);
		}
#endregion
#region Undo/Redo

		public override bool TryHandleUndo ()
		{
			if (is_editing)
			{
				// commit a history item to let the undo action undo text history item
				StopEditing(false);
			}

			return false;
		}

		public override bool TryHandleRedo()
		{
			//Rather than redoing something, if the text has been edited then simply commit and do not redo.
			if (is_editing && CurrentTextEngine.State == TextMode.Uncommitted)
			{
				//Commit a new TextHistoryItem.
				StopEditing(false);

				return true;
			}

			return false;
		}

#endregion
#region Copy/Paste

		public override bool TryHandlePaste (Clipboard cb)
		{
			if (!is_editing) {
				return false;
			}

			if (!CurrentTextEngine.PerformPaste (cb)) {
				return false;
			}
			RedrawText (true, true);
			return true;
		}

		public override bool TryHandleCopy (Clipboard cb)
		{
			if (!is_editing) {
				return false;
			}
			CurrentTextEngine.PerformCopy (cb);
			return true;
		}

		public override bool TryHandleCut (Clipboard cb)
		{
			if (!is_editing) {
				return false;
			}
			CurrentTextEngine.PerformCut (cb);
			RedrawText (true, true);
			return true;
		}

#endregion#endregion
	}
}
