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
		private Cairo.ImageSurface? text_undo_surface;
		private Cairo.ImageSurface? user_undo_surface;
		private TextEngine? undo_engine;
		// The selection from when editing started. This ensures that text doesn't suddenly disappear/appear
		// if the selection changes before the text is finalized.
		private DocumentSelection? selection;

		private Gtk.IMMulticontext imContext;
		private Pinta.Core.TextLayout layout;

		private Rectangle CurrentTextBounds {
			get {
				return PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.textBounds;
			}

			set {
				PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.previousTextBounds = PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.textBounds;

				PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.textBounds = value;
			}
		}

		private TextEngine CurrentTextEngine {
			get {
				if (!PintaCore.Workspace.HasOpenDocuments)
					throw new InvalidOperationException ("Attempting to get CurrentTextEngine when there are no open documents");

				return PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.tEngine;
			}
		}

		private Pinta.Core.TextLayout CurrentTextLayout {
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
		private Point lastMousePosition = new Point (0, 0);

		//Whether or not the previous TextTool mouse cursor shown was the normal one.
		private bool previousMouseCursorNormal = true;

		public override string Name { get { return Translations.GetString ("Text"); } }
		private string FinalizeName { get { return Translations.GetString ("Text - Finalize"); } }
		public override string Icon { get { return Pinta.Resources.Icons.ToolText; } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.T; } }
		public override int Priority { get { return 35; } }

		public override string StatusBarText {
			get { return Translations.GetString ("Left click to place cursor, then type desired text. Text color is primary color."); }
		}

		public override Gdk.Cursor DefaultCursor {
			get {
				return new Gdk.Cursor (Gdk.Display.Default,
						PintaCore.Resources.GetIcon ("Cursor.Text.png"),
						16, 16);
			}
		}
		public Gdk.Cursor InvalidEditCursor { get { return new Gdk.Cursor (Gdk.Display.Default, IconTheme.Default.LoadIcon (Pinta.Resources.Icons.EditSelectionErase, 16), 8, 0); } }

		#region Constructor
		public TextTool (IServiceManager services) : base (services)
		{
			cursor_hand = new Gdk.Cursor (Gdk.Display.Default, PintaCore.Resources.GetIcon ("Cursor.Pan.png"), 8, 8);
			imContext = new Gtk.IMMulticontext ();
			imContext.Commit += OnIMCommit;
			layout = new Pinta.Core.TextLayout ();
		}
		#endregion

		#region ToolBar
		// NRT - Created by OnBuildToolBar
		private ToolBarLabel font_label = null!;
		private ToolBarWidget<FontButton> font_button = null!;
		private ToolBarToggleButton bold_btn = null!;
		private ToolBarToggleButton italic_btn = null!;
		private ToolBarToggleButton underscore_btn = null!;
		private ToolBarToggleButton left_alignment_btn = null!;
		private ToolBarToggleButton center_alignment_btn = null!;
		private ToolBarToggleButton Right_alignment_btn = null!;
		private ToolBarLabel fill_label = null!;
		private ToolBarDropDownButton fill_button = null!;
		private SeparatorToolItem fill_sep = null!;
		private SeparatorToolItem outline_sep = null!;
		private ToolBarWidget<SpinButton> outline_width = null!;
		private ToolBarLabel outline_width_label = null!;

		private const string FONT_SETTING = "text-font";
		private const string BOLD_SETTING = "text-bold";
		private const string ITALIC_SETTING = "text-italic";
		private const string UNDERLINE_SETTING = "text-underline";
		private const string ALIGNMENT_SETTING = "text-alignment";
		private const string STYLE_SETTING = "text-style";
		private const string OUTLINE_WIDTH_SETTING = "text-outline-width";

		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			if (font_label == null)
				font_label = new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Font")));

			tb.AppendItem (font_label);

			if (font_button == null) {
				font_button = new (new FontButton () { ShowStyle = true, ShowSize = true, UseFont = true });
				// Default to Arial if possible.
				font_button.Widget.Font = Settings.GetSetting (FONT_SETTING, "Arial 12");

				font_button.Widget.FontSet += HandleFontChanged;
			}

			tb.AppendItem (font_button);

			tb.AppendItem (new SeparatorToolItem ());

			if (bold_btn == null) {
				bold_btn = new ToolBarToggleButton ("Toolbar.Bold.png", Translations.GetString ("Bold"), Translations.GetString ("Bold"));
				bold_btn.Active = Settings.GetSetting (BOLD_SETTING, false);
				bold_btn.Toggled += HandleBoldButtonToggled;
			}

			tb.AppendItem (bold_btn);

			if (italic_btn == null) {
				italic_btn = new ToolBarToggleButton ("Toolbar.Italic.png", Translations.GetString ("Italic"), Translations.GetString ("Italic"));
				italic_btn.Active = Settings.GetSetting (ITALIC_SETTING, false);
				italic_btn.Toggled += HandleItalicButtonToggled;
			}

			tb.AppendItem (italic_btn);

			if (underscore_btn == null) {
				underscore_btn = new ToolBarToggleButton ("Toolbar.Underline.png", Translations.GetString ("Underline"), Translations.GetString ("Underline"));
				underscore_btn.Active = Settings.GetSetting (UNDERLINE_SETTING, false);
				underscore_btn.Toggled += HandleUnderscoreButtonToggled;
			}

			tb.AppendItem (underscore_btn);

			tb.AppendItem (new SeparatorToolItem ());

			var alignment = (TextAlignment) Settings.GetSetting (ALIGNMENT_SETTING, (int) TextAlignment.Left);

			if (left_alignment_btn == null) {
				left_alignment_btn = new ToolBarToggleButton ("Toolbar.LeftAlignment.png", Translations.GetString ("Left Align"), Translations.GetString ("Left Align"));
				left_alignment_btn.Active = alignment == TextAlignment.Left;
				left_alignment_btn.Toggled += HandleLeftAlignmentButtonToggled;
			}

			tb.AppendItem (left_alignment_btn);

			if (center_alignment_btn == null) {
				center_alignment_btn = new ToolBarToggleButton ("Toolbar.CenterAlignment.png", Translations.GetString ("Center Align"), Translations.GetString ("Center Align"));
				center_alignment_btn.Active = alignment == TextAlignment.Center;
				center_alignment_btn.Toggled += HandleCenterAlignmentButtonToggled;
			}

			tb.AppendItem (center_alignment_btn);

			if (Right_alignment_btn == null) {
				Right_alignment_btn = new ToolBarToggleButton ("Toolbar.RightAlignment.png", Translations.GetString ("Right Align"), Translations.GetString ("Right Align"));
				Right_alignment_btn.Active = alignment == TextAlignment.Right;
				Right_alignment_btn.Toggled += HandleRightAlignmentButtonToggled;
			}

			tb.AppendItem (Right_alignment_btn);

			if (fill_sep == null)
				fill_sep = new Gtk.SeparatorToolItem ();

			tb.AppendItem (fill_sep);

			if (fill_label == null)
				fill_label = new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Text Style")));

			tb.AppendItem (fill_label);

			if (fill_button == null) {
				fill_button = new ToolBarDropDownButton ();

				fill_button.AddItem (Translations.GetString ("Normal"), Pinta.Resources.Icons.FillStyleFill, 0);
				fill_button.AddItem (Translations.GetString ("Normal and Outline"), Pinta.Resources.Icons.FillStyleOutlineFill, 1);
				fill_button.AddItem (Translations.GetString ("Outline"), Pinta.Resources.Icons.FillStyleOutline, 2);
				fill_button.AddItem (Translations.GetString ("Fill Background"), Pinta.Resources.Icons.FillStyleBackground, 3);

				fill_button.SelectedIndex = Settings.GetSetting (STYLE_SETTING, 0);
				fill_button.SelectedItemChanged += HandleBoldButtonToggled;
			}

			tb.AppendItem (fill_button);

			if (outline_sep == null)
				outline_sep = new SeparatorToolItem ();

			tb.AppendItem (outline_sep);

			if (outline_width_label == null)
				outline_width_label = new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Outline width")));

			tb.AppendItem (outline_width_label);

			if (outline_width == null) {
				outline_width = new (new SpinButton (1, 1e5, 1) { Value = Settings.GetSetting (OUTLINE_WIDTH_SETTING, 2) });
				outline_width.Widget.ValueChanged += HandleFontChanged;
			}

			tb.AppendItem (outline_width);

			outline_width.Visible = outline_width_label.Visible = outline_sep.Visible = StrokeText;

			UpdateFont ();

			if (PintaCore.Workspace.HasOpenDocuments) {
				//Make sure the event handler is never added twice.
				PintaCore.Workspace.ActiveDocument.LayerCloned -= FinalizeText;

				//When an ImageSurface is Cloned, finalize the re-editable text (if applicable).
				PintaCore.Workspace.ActiveDocument.LayerCloned += FinalizeText;
			}
		}

		protected override void OnSaveSettings (ISettingsService settings)
		{
			base.OnSaveSettings (settings);

			if (font_button is not null)
				settings.PutSetting (FONT_SETTING, font_button.Widget.Font);
			if (bold_btn is not null)
				settings.PutSetting (BOLD_SETTING, bold_btn.Active);
			if (italic_btn is not null)
				settings.PutSetting (ITALIC_SETTING, italic_btn.Active);
			if (underscore_btn is not null)
				settings.PutSetting (UNDERLINE_SETTING, underscore_btn.Active);
			if (left_alignment_btn is not null)
				settings.PutSetting (ALIGNMENT_SETTING, (int) Alignment);
			if (fill_button is not null)
				settings.PutSetting (STYLE_SETTING, fill_button.SelectedIndex);
			if (outline_width is not null)
				settings.PutSetting (OUTLINE_WIDTH_SETTING, outline_width.Widget.ValueAsInt);
		}

		private void HandleFontChanged (object? sender, EventArgs e)
		{
			if (PintaCore.Workspace.HasOpenDocuments)
				PintaCore.Workspace.ActiveDocument.Workspace.Canvas.GrabFocus ();

			UpdateFont ();
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

		private void HandlePintaCorePalettePrimaryColorChanged (object? sender, EventArgs e)
		{
			if (is_editing)
				RedrawText (true, true);
		}

		private void HandleLeftAlignmentButtonToggled (object? sender, EventArgs e)
		{
			if (left_alignment_btn.Active) {
				Right_alignment_btn.Active = false;
				center_alignment_btn.Active = false;
			} else if (!Right_alignment_btn.Active && !center_alignment_btn.Active) {
				left_alignment_btn.Active = true;
			}

			UpdateFont ();
		}

		private void HandleCenterAlignmentButtonToggled (object? sender, EventArgs e)
		{
			if (center_alignment_btn.Active) {
				Right_alignment_btn.Active = false;
				left_alignment_btn.Active = false;
			} else if (!Right_alignment_btn.Active && !left_alignment_btn.Active) {
				center_alignment_btn.Active = true;
			}

			UpdateFont ();
		}

		private void HandleRightAlignmentButtonToggled (object? sender, EventArgs e)
		{
			if (Right_alignment_btn.Active) {
				center_alignment_btn.Active = false;
				left_alignment_btn.Active = false;
			} else if (!center_alignment_btn.Active && !left_alignment_btn.Active) {
				Right_alignment_btn.Active = true;
			}

			UpdateFont ();
		}

		private void HandleUnderscoreButtonToggled (object? sender, EventArgs e)
		{
			UpdateFont ();
		}

		private void HandleItalicButtonToggled (object? sender, EventArgs e)
		{
			UpdateFont ();
		}

		private void HandleBoldButtonToggled (object? sender, EventArgs e)
		{
			outline_width.Visible = outline_width_label.Visible = outline_sep.Visible = StrokeText;

			UpdateFont ();
		}

		private void HandleSelectedLayerChanged (object? sender, EventArgs e)
		{
			UpdateFont ();
		}

		private void UpdateFont ()
		{
			if (PintaCore.Workspace.HasOpenDocuments) {
				var font = font_button.Widget.FontDesc.Copy ();
				font.Weight = bold_btn.Active ? Pango.Weight.Bold : Pango.Weight.Normal;
				font.Style = italic_btn.Active ? Pango.Style.Italic : Pango.Style.Normal;

				CurrentTextEngine.SetFont (font, Alignment, underscore_btn.Active);
			}

			if (is_editing)
				RedrawText (true, true);
		}

		private int OutlineWidth => outline_width.Widget.ValueAsInt;

		protected bool StrokeText { get { return (fill_button.SelectedItem.GetTagOrDefault (0) >= 1 && fill_button.SelectedItem.GetTagOrDefault (0) != 3); } }
		protected bool FillText { get { return fill_button.SelectedItem.GetTagOrDefault (0) <= 1 || fill_button.SelectedItem.GetTagOrDefault (0) == 3; } }
		protected bool BackgroundFill { get { return fill_button.SelectedItem.GetTagOrDefault (0) == 3; } }
		#endregion

		#region Activation/Deactivation
		protected override void OnActivated (Document? document)
		{
			base.OnActivated (document);

			// We may need to redraw our text when the color changes
			PintaCore.Palette.PrimaryColorChanged += HandlePintaCorePalettePrimaryColorChanged;
			PintaCore.Palette.SecondaryColorChanged += HandlePintaCorePalettePrimaryColorChanged;

			PintaCore.Layers.LayerAdded += HandleSelectedLayerChanged;
			PintaCore.Layers.LayerRemoved += HandleSelectedLayerChanged;
			PintaCore.Layers.SelectedLayerChanged += HandleSelectedLayerChanged;

			// We always start off not in edit mode
			is_editing = false;
		}

		protected override void OnCommit (Document? document)
		{
			imContext.FocusOut ();
			StopEditing (false);
		}

		protected override void OnDeactivated (Document? document, BaseTool? newTool)
		{
			base.OnDeactivated (document, newTool);

			// Stop listening for color change events
			PintaCore.Palette.PrimaryColorChanged -= HandlePintaCorePalettePrimaryColorChanged;
			PintaCore.Palette.SecondaryColorChanged -= HandlePintaCorePalettePrimaryColorChanged;

			PintaCore.Layers.LayerAdded -= HandleSelectedLayerChanged;
			PintaCore.Layers.LayerRemoved -= HandleSelectedLayerChanged;
			PintaCore.Layers.SelectedLayerChanged -= HandleSelectedLayerChanged;

			StopEditing (false);
		}
		#endregion

		#region Mouse Handlers
		protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
		{
			ctrlKey = e.IsControlPressed;

			//Store the mouse position.
			Point pt = e.PointDouble.ToGdkPoint ();

			// Grab focus so we can get keystrokes
			imContext.FocusIn ();

			if (selection != null)
				selection.Dispose ();
			selection = document.Selection.Clone ();

			// A right click allows you to move the text around
			if (e.MouseButton == MouseButton.Right) {
				//The user is dragging text with the right mouse button held down, so track the mouse as it moves.
				tracking = true;

				//Remember the position of the mouse before the text is dragged.
				startMouseXY = e.PointDouble;
				startClickPoint = clickPoint;

				//Change the cursor to indicate that the text is being dragged.
				SetCursor (cursor_hand);

				return;
			}

			// The user clicked the left mouse button			
			if (e.MouseButton == MouseButton.Left) {
				// If the user is [editing or holding down Ctrl] and clicked
				//within the text, move the cursor to the click location
				if ((is_editing || ctrlKey) && CurrentTextBounds.ContainsCorrect (pt)) {
					StartEditing ();

					//Change the position of the cursor to where the mouse clicked.
					TextPosition p = CurrentTextLayout.PointToTextPosition (pt);
					CurrentTextEngine.SetCursorPosition (p, true);

					//Redraw the text with the new cursor position.
					RedrawText (true, true);

					return;
				}

				// We're already editing and the user clicked outside the text,
				// commit the user's work, and start a new edit
				switch (CurrentTextEngine.State) {
					// We were editing, save and stop
					case TextMode.Uncommitted:
						StopEditing (true);
						break;

					// We were editing, but nothing had been
					// keyed. Stop editing.
					case TextMode.Unchanged:
						StopEditing (false);
						break;
				}

				if (ctrlKey) {
					//Go through every UserLayer.
					foreach (UserLayer ul in document.Layers.UserLayers) {
						//Check each UserLayer's editable text boundaries to see if they contain the mouse position.
						if (ul.textBounds.ContainsCorrect (pt)) {
							//The mouse clicked on editable text.

							//Change the current UserLayer to the Layer that contains the text that was clicked on.
							document.Layers.SetCurrentUserLayer (ul);

							//The user is editing text now.
							is_editing = true;

							//Set the cursor in the editable text where the mouse was clicked.
							TextPosition p = CurrentTextLayout.PointToTextPosition (pt);
							CurrentTextEngine.SetCursorPosition (p, true);

							//Redraw the editable text with the cursor.
							RedrawText (true, true);

							//Don't check any more UserLayers - stop at the first UserLayer that has editable text containing the mouse position.
							return;
						}
					}
				} else {
					if (CurrentTextEngine.State == TextMode.NotFinalized) {
						//The user is making a new text and the old text hasn't been finalized yet.
						FinalizeText ();
					}

					if (!is_editing) {
						// Start editing at the cursor location
						clickPoint = pt;
						CurrentTextEngine.Clear ();
						UpdateFont ();
						clickPoint.Offset (0, -CurrentTextLayout.FontHeight / 2);
						CurrentTextEngine.Origin = clickPoint;
						StartEditing ();
						RedrawText (true, true);
					}
				}
			}
		}

		protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
		{
			ctrlKey = e.IsControlPressed;

			lastMousePosition = e.PointDouble.ToGdkPoint ();

			// If we're dragging the text around, do that
			if (tracking) {
				Cairo.PointD delta = new Cairo.PointD (e.PointDouble.X - startMouseXY.X, e.PointDouble.Y - startMouseXY.Y);

				clickPoint = new Point ((int) (startClickPoint.X + delta.X), (int) (startClickPoint.Y + delta.Y));
				CurrentTextEngine.Origin = clickPoint;

				RedrawText (true, true);
			} else {
				UpdateMouseCursor (document);
			}
		}

		protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
		{
			// If we were dragging the text around, finish that up
			if (tracking) {
				Cairo.PointD delta = new Cairo.PointD (e.PointDouble.X - startMouseXY.X, e.PointDouble.Y - startMouseXY.Y);

				clickPoint = new Point ((int) (startClickPoint.X + delta.X), (int) (startClickPoint.Y + delta.Y));
				CurrentTextEngine.Origin = clickPoint;

				RedrawText (false, true);
				tracking = false;
				SetCursor (null);
			}
		}

		private void UpdateMouseCursor (Document document)
		{
			//Whether or not to show the normal text cursor.
			bool showNormalCursor = false;

			if (ctrlKey && PintaCore.Workspace.HasOpenDocuments) {
				//Go through every UserLayer.
				foreach (UserLayer ul in document.Layers.UserLayers) {
					//Check each UserLayer's editable text boundaries to see if they contain the mouse position.
					if (ul.textBounds.ContainsCorrect (lastMousePosition)) {
						//The mouse is over editable text.
						showNormalCursor = true;
					}

				}
			} else {
				showNormalCursor = true;
			}

			if (showNormalCursor) {
				if (!previousMouseCursorNormal) {
					SetCursor (DefaultCursor);

					previousMouseCursorNormal = showNormalCursor;

					if (PintaCore.Workspace.HasOpenDocuments)
						RedrawText (is_editing, true);
				}
			} else {
				if (previousMouseCursorNormal) {
					SetCursor (InvalidEditCursor);

					previousMouseCursorNormal = showNormalCursor;

					RedrawText (is_editing, true);
				}
			}
		}
		#endregion

		#region Keyboard Handlers
		protected override bool OnKeyDown (Document document, ToolKeyEventArgs e)
		{
			if (!PintaCore.Workspace.HasOpenDocuments) {
				return false;
			}

			// If we are dragging the text, we
			// aren't going to handle key presses
			if (tracking)
				return false;

			// Ignore anything with Alt pressed
			if (e.IsAltPressed)
				return false;

			ctrlKey = e.Key.IsControlKey ();
			UpdateMouseCursor (document);

			// Assume that we are going to handle the key
			bool keyHandled = true;

			if (is_editing) {
				switch (e.Key) {
					case Gdk.Key.BackSpace:
						CurrentTextEngine.PerformBackspace ();
						break;

					case Gdk.Key.Delete:
						CurrentTextEngine.PerformDelete ();
						break;

					case Gdk.Key.KP_Enter:
					case Gdk.Key.Return:
						CurrentTextEngine.PerformEnter ();
						break;

					case Gdk.Key.Left:
						CurrentTextEngine.PerformLeft (e.IsControlPressed, e.IsShiftPressed);
						break;

					case Gdk.Key.Right:
						CurrentTextEngine.PerformRight (e.IsControlPressed, e.IsShiftPressed);
						break;

					case Gdk.Key.Up:
						CurrentTextEngine.PerformUp (e.IsShiftPressed);
						break;

					case Gdk.Key.Down:
						CurrentTextEngine.PerformDown (e.IsShiftPressed);
						break;

					case Gdk.Key.Home:
						CurrentTextEngine.PerformHome (e.IsControlPressed, e.IsShiftPressed);
						break;

					case Gdk.Key.End:
						CurrentTextEngine.PerformEnd (e.IsControlPressed, e.IsShiftPressed);
						break;

					case Gdk.Key.Next:
					case Gdk.Key.Prior:
						break;

					case Gdk.Key.Escape:
						StopEditing (false);
						return true;
					case Gdk.Key.Insert:
						if (e.IsShiftPressed) {
							Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
							CurrentTextEngine.PerformPaste (cb);
						} else if (e.IsControlPressed) {
							Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
							CurrentTextEngine.PerformCopy (cb);
						}
						break;
					default:
						if (e.IsControlPressed) {
							if (e.Key == Gdk.Key.z) {
								//Ctrl + Z for undo while editing.
								OnHandleUndo (document);

								if (PintaCore.Workspace.ActiveDocument.History.CanUndo)
									PintaCore.Workspace.ActiveDocument.History.Undo ();

								return true;
							} else if (e.Key == Gdk.Key.i) {
								italic_btn.Toggle ();
								UpdateFont ();
							} else if (e.Key == Gdk.Key.b) {
								bold_btn.Toggle ();
								UpdateFont ();
							} else if (e.Key == Gdk.Key.u) {
								underscore_btn.Toggle ();
								UpdateFont ();
							} else if (e.Key == Gdk.Key.a) {
								// Select all of the text.
								CurrentTextEngine.PerformHome (false, false);
								CurrentTextEngine.PerformEnd (true, true);
							} else {
								//Ignore command shortcut.
								return false;
							}
						} else {
							if (e.Event is not null)
								keyHandled = TryHandleChar (e.Event);
						}

						break;
				}
				if (keyHandled) {
					imContext.FocusOut ();
					RedrawText (true, true);
					imContext.FocusIn ();
				}
			} else {
				// If we're not editing, allow the key press to be handled elsewhere (e.g. for selecting another tool).
				keyHandled = false;
			}

			return keyHandled;
		}

		protected override bool OnKeyUp (Document document, ToolKeyEventArgs e)
		{
			if (e.Key.IsControlKey () || e.IsControlPressed) {
				ctrlKey = false;

				UpdateMouseCursor (document);
			}
			return false;
		}

		private bool TryHandleChar (EventKey eventKey)
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
						utf32Char = (char) char.ConvertToUtf32 (args.Str, i);
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

			imContext.ClientWindow = PintaCore.Workspace.ActiveWorkspace.Canvas.Window;

			if (selection == null)
				selection = PintaCore.Workspace.ActiveDocument.Selection.Clone ();

			//Start ignoring any Surface.Clone calls from this point on (so that it doesn't start to loop).
			ignoreCloneFinalizations = true;

			//Store the previous state of the current UserLayer's and TextLayer's ImageSurfaces.
			user_undo_surface = PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.Surface.Clone ();
			text_undo_surface = PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.TextLayer.Layer.Surface.Clone ();

			//Store the previous state of the Text Engine.
			undo_engine = CurrentTextEngine.Clone ();

			//Stop ignoring any Surface.Clone calls from this point on.
			ignoreCloneFinalizations = false;
		}

		private void StopEditing (bool finalize)
		{
			imContext.ClientWindow = null;

			if (!PintaCore.Workspace.HasOpenDocuments)
				return;

			if (!is_editing)
				return;

			is_editing = false;

			//Make sure that neither undo surface is null, the user is editing, and there are uncommitted changes.
			if (text_undo_surface != null && user_undo_surface != null && CurrentTextEngine.State == TextMode.Uncommitted) {
				Document doc = PintaCore.Workspace.ActiveDocument;

				RedrawText (false, true);

				//Start ignoring any Surface.Clone calls from this point on (so that it doesn't start to loop).
				ignoreCloneFinalizations = true;

				//Create a new TextHistoryItem so that the committing of text can be undone.
				doc.History.PushNewItem (new TextHistoryItem (Icon, Name,
							text_undo_surface.Clone (), user_undo_surface.Clone (),
							undo_engine!.Clone (), doc.Layers.CurrentUserLayer)); // NRT - Set in StartEditing

				//Stop ignoring any Surface.Clone calls from this point on.
				ignoreCloneFinalizations = false;

				//Now that the text has been committed, change its state.
				CurrentTextEngine.State = TextMode.NotFinalized;
			}

			RedrawText (false, true);

			if (finalize) {
				FinalizeText ();
			}
		}
		#endregion

		#region Text Drawing Methods
		/// <summary>
		/// Clears the entire TextLayer and redraw the previous text boundary.
		/// </summary>
		private void ClearTextLayer ()
		{
			//Clear the TextLayer.
			PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.TextLayer.Layer.Surface.Clear ();

			//Redraw the previous text boundary.
			InflateAndInvalidate (PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.previousTextBounds);
		}

		/// <summary>
		/// Draws the text.
		/// </summary>
		/// <param name="showCursor">Whether or not to show the mouse cursor in the drawing.</param>
		/// <param name="useTextLayer">Whether or not to use the TextLayer (as opposed to the Userlayer).</param>
		private void RedrawText (bool showCursor, bool useTextLayer)
		{
			Rectangle r = CurrentTextLayout.GetLayoutBounds ();
			r.Inflate (10 + OutlineWidth, 10 + OutlineWidth);
			InflateAndInvalidate (r);
			CurrentTextBounds = r;

			Rectangle cursorBounds = Rectangle.Zero;

			Cairo.ImageSurface surf;

			if (!useTextLayer) {
				//Draw text on the current UserLayer's surface as finalized text.
				surf = PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.Surface;
			} else {
				//Draw text on the current UserLayer's TextLayer's surface as re-editable text.
				surf = PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.TextLayer.Layer.Surface;

				ClearTextLayer ();
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
					selection.Clip (g);
				}

				g.MoveTo (new Cairo.PointD (CurrentTextEngine.Origin.X, CurrentTextEngine.Origin.Y));

				g.SetSourceColor (PintaCore.Palette.PrimaryColor);

				//Fill in background
				if (BackgroundFill) {
					using (var g2 = new Cairo.Context (surf)) {
						if (selection != null) {
							selection.Clip (g2);
						}

						g2.FillRectangle (CurrentTextLayout.GetLayoutBounds ().ToCairoRectangle (), PintaCore.Palette.SecondaryColor);
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


				if (useTextLayer && (is_editing || ctrlKey) && !CurrentTextEngine.IsEmpty ()) {
					//Draw the text edit rectangle.

					g.Save ();

					g.Translate (.5, .5);

					using (Cairo.Path p = g.CreateRectanglePath (CurrentTextBounds.ToCairoRectangle ())) {
						g.AppendPath (p);
					}

					g.LineWidth = 1;

					g.SetSourceColor (new Cairo.Color (1, 1, 1));
					g.StrokePreserve ();

					g.SetDash (new double[] { 2, 4 }, 0);
					g.SetSourceColor (new Cairo.Color (1, .1, .2));

					g.Stroke ();

					g.Restore ();
				}
			}

			InflateAndInvalidate (PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.previousTextBounds);
			PintaCore.Workspace.Invalidate (old_cursor_bounds);
			InflateAndInvalidate (r);
			PintaCore.Workspace.Invalidate (cursorBounds);

			old_cursor_bounds = cursorBounds;
		}

		/// <summary>
		/// Finalize re-editable text (if applicable).
		/// </summary>
		public void FinalizeText ()
		{
			//If this is true, don't finalize any text - this is used to prevent the code from looping recursively.
			if (!ignoreCloneFinalizations) {
				//Only bother finalizing text if editing.
				if (CurrentTextEngine.State != TextMode.Unchanged) {
					//Start ignoring any Surface.Clone calls from this point on (so that it doesn't start to loop).
					ignoreCloneFinalizations = true;
					Document doc = PintaCore.Workspace.ActiveDocument;

					//Create a backup of everything before redrawing the text and etc.
					Cairo.ImageSurface oldTextSurface = doc.Layers.CurrentUserLayer.TextLayer.Layer.Surface.Clone ();
					Cairo.ImageSurface oldUserSurface = doc.Layers.CurrentUserLayer.Surface.Clone ();
					TextEngine oldTextEngine = CurrentTextEngine.Clone ();

					//Draw the text onto the UserLayer (without the cursor) rather than the TextLayer.
					RedrawText (false, false);

					//Clear the TextLayer.
					doc.Layers.CurrentUserLayer.TextLayer.Layer.Clear ();

					//Clear the text and its boundaries.
					CurrentTextEngine.Clear ();
					CurrentTextBounds = Gdk.Rectangle.Zero;

					//Create a new TextHistoryItem so that the finalization of the text can be undone. Construct
					//it on the spot so that it is more memory efficient if the changes are small.
					TextHistoryItem hist = new TextHistoryItem (Icon, FinalizeName, oldTextSurface, oldUserSurface,
							oldTextEngine, doc.Layers.CurrentUserLayer);

					//Add the new TextHistoryItem.
					doc.History.PushNewItem (hist);

					//Stop ignoring any Surface.Clone calls from this point on.
					ignoreCloneFinalizations = false;

					//Now that the text has been finalized, change its state.
					CurrentTextEngine.State = TextMode.Unchanged;

					if (selection != null) {
						selection.Dispose ();
						selection = null;
					}
				}
			}
		}

		private void InflateAndInvalidate (Rectangle passedRectangle)
		{
			//Create a new instance to preserve the passed Rectangle.
			Rectangle r = new Rectangle (passedRectangle.Location, passedRectangle.Size);

			r.Inflate (2, 2);
			PintaCore.Workspace.Invalidate (r);
		}
		#endregion
		#region Undo/Redo

		protected override bool OnHandleUndo (Document document)
		{
			if (is_editing) {
				// commit a history item to let the undo action undo text history item
				StopEditing (false);
			}

			return false;
		}

		protected override bool OnHandleRedo (Document document)
		{
			//Rather than redoing something, if the text has been edited then simply commit and do not redo.
			if (is_editing && CurrentTextEngine.State == TextMode.Uncommitted) {
				//Commit a new TextHistoryItem.
				StopEditing (false);

				return true;
			}

			return false;
		}

		#endregion
		#region Copy/Paste

		protected override bool OnHandlePaste (Document document, Clipboard cb)
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

		protected override bool OnHandleCopy (Document document, Clipboard cb)
		{
			if (!is_editing) {
				return false;
			}
			CurrentTextEngine.PerformCopy (cb);
			return true;
		}

		protected override bool OnHandleCut (Document document, Clipboard cb)
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
