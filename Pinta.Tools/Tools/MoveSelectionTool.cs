//
// MoveSelectionTool.cs
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
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class MoveSelectionTool : BaseTransformTool
{
	private SelectionHistoryItem? hist;
	private DocumentSelection? original_selection;

	private readonly SystemManager system_manager;
	private readonly IWorkspaceService workspace;
	public MoveSelectionTool (IServiceProvider services) : base (services)
	{
		system_manager = services.GetService<SystemManager> ();
		workspace = services.GetService<IWorkspaceService> ();
	}

	public override string Name => Translations.GetString ("Move Selection");
	public override string Icon => Pinta.Resources.Icons.ToolMoveSelection;
	// Translators: {0} is 'Ctrl', or a platform-specific key such as 'Command' on macOS.
	public override string StatusBarText => Translations.GetString (
		"Left click and drag the selection to move selection outline." +
		"\nHold {0} to scale instead of move." +
		"\nRight click and drag the selection to rotate selection outline." +
		"\nHold Shift to rotate in steps." +
		"\nUse arrow keys to move selection outline by a single pixel.",
		system_manager.CtrlLabel ());

	public override Gdk.Cursor DefaultCursor => Gdk.Cursor.NewFromTexture (Resources.GetIcon (Pinta.Resources.Icons.ToolMoveSelection), 0, 0, null);
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_M);
	public override int Priority => 7;
	public override bool IsSelectionTool => true;

	protected override RectangleD GetSourceRectangle (Document document)
		=> document.Selection.SelectionPath.GetBounds ().ToDouble ();

	protected override void OnStartTransform (Document document)
	{
		base.OnStartTransform (document);

		original_selection = document.Selection.Clone ();

		hist = new SelectionHistoryItem (workspace, Icon, Name);
		hist.TakeSnapshot ();
	}

	protected override void OnUpdateTransform (Document document, Matrix transform)
	{
		base.OnUpdateTransform (document, transform);

		// Should never be null, set in OnStartTransform
		if (original_selection is null)
			return;

		document.Selection = original_selection.Transform (transform);
		document.Selection.Visible = true;

		document.Workspace.Invalidate ();
	}

	protected override void OnFinishTransform (Document document, Matrix transform)
	{
		base.OnFinishTransform (document, transform);

		// Also transform the base selection used for the various select modes.
		var prev_selection = document.PreviousSelection;
		document.PreviousSelection = prev_selection.Transform (transform);

		if (hist != null)
			document.History.PushNewItem (hist);

		hist = null;

		original_selection = null;
	}
}
