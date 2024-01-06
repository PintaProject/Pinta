// 
// ToolManager.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Gdk;
using Gtk;

namespace Pinta.Core;

public interface IToolService
{
	/// <summary>
	/// Adds a new tool to the tool box.
	/// </summary>
	void AddTool (BaseTool tool);

	/// <summary>
	/// Instructs the current tool to commit any work that is in a temporary state.
	/// </summary>
	void Commit ();

	/// <summary>
	/// Gets the currently selected tool.
	/// </summary>
	BaseTool? CurrentTool { get; }

	/// <summary>
	/// Performs the mouse down event for the currently selected tool.
	/// </summary>
	void DoMouseDown (Document document, ToolMouseEventArgs e);

	/// <summary>
	/// Gets the previously selected tool.
	/// </summary>
	BaseTool? PreviousTool { get; }

	/// <summary>
	/// Removes the first found tool of the specified type from tool box.
	/// </summary>
	void RemoveInstanceOfTool<T> () where T : BaseTool;

	/// <summary>
	/// Sets the current tool to the specified tool.
	/// </summary>
	void SetCurrentTool (BaseTool tool);

	/// <summary>
	/// Sets the current tool to the first tool with the specified tool type name, like
	/// 'PencilTool'. Returns a value indicating if tool was successfully changed.
	/// </summary>
	bool SetCurrentTool (string tool);

	/// <summary>
	/// Sets the current tool to the next tool with the specified shortcut.
	/// </summary>
	bool SetCurrentTool (Gdk.Key shortcut);
}

public sealed class ToolManager : IEnumerable<BaseTool>, IToolService
{
	private readonly SortedSet<BaseTool> tools = new (new ToolSorter ());

	private bool is_panning;
	private Cursor? stored_cursor;

	public event EventHandler<ToolEventArgs>? ToolAdded;
	public event EventHandler<ToolEventArgs>? ToolRemoved;

	public BaseTool? CurrentTool { get; private set; }

	public BaseTool? PreviousTool { get; private set; }

	public void AddTool (BaseTool tool)
	{
		tool.ToolItem.OnClicked += HandleToolBoxToolItemClicked;

		if (!tools.Add (tool))
			throw new Exception ("Attempted to add a duplicate tool");

		ToolAdded?.Invoke (this, new ToolEventArgs (tool));

		if (CurrentTool is null)
			SetCurrentTool (tool);
	}

	public void RemoveInstanceOfTool<T> () where T : BaseTool
	{
		var tool = tools.OfType<T> ().FirstOrDefault ();

		if (tool is null)
			return;

		tool.ToolItem.OnClicked -= HandleToolBoxToolItemClicked;
		tool.ToolItem.Active = false;
		tool.ToolItem.Sensitive = false;

		if (!tools.Remove (tool))
			throw new Exception ("Attempted to remove a tool that wasn't registered");

		// Are we trying to remove the current tool?
		if (CurrentTool == tool) {
			// Can we set it back to the previous tool?
			if (PreviousTool is not null && PreviousTool != CurrentTool)
				SetCurrentTool (PreviousTool);
			else if (tools.Any ())  // Any tool?
				SetCurrentTool (tools.First ());
			else {
				// There are no tools left.
				DeactivateTool (tool, null);
				PreviousTool = null;
				CurrentTool = null;
			}
		}

		ToolRemoved?.Invoke (this, new ToolEventArgs (tool));
	}

	private void HandleToolBoxToolItemClicked (object? sender, EventArgs e)
	{
		if (sender is not ToolBoxButton tb)
			return;

		var new_tool = tb.Tool;

		// Don't let the user unselect the current tool	
		if (CurrentTool != null && new_tool.GetType ().Name == CurrentTool.GetType ().Name) {
			if (PreviousTool != CurrentTool)
				tb.Active = true;

			return;
		}

		SetCurrentTool (new_tool);
	}

	private BaseTool? FindTool (string name)
	{
		return tools.FirstOrDefault (t => string.Compare (name, t.GetType ().Name, true) == 0);
	}

	public void Commit ()
	{
		CurrentTool?.DoCommit (PintaCore.Workspace.ActiveDocumentOrDefault);
	}

	public void SetCurrentTool (BaseTool tool)
	{
		// Bail if this is already the current tool
		if (CurrentTool == tool)
			return;

		// Unload previous tool if needed
		if (CurrentTool is not null) {
			PreviousTool = CurrentTool;
			DeactivateTool (PreviousTool, tool);
		}

		// Load new tool
		CurrentTool = tool;

		tool.ToolItem.Active = true;
		tool.DoActivated (PintaCore.Workspace.ActiveDocumentOrDefault);

		ToolImage.SetFromIconName (tool.Icon);

		PintaCore.Chrome.ToolToolBar.Append (ToolLabel);
		PintaCore.Chrome.ToolToolBar.Append (ToolImage);
		PintaCore.Chrome.ToolToolBar.Append (ToolSeparator);

		PintaCore.Chrome.ToolToolBar.Append (ToolWidgetsScroll);
		tool.DoBuildToolBar (ToolWidgetsBox);

		PintaCore.Workspace.Invalidate ();
		PintaCore.Chrome.SetStatusBarText ($" {tool.Name}: {tool.StatusBarText}");
	}

	public bool SetCurrentTool (string tool)
	{
		if (FindTool (tool) is BaseTool t) {
			SetCurrentTool (t);
			return true;
		}

		return false;
	}

	public bool SetCurrentTool (Gdk.Key shortcut)
	{
		if (FindNextTool (shortcut) is BaseTool tool) {
			SetCurrentTool (tool);
			return true;
		}

		return false;
	}

	private BaseTool? FindNextTool (Gdk.Key shortcut)
	{
		// Find all tools with this shortcut
		var shortcut_tools = tools.Where (t => t.ShortcutKey.ToUpper () == shortcut.ToUpper ()).ToList ();

		// No tools with this shortcut, bail
		if (!shortcut_tools.Any ())
			return null;

		// Only one option, return it
		if (shortcut_tools.Count == 1 || CurrentTool is null)
			return shortcut_tools.First ();

		// Get the tool after the currently selected tool
		var next_index = shortcut_tools.IndexOf (CurrentTool) + 1;

		// Wrap if we're past the final tool
		if (next_index >= shortcut_tools.Count)
			next_index = 0;
		return shortcut_tools[next_index];
	}

	private void DeactivateTool (BaseTool tool, BaseTool? newTool)
	{
		ToolWidgetsBox.RemoveAll ();
		PintaCore.Chrome.ToolToolBar.RemoveAll ();

		tool.DoDeactivated (PintaCore.Workspace.ActiveDocumentOrDefault, newTool);
		tool.ToolItem.Active = false;
	}

	public void DoMouseDown (Document document, ToolMouseEventArgs args)
	{
		if (!TryMouseDownPanOverride (document, args))
			CurrentTool?.DoMouseDown (document, args);
	}

	public void DoMouseMove (Document document, ToolMouseEventArgs args)
	{
		if (!TryMouseMovePanOverride (document, args))
			CurrentTool?.DoMouseMove (document, args);
	}

	public void DoMouseUp (Document document, ToolMouseEventArgs args)
	{
		if (!TryMouseUpPanOverride (document, args))
			CurrentTool?.DoMouseUp (document, args);
	}

	public bool DoKeyDown (Document document, ToolKeyEventArgs args) => CurrentTool?.DoKeyDown (document, args) ?? false;
	public bool DoKeyUp (Document document, ToolKeyEventArgs args) => CurrentTool?.DoKeyUp (document, args) ?? false;

	public void DoAfterSave (Document document) => CurrentTool?.DoAfterSave (document);
	public Task<bool> DoHandlePaste (Document document, Clipboard clipboard) => CurrentTool?.DoHandlePaste (document, clipboard) ?? Task.FromResult (false);

	public IEnumerator<BaseTool> GetEnumerator () => tools.GetEnumerator ();

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () => tools.GetEnumerator ();

	private bool TryMouseDownPanOverride (Document document, ToolMouseEventArgs args)
	{
		if (is_panning)
			return true;

		if (args.MouseButton == MouseButton.Middle && TryGetPanTool (out var pan)) {
			is_panning = true;
			stored_cursor = document.Workspace.Canvas.Cursor;
			document.Workspace.Canvas.Cursor = pan.DefaultCursor;
			pan.DoMouseDown (document, args);
			return true;
		}

		return false;
	}

	private bool TryMouseMovePanOverride (Document document, ToolMouseEventArgs args)
	{
		if (is_panning && TryGetPanTool (out var pan)) {
			pan.DoMouseMove (document, args);
			return true;
		}

		return false;
	}

	private bool TryMouseUpPanOverride (Document document, ToolMouseEventArgs args)
	{
		if (is_panning && TryGetPanTool (out var pan)) {
			// Ignore any mouse button releases that aren't Middle
			if (args.MouseButton != MouseButton.Middle)
				return true;

			is_panning = false;
			pan.DoMouseUp (document, args);
			document.Workspace.Canvas.Cursor = stored_cursor;
			return true;
		}

		return false;
	}

	private bool TryGetPanTool ([NotNullWhen (true)] out BaseTool? tool)
	{
		tool = FindTool ("PanTool");

		return tool is not null;
	}

	private sealed class ToolSorter : Comparer<BaseTool>
	{
		public override int Compare (BaseTool? x, BaseTool? y)
		{
			int result = (x?.Priority ?? 0) - (y?.Priority ?? 0);
			// If two tools have the same priority, sort by type name so that both tools can still
			// be inserted into the set.
			if (result == 0) {
				string x_type = x?.GetType ().AssemblyQualifiedName ?? string.Empty;
				string y_type = y?.GetType ().AssemblyQualifiedName ?? string.Empty;
				result = x_type.CompareTo (y_type);
			}

			return result;
		}
	}

	private Label? tool_label;
	private Image? tool_image;
	private Separator? tool_sep;
	private Box? tool_widgets_box;
	private ScrolledWindow? tool_widgets_scroll;

	private Label ToolLabel => tool_label ??= Label.New (string.Format (" {0}:  ", Translations.GetString ("Tool")));
	private Image ToolImage => tool_image ??= new Image ();
	private Separator ToolSeparator => tool_sep ??= GtkExtensions.CreateToolBarSeparator ();
	private Box ToolWidgetsBox => tool_widgets_box ??= Gtk.Box.New (Orientation.Horizontal, 0);
	// Scroll the toolbar contents if they are very long (e.g. the line/curve tool).
	private ScrolledWindow ToolWidgetsScroll => tool_widgets_scroll ??= new ScrolledWindow () {
		Child = ToolWidgetsBox,
		HscrollbarPolicy = PolicyType.Automatic,
		VscrollbarPolicy = PolicyType.Never,
		HasFrame = false,
		OverlayScrolling = true,
		WindowPlacement = CornerType.BottomRight,
		Hexpand = true,
		Halign = Align.Fill
	};
}
