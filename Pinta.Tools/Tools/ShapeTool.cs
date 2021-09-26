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
using Pinta.Core;

namespace Pinta.Tools
{
	public abstract class ShapeTool : BaseTool
	{
		public BaseEditEngine EditEngine { get; }

		public ShapeTool (IServiceManager services) : base (services)
		{
			EditEngine = CreateEditEngine ();
		}

		public override Gdk.Key ShortcutKey => Gdk.Key.O;
		protected override bool ShowAntialiasingButton => true;
		public virtual BaseEditEngine.ShapeTypes ShapeType => BaseEditEngine.ShapeTypes.ClosedLineCurveSeries;
		public override bool IsEditableShapeTool => true;

		public override string StatusBarText {
			get {
				// Translators: {0} is 'Ctrl', or a platform-specific key such as 'Command' on macOS.
				return Translations.GetString ("Left click to draw a shape with the primary color." +
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
				    "\nHold {0} while clicking outside of the image bounds to create a new shape starting at the edge." +
				    "\nPress Enter to finalize the shape.", GtkExtensions.CtrlLabel ());
			}
		}

		protected abstract BaseEditEngine CreateEditEngine ();

		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			EditEngine.HandleBuildToolBar (tb, Settings, GetType ().Name.ToLowerInvariant ());
		}

		protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
		{
			EditEngine.HandleMouseDown (document, e);
		}

		protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
		{
			EditEngine.HandleMouseUp (document, e);
		}

		protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
		{
			EditEngine.HandleMouseMove (document, e);
		}

		protected override void OnActivated (Document? document)
		{
			EditEngine.HandleActivated ();

			base.OnActivated (document);
		}

		protected override void OnDeactivated (Document? document, BaseTool? newTool)
		{
			EditEngine.HandleDeactivated (newTool);

			base.OnDeactivated (document, newTool);
		}

		protected override void OnAfterSave (Document document)
		{
			EditEngine.HandleAfterSave ();

			base.OnAfterSave (document);
		}

		protected override void OnCommit (Document? document)
		{
			EditEngine.HandleCommit ();

			base.OnCommit (document);
		}

		protected override bool OnKeyDown (Document document, ToolKeyEventArgs e)
		{
			if (EditEngine.HandleKeyDown (document, e))
				return true;

			return base.OnKeyDown (document, e);
		}

		protected override bool OnKeyUp (Document document, ToolKeyEventArgs e)
		{
			if (EditEngine.HandleKeyUp (document, e))
				return true;

			return base.OnKeyUp (document, e);
		}

		protected override bool OnHandleUndo (Document document)
		{
			if (!EditEngine.HandleBeforeUndo ())
				return base.OnHandleUndo (document);
			else
				return true;
		}

		protected override bool OnHandleRedo (Document document)
		{
			if (!EditEngine.HandleBeforeRedo ())
				return base.OnHandleRedo (document);
			else
				return true;
		}

		protected override void OnAfterUndo (Document document)
		{
			EditEngine.HandleAfterUndo ();

			base.OnAfterUndo (document);
		}

		protected override void OnAfterRedo (Document document)
		{
			EditEngine.HandleAfterRedo ();

			base.OnAfterRedo (document);
		}

		protected override void OnSaveSettings (ISettingsService settings)
		{
			base.OnSaveSettings (settings);

			EditEngine.OnSaveSettings (settings, GetType ().Name.ToLowerInvariant ());
		}
	}
}
