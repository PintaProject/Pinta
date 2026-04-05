//
// ShapeTool.cs
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
using Pinta.Core;

namespace Pinta.Tools;

public abstract class ShapeTool : BaseTool
{
	public BaseEditEngine edit_engine;

	private readonly SystemManager system_manager;
	public ShapeTool (IServiceProvider services) : base (services)
	{
		system_manager = services.GetService<SystemManager> ();
	}

	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_O);
	protected override bool ShowAntialiasingButton => true;
	public virtual BaseEditEngine.ShapeTypes ShapeType => BaseEditEngine.ShapeTypes.ClosedLineCurveSeries;
	public override bool IsEditableShapeTool => true;

	public override string StatusBarText =>
			// Translators: {0} is 'Ctrl', or a platform-specific key such as 'Command' on macOS.
			Translations.GetString ("Left click to draw a shape with the primary color." +
			    "\nLeft click on a shape to add a control point." +
			    "\nLeft click on a control point and drag to move it." +
			    "\nRight click on a control point and drag to change its tension." +
			    "\nHold Shift to snap to angles." +
			    "\nUse arrow keys to move the selected control point." +
			    "\nPress {0} + left/right arrows to select control points by order." +
			    "\nPress Delete to delete the selected control point." +
			    "\nPress Space to add a new control point at the mouse position." +
			    "\nHold {0} while pressing Space to create the control point at the exact same position." +
			    "\nHold {0} while left clicking on a control point to create a new shape at the exact same position." +
			    "\nPress Enter to finalize the shape.", system_manager.CtrlLabel ());

	protected abstract BaseEditEngine CreateEditEngine ();

	protected override void OnBuildToolBar (Gtk.Box tb)
	{
		base.OnBuildToolBar (tb);

		edit_engine.HandleBuildToolBar (tb, Settings, GetType ().Name.ToLowerInvariant ());
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		edit_engine.HandleMouseDown (document, e);
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		edit_engine.HandleMouseUp (document, e);
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		edit_engine.HandleMouseMove (document, e);
	}

	protected override void OnActivated (Document? document)
	{
		edit_engine.HandleActivated ();

		base.OnActivated (document);
	}

	protected override void OnDeactivated (Document? document, BaseTool? newTool)
	{
		edit_engine.HandleDeactivated (newTool);

		base.OnDeactivated (document, newTool);
	}

	protected override void OnAfterSave (Document document)
	{
		edit_engine.HandleAfterSave ();

		base.OnAfterSave (document);
	}

	protected override void OnCommit (Document? document)
	{
		edit_engine.HandleCommit ();

		base.OnCommit (document);
	}

	protected override bool OnKeyDown (Document document, ToolKeyEventArgs e)
	{
		if (edit_engine.HandleKeyDown (document, e))
			return true;

		return base.OnKeyDown (document, e);
	}

	protected override bool OnKeyUp (Document document, ToolKeyEventArgs e)
	{
		if (edit_engine.HandleKeyUp (document, e))
			return true;

		return base.OnKeyUp (document, e);
	}

	protected override bool OnHandleUndo (Document document)
	{
		if (!edit_engine.HandleBeforeUndo ())
			return base.OnHandleUndo (document);
		else
			return true;
	}

	protected override bool OnHandleRedo (Document document)
	{
		if (!edit_engine.HandleBeforeRedo ())
			return base.OnHandleRedo (document);
		else
			return true;
	}

	protected override void OnAfterUndo (Document document)
	{
		edit_engine.HandleAfterUndo ();

		base.OnAfterUndo (document);
	}

	protected override void OnAfterRedo (Document document)
	{
		edit_engine.HandleAfterRedo ();

		base.OnAfterRedo (document);
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		edit_engine.OnSaveSettings (settings, GetType ().Name.ToLowerInvariant ());
	}

	public override IEnumerable<IToolHandle> Handles => edit_engine.Handles;
}
