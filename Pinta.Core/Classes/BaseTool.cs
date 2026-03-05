//
// BaseTool.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2010 Jonathan Pobst
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gdk;
using Gtk;

namespace Pinta.Core;

[Mono.Addins.TypeExtensionPoint]
public abstract class BaseTool
{
	private readonly IToolService tools;
	private readonly IWorkspaceService workspace;

	protected IResourceService Resources { get; }
	protected ISettingsService Settings { get; }

	public const int DEFAULT_BRUSH_WIDTH = 2;

	protected BaseTool (IServiceProvider services)
	{
		Resources = services.GetService<IResourceService> ();
		Settings = services.GetService<ISettingsService> ();

		tools = services.GetService<IToolService> ();
		workspace = services.GetService<IWorkspaceService> ();

		CurrentCursor = DefaultCursor;

		// Update cursor when active document changes
		workspace.ActiveDocumentChanged += (_, _) => {
			if (tools.CurrentTool == this)
				SetCursor (CurrentCursor);
		};

		// Give tools a chance to save their settings on application quit
		Settings.SaveSettingsBeforeQuit += (_, _)
			=> OnSaveSettings (Settings);
	}

	/// <summary>
	/// The localized name of the tool.
	/// </summary>
	public abstract string Name { get; }

	/// <summary>
	/// The tool's icon which is used in the toolbox.
	/// </summary>
	public abstract string Icon { get; }

	/// <summary>
	/// Localized help text shown to the user on how to use the tool.
	/// </summary>
	public virtual string StatusBarText
		=> string.Empty;

	/// <summary>
	/// The default cursor used by the tool. Return 'null' for the default pointer.
	/// </summary>
	public virtual Cursor? DefaultCursor
		=> null;

	/// <summary>
	/// The current cursor for this tool. Return 'null' for the default pointer.
	/// </summary>
	public Cursor? CurrentCursor { get; private set; }

	/// <summary>
	/// Specifies whether this application needs to update this tool's
	/// cursor after a zoom operation.
	/// </summary>
	public virtual bool CursorChangesOnZoom
		=> false;

	/// <summary>
	/// Specifies whether the tool manipulates selections.
	/// This controls whether the selection is drawn with a filled color, rather than only
	/// showing the selection outline.
	/// </summary>
	public virtual bool IsSelectionTool
		=> false;

	/// <summary>
	/// Whether or not the tool is an editable ShapeTool.
	/// </summary>
	public virtual bool IsEditableShapeTool
		=> false;

	/// <summary>
	/// A list of handles that should be drawn on the canvas window.
	/// </summary>
	public virtual IEnumerable<IToolHandle> Handles
		=> [];

	/// <summary>
	/// The shortcut key used to activate this tool in the toolbox.
	/// Return Gdk.Key.Invalid for no shortcut key.
	/// </summary>
	public virtual Gdk.Key ShortcutKey
		=> Gdk.Key.Invalid;

	/// <summary>
	/// Affects the order of the tool in the toolbox. Lower numbers will appear first.
	/// </summary>
	public virtual int Priority
		=> 75;

	/// <summary>
	/// Specifies if the Antialiasing toolbar button should be shown for this tool.
	/// </summary>
	protected virtual bool ShowAntialiasingButton
		=> false;

	/// <summary>
	/// Specifies if the Alpha Blending toolbar button should be shown for this tool.
	/// </summary>
	protected virtual bool ShowAlphaBlendingButton
		=> false;

	/// <summary>
	/// Specifies if the tool should use anti-aliasing.
	/// </summary>
	public virtual bool UseAntialiasing {
		get => ShowAntialiasingButton && AntialiasingDropDown.SelectedItem.GetTagOrDefault (true);
		set {
			if (!ShowAntialiasingButton)
				return;

			AntialiasingDropDown.SelectedItem = AntialiasingDropDown.Items.First (i => i.Tag is bool b && b == value);
		}
	}

	/// <summary>
	/// Specifies if the tool should use alpha-blending.
	/// </summary>
	public virtual bool UseAlphaBlending {
		get => ShowAlphaBlendingButton && AlphaBlendingDropDown.SelectedItem.GetTagOrDefault (true);
		set {
			if (!ShowAlphaBlendingButton)
				return;

			AlphaBlendingDropDown.SelectedItem = AlphaBlendingDropDown.Items.First (i => i.Tag is bool b && b == value);
		}
	}

	/// <summary>
	/// Called when the tool is selected from the toolbox.
	/// </summary>
	protected virtual void OnActivated (Document? document)
	{
	}

	/// <summary>
	/// Called after a history item is redone.
	/// </summary>
	protected virtual void OnAfterRedo (Document document)
	{
	}

	/// <summary>
	/// Called after the active document is saved.
	/// </summary>
	protected virtual void OnAfterSave (Document document)
	{
	}

	/// <summary>
	/// Called after a history item is undone.
	/// </summary>
	protected virtual void OnAfterUndo (Document document)
	{
	}

	/// <summary>
	/// Called when the tool needs to add its items to the Tool toolbar.
	/// </summary>
	protected virtual void OnBuildToolBar (Box toolbar)
	{
	}

	/// <summary>
	/// Called whenever another component is activated and this tool should
	/// commit any work that was in a temporary state.
	/// </summary>
	protected virtual void OnCommit (Document? document)
	{
	}

	/// <summary>
	/// Called when the tool is deselected from the toolbox.
	/// </summary>
	protected virtual void OnDeactivated (Document? document, BaseTool? newTool)
	{
	}

	/// <summary>
	/// Called to give the tool an opportunity to consume a Copy clipboard operation.
	/// Return 'true' if the Copy is handled, or 'false' to allow other
	/// components to handle it.
	/// </summary>
	protected virtual bool OnHandleCopy (Document document, Clipboard cb)
		=> false;

	/// <summary>
	/// Called to give the tool an opportunity to consume a Cut clipboard operation.
	/// Return 'true' if the Cut is handled, or 'false' to allow other
	/// components to handle it.
	/// </summary>
	protected virtual bool OnHandleCut (Document document, Clipboard cb)
		=> false;

	/// <summary>
	/// Called to give the tool an opportunity to consume a Paste clipboard operation.
	/// Return 'true' if the Cut is handled, or 'false' to allow other
	/// components to handle it.
	/// </summary>
	protected virtual Task<bool> OnHandlePaste (Document document, Clipboard cb)
		=> Task.FromResult (false);

	/// <summary>
	/// Called to give the tool an opportunity to consume a Redo operation.
	/// Return 'true' if the Redo is handled, or 'false' to allow other
	/// components to handle it.
	/// </summary>
	protected virtual bool OnHandleRedo (Document document)
		=> false;

	/// <summary>
	/// Called to give the tool an opportunity to consume an Undo operation.
	/// Return 'true' if the Undo is handled, or 'false' to allow other
	/// components to handle it.
	/// </summary>
	protected virtual bool OnHandleUndo (Document document)
		=> false;

	/// <summary>
	/// Called when a key is pressed. Return 'true' if the key is handled, or
	/// 'false' to allow other components to handle it.
	/// </summary>
	protected virtual bool OnKeyDown (Document document, ToolKeyEventArgs e)
		=> false;

	/// <summary>
	/// Called when a key is released. Return 'true' if the key is handled, or
	/// 'false' to allow other components to handle it.
	/// </summary>
	protected virtual bool OnKeyUp (Document document, ToolKeyEventArgs e)
		=> false;

	/// <summary>
	/// Called when a mouse button is pressed.
	/// </summary>
	protected virtual void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
	}

	/// <summary>
	/// Called when the mouse is moved.
	/// </summary>
	protected virtual void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
	}

	/// <summary>
	/// Called when a mouse button is released.
	/// </summary>
	protected virtual void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
	}

	/// <summary>
	/// Called before the application exist to give tool a chance to save settings.
	/// </summary>
	protected virtual void OnSaveSettings (ISettingsService settings)
	{
		if (alphablending_button is not null)
			settings.PutSetting (SettingNames.ToolAlphaBlend (this), alphablending_button.SelectedIndex);

		if (antialiasing_button is not null)
			settings.PutSetting (SettingNames.ToolAntialias (this), antialiasing_button.SelectedIndex);
	}

	/// <summary>
	/// Called when the antialias setting is changed.
	/// </summary>
	protected virtual void OnAntialiasingChanged ()
	{

	}

	/// <summary>
	/// Tool should call this in order to change the application cursor.
	/// Pass 'null' to reset the cursor back to the default.
	/// </summary>
	public void SetCursor (Cursor? cursor)
	{
		CurrentCursor = cursor;

		if (workspace.HasOpenDocuments)
			workspace.ActiveWorkspace.Canvas.Cursor = cursor;
	}

	#region Toolbar
	private ToolBoxButton? tool_item;
	private ToolBarDropDownButton? antialiasing_button;
	private ToolBarDropDownButton? alphablending_button;
	private Separator? separator;

	public virtual ToolBoxButton ToolItem => tool_item ??= CreateToolButton ();

	private Separator Separator => separator ??= GtkExtensions.CreateToolBarSeparator ();

	private ToolBarDropDownButton AlphaBlendingDropDown {
		get {
			if (alphablending_button is null) {
				alphablending_button = new ToolBarDropDownButton ();

				alphablending_button.AddItem (Translations.GetString ("Normal Blending"), Pinta.Resources.Icons.BlendingNormal, true);
				alphablending_button.AddItem (Translations.GetString ("Overwrite"), Pinta.Resources.Icons.BlendingOverwrite, false);

				alphablending_button.SelectedIndex = Settings.GetSetting (
					SettingNames.ToolAlphaBlend (this),
					0);
			}

			return alphablending_button;
		}
	}

	private ToolBarDropDownButton AntialiasingDropDown {
		get {
			if (antialiasing_button is null) {
				antialiasing_button = new ToolBarDropDownButton ();

				antialiasing_button.AddItem (Translations.GetString ("Antialiasing On"), Pinta.Resources.Icons.AntiAliasingEnabled, true);
				antialiasing_button.AddItem (Translations.GetString ("Antialiasing Off"), Pinta.Resources.Icons.AntiAliasingDisabled, false);

				antialiasing_button.SelectedIndex = Settings.GetSetting (
					SettingNames.ToolAntialias (this),
					0);

				antialiasing_button.SelectedItemChanged += (object? sender, EventArgs e) => {
					OnAntialiasingChanged ();
				};
			}

			return antialiasing_button;
		}
	}

	private ToolBoxButton CreateToolButton ()
		=> new (this);

	#endregion

	#region Event Invokers
	internal void DoActivated (Document? document)
	{
		SetCursor (DefaultCursor);
		OnActivated (document);
	}

	internal void DoAfterRedo (Document document) => OnAfterRedo (document);

	internal void DoAfterSave (Document document) => OnAfterSave (document);

	internal void DoAfterUndo (Document document) => OnAfterUndo (document);

	internal void DoBuildToolBar (Box toolbar)
	{
		OnBuildToolBar (toolbar);

		// Add alpha-blending and anti-aliasing dropdowns if needed
		if (ShowAlphaBlendingButton || ShowAntialiasingButton)
			toolbar.Append (Separator);

		if (ShowAntialiasingButton)
			toolbar.Append (AntialiasingDropDown);
		if (ShowAlphaBlendingButton)
			toolbar.Append (AlphaBlendingDropDown);
	}

	internal void DoCommit (Document? document) => OnCommit (document);

	internal void DoDeactivated (Document? document, BaseTool? newTool)
	{
		SetCursor (null);
		OnDeactivated (document, newTool);
	}

	internal bool DoHandleCopy (Document document, Clipboard clipboard) => OnHandleCopy (document, clipboard);

	internal bool DoHandleCut (Document document, Clipboard clipboard) => OnHandleCut (document, clipboard);

	internal Task<bool> DoHandlePaste (Document document, Clipboard clipboard) => OnHandlePaste (document, clipboard);

	internal bool DoHandleRedo (Document document) => OnHandleRedo (document);

	internal bool DoHandleUndo (Document document) => OnHandleUndo (document);

	internal bool DoKeyDown (Document document, ToolKeyEventArgs args) => OnKeyDown (document, args);

	internal bool DoKeyUp (Document document, ToolKeyEventArgs args) => OnKeyUp (document, args);

	internal void DoMouseDown (Document document, ToolMouseEventArgs args) => OnMouseDown (document, args);
	internal void DoMouseMove (Document document, ToolMouseEventArgs args) => OnMouseMove (document, args);
	internal void DoMouseUp (Document document, ToolMouseEventArgs args) => OnMouseUp (document, args);
	#endregion
}
