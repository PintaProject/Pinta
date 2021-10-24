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

namespace Pinta.Tools
{
	public class MoveSelectionTool : BaseTransformTool
	{
		private SelectionHistoryItem? hist;
		private DocumentSelection? original_selection;

		public MoveSelectionTool (IServiceManager service) : base (service)
		{
		}

		public override string Name => Translations.GetString ("Move Selection");
		public override string Icon => Pinta.Resources.Icons.ToolMoveSelection;
		// Translators: {0} is 'Ctrl', or a platform-specific key such as 'Command' on macOS.
		public override string StatusBarText => Translations.GetString ("Left click and drag the selection to move selection outline. Hold {0} to scale instead of move. Right click and drag the selection to rotate selection outline. Hold Shift to rotate in steps. Use arrow keys to move selection outline by a single pixel.", GtkExtensions.CtrlLabel ());
		public override Gdk.Cursor DefaultCursor => new Gdk.Cursor (Gdk.Display.Default, Gtk.IconTheme.Default.LoadIcon (Pinta.Resources.Icons.ToolMoveSelection, 16), 0, 0);
		public override Gdk.Key ShortcutKey => Gdk.Key.M;
		public override int Priority => 7;

		protected override Rectangle GetSourceRectangle (Document document)
		{
			return document.Selection.SelectionPath.GetBounds ().ToCairoRectangle ();
		}

		protected override void OnStartTransform (Document document)
		{
			base.OnStartTransform (document);

			original_selection = document.Selection.Clone ();

			hist = new SelectionHistoryItem (Icon, Name);
			hist.TakeSnapshot ();
		}

		protected override void OnUpdateTransform (Document document, Matrix transform)
		{
			base.OnUpdateTransform (document, transform);

			// Should never be null, set in OnStartTransform
			if (original_selection is null)
				return;

			document.Selection.Dispose ();
			document.Selection = original_selection.Transform (transform);
			document.Selection.Visible = true;

			document.Workspace.Invalidate ();
		}

		protected override void OnFinishTransform (Document document, Matrix transform)
		{
			base.OnFinishTransform (document, transform);

			// Also transform the base selection used for the various select modes.
			using (var prev_selection = document.PreviousSelection)
				document.PreviousSelection = prev_selection.Transform (transform);

			if (hist != null)
				document.History.PushNewItem (hist);

			hist = null;

			original_selection?.Dispose ();
			original_selection = null;
		}
	}
}
