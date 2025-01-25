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
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class TextTool : BaseTool
{
	// Variables for dragging
	private PointD start_mouse_xy;
	private PointI start_click_point;
	private bool tracking;
	private readonly Gdk.Cursor cursor_move = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.Move);
	private readonly Gdk.Cursor cursor_invalid = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.NotAllowed);

	private PointI click_point;
	private bool is_editing;
	private RectangleI old_cursor_bounds = RectangleI.Zero;

	//This is used to temporarily store the UserLayer's and TextLayer's previous ImageSurface states.
	private Cairo.ImageSurface? text_undo_surface;
	private Cairo.ImageSurface? user_undo_surface;
	private TextEngine? undo_engine;
	// The last pre-editing string, if pre-editing is active.
	private string? preedit_string;
	// The selection from when editing started. This ensures that text doesn't suddenly disappear/appear
	// if the selection changes before the text is finalized.
	private DocumentSelection? selection;

	private readonly Gtk.IMMulticontext im_context;
	private readonly TextLayout layout;

	private static RectangleI CurrentTextBounds {
		get => PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.TextBounds;

		set {
			PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.PreviousTextBounds = PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.TextBounds;
			PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.TextBounds = value;
		}
	}

	private static TextEngine CurrentTextEngine {
		get {
			if (!PintaCore.Workspace.HasOpenDocuments)
				throw new InvalidOperationException ("Attempting to get CurrentTextEngine when there are no open documents");

			return PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer.TextEngine;
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
	private bool ignore_clone_finalizations = false;

	//Whether or not either (or both) of the Ctrl keys are pressed.
	private bool ctrl_key = false;

	//Store the most recent mouse position.
	private PointI last_mouse_position = new (0, 0);

	//Whether or not the previous TextTool mouse cursor shown was the normal one.
	private bool previous_mouse_cursor_normal = true;

	public override string Name
		=> Translations.GetString ("Text");

	private static string FinalizeName
		=> Translations.GetString ("Text - Finalize");

	public override string Icon
		=> Pinta.Resources.Icons.ToolText;

	public override Gdk.Key ShortcutKey
		=> new (Gdk.Constants.KEY_T);

	public override int Priority
		=> 35;

	public override string StatusBarText
		=> Translations.GetString ("Left click to place cursor, then type desired text. Text color is primary color.");

	public override Gdk.Cursor DefaultCursor { get; }

	protected override bool ShowAntialiasingButton => true;

	private readonly IWorkspaceService workspace;
	private readonly IPaletteService palette;

	#region Constructor

	public TextTool (IServiceProvider services) : base (services)
	{
		workspace = services.GetService<IWorkspaceService> ();
		palette = services.GetService<IPaletteService> ();

		im_context = Gtk.IMMulticontext.New ();
		im_context.OnCommit += OnIMCommit;
		im_context.OnPreeditStart += OnPreeditStart;
		im_context.OnPreeditChanged += OnPreeditChanged;
		im_context.OnPreeditEnd += OnPreeditEnd;

		layout = new TextLayout ();

		DefaultCursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.Text);
	}
	#endregion

	#region ToolBar
	// NRT - Created by OnBuildToolBar
	private Gtk.Label font_label = null!;
	private Gtk.FontButton font_button = null!;
	private Gtk.ToggleButton bold_btn = null!;
	private Gtk.ToggleButton italic_btn = null!;
	private Gtk.ToggleButton underscore_btn = null!;
	private Gtk.ToggleButton left_alignment_btn = null!;
	private Gtk.ToggleButton center_alignment_btn = null!;
	private Gtk.ToggleButton right_alignment_btn = null!;
	private Gtk.Label fill_label = null!;
	private ToolBarDropDownButton fill_button = null!;
	private Gtk.Separator fill_sep = null!;
	private Gtk.Separator outline_sep = null!;
	private Gtk.SpinButton outline_width = null!;
	private Gtk.Label outline_width_label = null!;

	private const string FONT_SETTING = "text-font";
	private const string BOLD_SETTING = "text-bold";
	private const string ITALIC_SETTING = "text-italic";
	private const string UNDERLINE_SETTING = "text-underline";
	private const string ALIGNMENT_SETTING = "text-alignment";
	private const string STYLE_SETTING = "text-style";
	private const string OUTLINE_WIDTH_SETTING = "text-outline-width";

	protected override void OnBuildToolBar (Gtk.Box tb)
	{
		base.OnBuildToolBar (tb);

		if (font_label == null) {
			string fontText = Translations.GetString ("Font");
			font_label = Gtk.Label.New ($" {fontText}: ");
		}

		tb.Append (font_label);

		if (font_button == null) {
			font_button = new Gtk.FontButton {
				UseSize = false,
				UseFont = true,
				CanFocus = false, // Default to Arial if possible.
				Font = Settings.GetSetting (FONT_SETTING, "Arial 12"),
			};

			font_button.OnFontSet += HandleFontChanged;
		}

		tb.Append (font_button);

		tb.Append (GtkExtensions.CreateToolBarSeparator ());

		if (bold_btn == null) {
			bold_btn = new Gtk.ToggleButton {
				IconName = Pinta.Resources.StandardIcons.FormatTextBold,
				TooltipText = Translations.GetString ("Bold"),
				CanFocus = false,
				Active = Settings.GetSetting (BOLD_SETTING, false),
			};
			bold_btn.OnToggled += HandleBoldButtonToggled;
		}

		tb.Append (bold_btn);

		if (italic_btn == null) {
			italic_btn = new Gtk.ToggleButton {
				IconName = Pinta.Resources.StandardIcons.FormatTextItalic,
				TooltipText = Translations.GetString ("Italic"),
				CanFocus = false,
				Active = Settings.GetSetting (ITALIC_SETTING, false),
			};
			italic_btn.OnToggled += HandleItalicButtonToggled;
		}

		tb.Append (italic_btn);

		if (underscore_btn == null) {
			underscore_btn = new Gtk.ToggleButton {
				IconName = Pinta.Resources.StandardIcons.FormatTextUnderline,
				TooltipText = Translations.GetString ("Underline"),
				CanFocus = false,
				Active = Settings.GetSetting (UNDERLINE_SETTING, false),
			};
			underscore_btn.OnToggled += HandleUnderscoreButtonToggled;
		}

		tb.Append (underscore_btn);

		tb.Append (GtkExtensions.CreateToolBarSeparator ());

		var alignment = (TextAlignment) Settings.GetSetting (ALIGNMENT_SETTING, (int) TextAlignment.Left);

		if (left_alignment_btn == null) {
			left_alignment_btn = new Gtk.ToggleButton {
				IconName = Pinta.Resources.StandardIcons.FormatJustifyLeft,
				TooltipText = Translations.GetString ("Left Align"),
				CanFocus = false,
				Active = alignment == TextAlignment.Left,
			};
			left_alignment_btn.OnToggled += HandleLeftAlignmentButtonToggled;
		}

		tb.Append (left_alignment_btn);

		if (center_alignment_btn == null) {
			center_alignment_btn = new Gtk.ToggleButton {
				IconName = Pinta.Resources.StandardIcons.FormatJustifyCenter,
				TooltipText = Translations.GetString ("Center Align"),
				CanFocus = false,
				Active = alignment == TextAlignment.Center,
			};
			center_alignment_btn.OnToggled += HandleCenterAlignmentButtonToggled;
		}

		tb.Append (center_alignment_btn);

		if (right_alignment_btn == null) {
			right_alignment_btn = new Gtk.ToggleButton {
				IconName = Pinta.Resources.StandardIcons.FormatJustifyRight,
				TooltipText = Translations.GetString ("Right Align"),
				CanFocus = false,
				Active = alignment == TextAlignment.Right,
			};
			right_alignment_btn.OnToggled += HandleRightAlignmentButtonToggled;
		}

		tb.Append (right_alignment_btn);

		fill_sep ??= GtkExtensions.CreateToolBarSeparator ();

		tb.Append (fill_sep);

		if (fill_label == null) {
			string textStyleText = Translations.GetString ("Text Style");
			fill_label = Gtk.Label.New ($" {textStyleText}: ");
		}

		tb.Append (fill_label);

		if (fill_button == null) {
			fill_button = new ToolBarDropDownButton ();

			fill_button.AddItem (Translations.GetString ("Normal"), Pinta.Resources.Icons.FillStyleFill, 0);
			fill_button.AddItem (Translations.GetString ("Normal and Outline"), Pinta.Resources.Icons.FillStyleOutlineFill, 1);
			fill_button.AddItem (Translations.GetString ("Outline"), Pinta.Resources.Icons.FillStyleOutline, 2);
			fill_button.AddItem (Translations.GetString ("Fill Background"), Pinta.Resources.Icons.FillStyleBackground, 3);

			fill_button.SelectedIndex = Settings.GetSetting (STYLE_SETTING, 0);
			fill_button.SelectedItemChanged += HandleBoldButtonToggled;
		}

		tb.Append (fill_button);

		outline_sep ??= GtkExtensions.CreateToolBarSeparator ();

		tb.Append (outline_sep);

		if (outline_width_label == null) {
			string outlineWidthText = Translations.GetString ("Outline width");
			outline_width_label = Gtk.Label.New ($" {outlineWidthText}: ");
		}

		tb.Append (outline_width_label);

		if (outline_width == null) {
			outline_width = GtkExtensions.CreateToolBarSpinButton (1, 1e5, 1, Settings.GetSetting (OUTLINE_WIDTH_SETTING, 2));
			outline_width.OnValueChanged += HandleFontChanged;
		}

		tb.Append (outline_width);

		outline_width.Visible = outline_width_label.Visible = outline_sep.Visible = StrokeText;

		UpdateFont ();
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		if (font_button is not null)
			settings.PutSetting (FONT_SETTING, font_button.Font!);

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
			settings.PutSetting (OUTLINE_WIDTH_SETTING, outline_width.GetValueAsInt ());
	}

	private void HandleFontChanged (object? sender, EventArgs e)
	{
		if (workspace.HasOpenDocuments)
			workspace.ActiveDocument.Workspace.GrabFocusToCanvas ();

		UpdateFont ();
	}

	private TextAlignment Alignment {
		get {
			if (right_alignment_btn.Active)
				return TextAlignment.Right;
			else if (center_alignment_btn.Active)
				return TextAlignment.Center;
			else
				return TextAlignment.Left;
		}
	}

	private void HandlePintaCorePalettePrimaryColorChanged (object? sender, EventArgs e)
	{
		UpdateTextEngineColor ();
		if (is_editing || (workspace.HasOpenDocuments && CurrentTextEngine.State == TextMode.NotFinalized))
			RedrawText (is_editing, true);
	}

	private void HandleLeftAlignmentButtonToggled (object? sender, EventArgs e)
	{
		if (left_alignment_btn.Active) {
			right_alignment_btn.Active = false;
			center_alignment_btn.Active = false;
		} else if (!right_alignment_btn.Active && !center_alignment_btn.Active) {
			left_alignment_btn.Active = true;
		}

		UpdateFont ();
	}

	private void HandleCenterAlignmentButtonToggled (object? sender, EventArgs e)
	{
		if (center_alignment_btn.Active) {
			right_alignment_btn.Active = false;
			left_alignment_btn.Active = false;
		} else if (!right_alignment_btn.Active && !left_alignment_btn.Active) {
			center_alignment_btn.Active = true;
		}

		UpdateFont ();
	}

	private void HandleRightAlignmentButtonToggled (object? sender, EventArgs e)
	{
		if (right_alignment_btn.Active) {
			center_alignment_btn.Active = false;
			left_alignment_btn.Active = false;
		} else if (!center_alignment_btn.Active && !left_alignment_btn.Active) {
			right_alignment_btn.Active = true;
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

	protected override void OnAntialiasingChanged ()
	{
		UpdateFont ();
	}

	private void UpdateFont ()
	{
		if (workspace.HasOpenDocuments) {

			var font = font_button.GetFontDesc ()!.Copy ()!; // NRT: Only nullable when nullptr is passed.
			font.SetWeight (bold_btn.Active ? Pango.Weight.Bold : Pango.Weight.Normal);
			font.SetStyle (italic_btn.Active ? Pango.Style.Italic : Pango.Style.Normal);

			CurrentTextEngine.SetFont (font, Alignment, underscore_btn.Active);
		}

		if (is_editing || (workspace.HasOpenDocuments && CurrentTextEngine.State == TextMode.NotFinalized))
			RedrawText (is_editing, true);
	}

	private void UpdateTextEngineColor ()
	{
		if (PintaCore.Workspace.HasOpenDocuments) {
			CurrentTextEngine.PrimaryColor = palette.PrimaryColor;
			CurrentTextEngine.SecondaryColor = palette.SecondaryColor;
		}
	}

	private int OutlineWidth
		=> outline_width.GetValueAsInt ();

	private bool StrokeText
		=> fill_button.SelectedItem.GetTagOrDefault (0) >= 1 && fill_button.SelectedItem.GetTagOrDefault (0) != 3;

	private bool FillText
		=> fill_button.SelectedItem.GetTagOrDefault (0) <= 1 || fill_button.SelectedItem.GetTagOrDefault (0) == 3;

	private bool BackgroundFill
		=> fill_button.SelectedItem.GetTagOrDefault (0) == 3;

	#endregion

	#region Activation/Deactivation
	protected override void OnActivated (Document? document)
	{
		base.OnActivated (document);

		// We may need to redraw our text when the color changes
		palette.PrimaryColorChanged += HandlePintaCorePalettePrimaryColorChanged;
		palette.SecondaryColorChanged += HandlePintaCorePalettePrimaryColorChanged;

		workspace.LayerAdded += HandleSelectedLayerChanged;
		workspace.LayerRemoved += HandleSelectedLayerChanged;
		workspace.SelectedLayerChanged += HandleSelectedLayerChanged;

		// We always start off not in edit mode
		is_editing = false;
	}

	protected override void OnCommit (Document? document)
	{
		im_context.FocusOut ();
		StopEditing (false);
	}

	protected override void OnDeactivated (Document? document, BaseTool? newTool)
	{
		base.OnDeactivated (document, newTool);

		// Stop listening for color change events
		palette.PrimaryColorChanged -= HandlePintaCorePalettePrimaryColorChanged;
		palette.SecondaryColorChanged -= HandlePintaCorePalettePrimaryColorChanged;

		workspace.LayerAdded -= HandleSelectedLayerChanged;
		workspace.LayerRemoved -= HandleSelectedLayerChanged;
		workspace.SelectedLayerChanged -= HandleSelectedLayerChanged;

		StopEditing (false);
	}
	#endregion

	#region Mouse Handlers
	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		ctrl_key = e.IsControlPressed;
		im_context.FocusIn (); // Grab focus so we can get keystrokes
		selection = document.Selection.Clone ();

		switch (e.MouseButton) {
			case MouseButton.Right:
				HandleRightClick (e);
				break;
			case MouseButton.Left:
				HandleLeftClick (document, e);
				break;
		}
	}

	private void HandleLeftClick (Document document, ToolMouseEventArgs e)
	{
		//Store the mouse position.
		PointI pt = e.Point;

		// If the user is [editing or holding down Ctrl] and clicked
		//within the text, move the cursor to the click location
		if ((is_editing || ctrl_key) && CurrentTextBounds.Contains (pt)) {
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

		if (ctrl_key) {
			//Go through every UserLayer.
			foreach (UserLayer ul in document.Layers.UserLayers) {
				//Check each UserLayer's editable text boundaries to see if they contain the mouse position.
				if (!ul.TextBounds.Contains (pt))
					continue;

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
		} else {
			if (CurrentTextEngine.State == TextMode.NotFinalized) {
				//The user is making a new text and the old text hasn't been finalized yet.
				FinalizeText ();
			}

			if (!is_editing) {
				// Start editing at the cursor location
				click_point = pt;
				CurrentTextEngine.Clear ();
				UpdateFont ();
				click_point = click_point with { Y = click_point.Y - (CurrentTextLayout.FontHeight / 2) };
				CurrentTextEngine.Origin = click_point;
				StartEditing ();
				RedrawText (true, true);
			}
		}
	}

	private void HandleRightClick (ToolMouseEventArgs e)
	{
		// A right click allows you to move the text around

		//The user is dragging text with the right mouse button held down, so track the mouse as it moves.
		tracking = true;

		//Remember the position of the mouse before the text is dragged.
		start_mouse_xy = e.PointDouble;
		start_click_point = click_point;

		//Change the cursor to indicate that the text is being dragged.
		SetCursor (cursor_move);
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		ctrl_key = e.IsControlPressed;

		last_mouse_position = e.Point;

		// If we're dragging the text around, do that
		if (tracking) {
			PointD delta = new (
				e.PointDouble.X - start_mouse_xy.X,
				e.PointDouble.Y - start_mouse_xy.Y);

			click_point = new PointI ((int) (start_click_point.X + delta.X), (int) (start_click_point.Y + delta.Y));
			CurrentTextEngine.Origin = click_point;

			RedrawText (true, true);
		} else {
			UpdateMouseCursor (document);
		}
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		// If we were dragging the text around, finish that up
		if (!tracking)
			return;

		PointD delta = new (e.PointDouble.X - start_mouse_xy.X, e.PointDouble.Y - start_mouse_xy.Y);

		click_point = new PointI ((int) (start_click_point.X + delta.X), (int) (start_click_point.Y + delta.Y));
		CurrentTextEngine.Origin = click_point;

		RedrawText (false, true);
		tracking = false;
		SetCursor (null);
	}

	private void UpdateMouseCursor (Document document)
	{
		//Whether or not to show the normal text cursor.
		bool showNormalCursor = false;

		if (ctrl_key && workspace.HasOpenDocuments) {
			//Go through every UserLayer.
			foreach (UserLayer ul in document.Layers.UserLayers) {
				//Check each UserLayer's editable text boundaries to see if they contain the mouse position.
				if (ul.TextBounds.Contains (last_mouse_position)) {
					//The mouse is over editable text.
					showNormalCursor = true;
				}

			}
		} else {
			showNormalCursor = true;
		}

		if (showNormalCursor) {
			if (!previous_mouse_cursor_normal) {
				SetCursor (DefaultCursor);

				previous_mouse_cursor_normal = showNormalCursor;

				if (workspace.HasOpenDocuments)
					RedrawText (is_editing, true);
			}
		} else {
			if (previous_mouse_cursor_normal) {
				SetCursor (cursor_invalid);

				previous_mouse_cursor_normal = showNormalCursor;

				RedrawText (is_editing, true);
			}
		}
	}
	#endregion

	#region Keyboard Handlers

	protected override bool OnKeyDown (Document document, ToolKeyEventArgs e)
	{
		if (!workspace.HasOpenDocuments)
			return false;

		// If we are dragging the text, we
		// aren't going to handle key presses
		if (tracking)
			return false;

		// Ignore anything with Alt pressed
		if (e.IsAltPressed)
			return false;

		ctrl_key = e.Key.IsControlKey ();
		UpdateMouseCursor (document);

		bool keyHandled = false;
		if (is_editing) {
			if (preedit_string is not null && e.Event is not null) {
				// When pre-editing is active, the input method should consume all keystrokes first.
				// (e.g. Enter might be used to finish pre-editing)
				keyHandled = TryHandleChar (e.Event);
			}

			if (!keyHandled) {
				// Assume that we are going to handle the key
				keyHandled = true;

				switch (e.Key.Value) {
					case Gdk.Constants.KEY_BackSpace:
						CurrentTextEngine.PerformBackspace ();
						break;

					case Gdk.Constants.KEY_Delete:
						CurrentTextEngine.PerformDelete ();
						break;

					case Gdk.Constants.KEY_KP_Enter:
					case Gdk.Constants.KEY_Return:
						CurrentTextEngine.PerformEnter ();
						break;

					case Gdk.Constants.KEY_Left:
						CurrentTextEngine.PerformLeft (e.IsControlPressed, e.IsShiftPressed);
						break;

					case Gdk.Constants.KEY_Right:
						CurrentTextEngine.PerformRight (e.IsControlPressed, e.IsShiftPressed);
						break;

					case Gdk.Constants.KEY_Up:
						CurrentTextEngine.PerformUp (e.IsShiftPressed);
						break;

					case Gdk.Constants.KEY_Down:
						CurrentTextEngine.PerformDown (e.IsShiftPressed);
						break;

					case Gdk.Constants.KEY_Home:
						CurrentTextEngine.PerformHome (e.IsControlPressed, e.IsShiftPressed);
						break;

					case Gdk.Constants.KEY_End:
						CurrentTextEngine.PerformEnd (e.IsControlPressed, e.IsShiftPressed);
						break;

					case Gdk.Constants.KEY_Next:
					case Gdk.Constants.KEY_Prior:
						break;

					case Gdk.Constants.KEY_Escape:
						StopEditing (false);
						return true;
					case Gdk.Constants.KEY_Insert:
						if (e.IsShiftPressed) {
							CurrentTextEngine.PerformPaste (GdkExtensions.GetDefaultClipboard ()).Wait ();
						} else if (e.IsControlPressed) {
							CurrentTextEngine.PerformCopy (GdkExtensions.GetDefaultClipboard ());
						}
						break;
					default:
						if (e.IsControlPressed) {
							if (e.Key.Value == Gdk.Constants.KEY_z) {
								//Ctrl + Z for undo while editing.
								OnHandleUndo (document);

								if (workspace.ActiveDocument.History.CanUndo)
									workspace.ActiveDocument.History.Undo ();

								return true;
							} else if (e.Key.Value == Gdk.Constants.KEY_i) {
								italic_btn.Toggle ();
								UpdateFont ();
							} else if (e.Key.Value == Gdk.Constants.KEY_b) {
								bold_btn.Toggle ();
								UpdateFont ();
							} else if (e.Key.Value == Gdk.Constants.KEY_u) {
								underscore_btn.Toggle ();
								UpdateFont ();
							} else if (e.Key.Value == Gdk.Constants.KEY_a) {
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
			}

			if (keyHandled)
				RedrawText (true, true);
		}

		return keyHandled;
	}

	protected override bool OnKeyUp (Document document, ToolKeyEventArgs e)
	{
		if (e.Key.IsControlKey () || e.IsControlPressed) {
			ctrl_key = false;

			UpdateMouseCursor (document);
		}
		return false;
	}

	private bool TryHandleChar (Gdk.Event eventKey)
	{
		// Try to handle it as a character
		if (im_context.FilterKeypress (eventKey))
			return true;

		// We didn't handle the key
		return false;
	}

	private void OnIMCommit (object o, Gtk.IMContext.CommitSignalArgs args)
	{
		try {
			// Reset the pre-edit string. Depending on the platform there might still be
			// a preedit-changed signal (setting it to the empty string) after the commit, rather than before.
			UpdatePreeditString (string.Empty, redraw: false);

			CurrentTextEngine.InsertText (args.Str);
			RedrawText (true, true);
		} finally {
			im_context.Reset ();
		}
	}

	private void OnPreeditStart (object o, EventArgs args)
	{
		// Initialize to empty string (null means pre-editing is inactive).
		preedit_string = string.Empty;
	}

	private void OnPreeditEnd (object o, EventArgs args)
	{
		// Reset to indicate that pre-editing is done. There should have previously been
		// a preedit-changed signal to erase the last preedited string.
		preedit_string = null;
	}

	private void OnPreeditChanged (object o, EventArgs args)
	{
		// TODO - use the Pango.AttrList argument to better visualize the pre-edited text vs the regular text.
		im_context.GetPreeditString (out string updated_str, out _, out _);
		UpdatePreeditString (updated_str, redraw: true);
	}

	private void UpdatePreeditString (string updated, bool redraw)
	{
		// Remove the previous preedit string.
		for (int i = 0; i < preedit_string?.Length; ++i)
			CurrentTextEngine.PerformBackspace ();

		// Insert the new string.
		preedit_string = updated;
		CurrentTextEngine.InsertText (preedit_string);

		RedrawText (true, true);
	}

	#endregion

	#region Start/Stop Editing

	private void StartEditing ()
	{
		// Ensure we have an event handler added to finalize re-editable text for the document if the layer is cloned.
		workspace.ActiveDocument.LayerCloned -= FinalizeText;
		workspace.ActiveDocument.LayerCloned += FinalizeText;

		is_editing = true;

		im_context.SetClientWidget (workspace.ActiveWorkspace.Canvas);

		selection ??= workspace.ActiveDocument.Selection.Clone ();

		//Start ignoring any Surface.Clone calls from this point on (so that it doesn't start to loop).
		ignore_clone_finalizations = true;

		//Store the previous state of the current UserLayer's and TextLayer's ImageSurfaces.
		user_undo_surface = workspace.ActiveDocument.Layers.CurrentUserLayer.Surface.Clone ();
		text_undo_surface = workspace.ActiveDocument.Layers.CurrentUserLayer.TextLayer.Layer.Surface.Clone ();

		//Store the previous state of the Text Engine.
		undo_engine = CurrentTextEngine.Clone ();

		//Update Text Engine to use current colors of color palette
		UpdateTextEngineColor ();

		//Stop ignoring any Surface.Clone calls from this point on.
		ignore_clone_finalizations = false;
	}

	private void StopEditing (bool finalize)
	{
		im_context.SetClientWidget (null);

		if (!workspace.HasOpenDocuments)
			return;

		if (!is_editing)
			return;

		is_editing = false;

		//Make sure that neither undo surface is null, the user is editing, and there are uncommitted changes.
		if (text_undo_surface != null && user_undo_surface != null && CurrentTextEngine.State == TextMode.Uncommitted) {
			Document doc = workspace.ActiveDocument;

			RedrawText (false, true);

			//Start ignoring any Surface.Clone calls from this point on (so that it doesn't start to loop).
			ignore_clone_finalizations = true;

			//Create a new TextHistoryItem so that the committing of text can be undone.
			doc.History.PushNewItem (
				new TextHistoryItem (
					Icon,
					Name,
					text_undo_surface.Clone (),
					user_undo_surface.Clone (),
					undo_engine!.Clone (), // NRT - Set in StartEditing
					doc.Layers.CurrentUserLayer
				)
			);

			//Stop ignoring any Surface.Clone calls from this point on.
			ignore_clone_finalizations = false;

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
		workspace.ActiveDocument.Layers.CurrentUserLayer.TextLayer.Layer.Surface.Clear ();

		//Redraw the previous text boundary.
		InflateAndInvalidate (workspace.ActiveDocument.Layers.CurrentUserLayer.PreviousTextBounds);
	}

	/// <summary>
	/// Draws the text.
	/// </summary>
	/// <param name="showCursor">Whether or not to show the mouse cursor in the drawing.</param>
	/// <param name="useTextLayer">Whether or not to use the TextLayer (as opposed to the Userlayer).</param>
	private void RedrawText (bool showCursor, bool useTextLayer)
	{
		RectangleI r = CurrentTextLayout.GetLayoutBounds ();
		r = r.Inflated (10 + OutlineWidth, 10 + OutlineWidth);
		InflateAndInvalidate (r);
		CurrentTextBounds = r;

		RectangleI cursorBounds = RectangleI.Zero;

		Cairo.ImageSurface surf;

		if (!useTextLayer) {
			//Draw text on the current UserLayer's surface as finalized text.
			surf = workspace.ActiveDocument.Layers.CurrentUserLayer.Surface;
		} else {
			//Draw text on the current UserLayer's TextLayer's surface as re-editable text.
			surf = workspace.ActiveDocument.Layers.CurrentUserLayer.TextLayer.Layer.Surface;

			ClearTextLayer ();
		}

		using Cairo.Context g = new (surf);

		var options = new Cairo.FontOptions ();

		if (UseAntialiasing) {
			// Adjusts antialiasing JUST for the outline brush
			g.Antialias = Cairo.Antialias.Gray;
			// Adjusts antialiasing for PangoCairo's text draw function
			options.Antialias = Antialias.Gray;
		} else {
			g.Antialias = Cairo.Antialias.None;
			options.Antialias = Antialias.None;
		}

		g.Save ();
		PangoCairo.Functions.ContextSetFontOptions (PintaCore.Chrome.MainWindow.GetPangoContext (), options);


		// Show selection if on text layer
		if (useTextLayer) {
			// Selected Text
			Cairo.Color c = new Cairo.Color (0.7, 0.8, 0.9, 0.5);
			foreach (RectangleI rect in CurrentTextLayout.GetSelectionRectangles ())
				g.FillRectangle (rect.ToDouble (), c);
		}

		selection?.Clip (g);

		g.MoveTo (CurrentTextEngine.Origin.X, CurrentTextEngine.Origin.Y);

		g.SetSourceColor (CurrentTextEngine.PrimaryColor);

		//Fill in background
		if (BackgroundFill) {
			using Cairo.Context g2 = new (surf);
			selection?.Clip (g2);

			g2.FillRectangle (CurrentTextLayout.GetLayoutBounds ().ToDouble (), CurrentTextEngine.SecondaryColor);
		}

		// Draw the text
		if (FillText)
			PangoCairo.Functions.ShowLayout (g, CurrentTextLayout.Layout);

		if (FillText && StrokeText) {
			g.SetSourceColor (CurrentTextEngine.SecondaryColor);
			g.LineWidth = OutlineWidth;

			PangoCairo.Functions.LayoutPath (g, CurrentTextLayout.Layout);
			g.Stroke ();
		} else if (StrokeText) {
			g.SetSourceColor (CurrentTextEngine.PrimaryColor);
			g.LineWidth = OutlineWidth;

			PangoCairo.Functions.LayoutPath (g, CurrentTextLayout.Layout);
			g.Stroke ();
		}

		if (showCursor) {

			var loc = CurrentTextLayout.GetCursorLocation ();
			var color = CurrentTextEngine.PrimaryColor;

			g.DrawLine (
				new PointD (loc.X, loc.Y),
				new PointD (loc.X, loc.Y + loc.Height),
				color, 1);

			cursorBounds = loc;
			cursorBounds = cursorBounds.Inflated (2, 10);
		}

		g.Restore ();


		if (useTextLayer && (is_editing || ctrl_key) && !CurrentTextEngine.IsEmpty ()) {
			//Draw the text edit rectangle.

			g.Save ();

			g.Translate (.5, .5);

			g.AppendPath (g.CreateRectanglePath (CurrentTextBounds.ToDouble ()));

			g.LineWidth = 1;

			g.SetSourceColor (new Cairo.Color (1, 1, 1));
			g.StrokePreserve ();

			g.SetDash ([2, 4], 0);
			g.SetSourceColor (new Cairo.Color (1, .1, .2));

			g.Stroke ();

			g.Restore ();
		}

		InflateAndInvalidate (workspace.ActiveDocument.Layers.CurrentUserLayer.PreviousTextBounds);
		workspace.Invalidate (old_cursor_bounds);
		InflateAndInvalidate (r);
		workspace.Invalidate (cursorBounds);

		old_cursor_bounds = cursorBounds;
	}

	/// <summary>
	/// Finalize re-editable text (if applicable).
	/// </summary>
	public void FinalizeText ()
	{
		//If this is true, don't finalize any text - this is used to prevent the code from looping recursively.
		if (ignore_clone_finalizations)
			return;

		//Only bother finalizing text if editing.
		if (CurrentTextEngine.State == TextMode.Unchanged)
			return;

		//Start ignoring any Surface.Clone calls from this point on (so that it doesn't start to loop).
		ignore_clone_finalizations = true;
		Document doc = workspace.ActiveDocument;

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
		CurrentTextBounds = RectangleI.Zero;

		//Create a new TextHistoryItem so that the finalization of the text can be undone. Construct
		//it on the spot so that it is more memory efficient if the changes are small.
		TextHistoryItem hist = new (
			Icon,
			FinalizeName,
			oldTextSurface,
			oldUserSurface,
			oldTextEngine,
			doc.Layers.CurrentUserLayer);

		//Add the new TextHistoryItem.
		doc.History.PushNewItem (hist);

		//Stop ignoring any Surface.Clone calls from this point on.
		ignore_clone_finalizations = false;

		//Now that the text has been finalized, change its state.
		CurrentTextEngine.State = TextMode.Unchanged;

		selection = null;
	}

	private void InflateAndInvalidate (in RectangleI passedRectangle)
	{
		//Create a new instance to preserve the passed Rectangle.
		RectangleI r = new (
			passedRectangle.Location,
			passedRectangle.Size);

		r = r.Inflated (2, 2);

		workspace.Invalidate (r);
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

	protected override async Task<bool> OnHandlePaste (Document document, Gdk.Clipboard cb)
	{
		if (!is_editing)
			return false;

		if (await CurrentTextEngine.PerformPaste (cb)) {
			RedrawText (true, true);
			return true;
		}

		return false;
	}

	protected override bool OnHandleCopy (Document document, Gdk.Clipboard cb)
	{
		if (!is_editing)
			return false;

		CurrentTextEngine.PerformCopy (cb);
		return true;
	}

	protected override bool OnHandleCut (Document document, Gdk.Clipboard cb)
	{
		if (!is_editing)
			return false;

		CurrentTextEngine.PerformCut (cb);
		RedrawText (true, true);
		return true;
	}

	#endregion
}
