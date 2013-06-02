// 
// FileActionHandler.cs
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
using Pinta.Actions;
using Pinta.Core;

namespace Pinta
{
	public class ActionHandlers
	{
		private List<IActionHandler> action_handlers = new List<IActionHandler> ();

		public ActionHandlers ()
		{
			// File
			action_handlers.Add (new NewDocumentAction ());
			action_handlers.Add (new NewScreenshotAction ());
			action_handlers.Add (new OpenDocumentAction ());
			action_handlers.Add (new OpenRecentAction ());
			action_handlers.Add (new SaveDocumentAction ());
			action_handlers.Add (new SaveDocumentAsAction ());
			action_handlers.Add (new PrintDocumentAction ());
			action_handlers.Add (new CloseDocumentAction ());
			action_handlers.Add (new ExitProgramAction ());
			action_handlers.Add (new ModifyCompressionAction ());
			action_handlers.Add (new SaveDocumentImplmentationAction ());

			// Edit
			action_handlers.Add (new PasteAction ());
			action_handlers.Add (new PasteIntoNewLayerAction ());
			action_handlers.Add (new PasteIntoNewImageAction ());
			action_handlers.Add (new ResizePaletteAction ());
			action_handlers.Add (new AddinManagerAction ());

			// Image
			action_handlers.Add (new ResizeImageAction ());
			action_handlers.Add (new ResizeCanvasAction ());

			// Layers
			action_handlers.Add (new LayerPropertiesAction ());
			action_handlers.Add (new RotateZoomLayerAction ());

			// View
			action_handlers.Add (new ToolBarToggledAction ());

			// Window
			action_handlers.Add (new CloseAllDocumentsAction ());
			action_handlers.Add (new SaveAllDocumentsAction ());

			// Help
			action_handlers.Add (new AboutDialogAction ());

			// Initialize each action handler
			foreach (var action in action_handlers)
				action.Initialize ();

			// We need to toggle actions active/inactive
			// when there isn't an open document
			PintaCore.Workspace.DocumentCreated += Workspace_DocumentCreated;
			PintaCore.Workspace.DocumentClosed += Workspace_DocumentClosed;
		}

		private void Workspace_DocumentClosed (object sender, DocumentEventArgs e)
		{
			PintaCore.Actions.Window.RemoveDocument (e.Document);

			if (!PintaCore.Workspace.HasOpenDocuments) {
				ToggleActions (false);
			}
		}

		private void Workspace_DocumentCreated (object sender, DocumentEventArgs e)
		{
			PintaCore.Actions.Window.AddDocument (e.Document);

			ToggleActions (true);
		}

		private static void ToggleActions (bool enable)
		{
			PintaCore.Actions.File.Close.Sensitive = enable;
			PintaCore.Actions.File.Save.Sensitive = enable;
			PintaCore.Actions.File.SaveAs.Sensitive = enable;
			PintaCore.Actions.File.Print.Sensitive = enable;
			PintaCore.Actions.Edit.Copy.Sensitive = enable;
			PintaCore.Actions.Edit.CopyMerged.Sensitive = enable;
			PintaCore.Actions.Edit.Cut.Sensitive = enable;
			PintaCore.Actions.Edit.PasteIntoNewLayer.Sensitive = enable;
			PintaCore.Actions.Edit.EraseSelection.Sensitive = enable;
			PintaCore.Actions.Edit.FillSelection.Sensitive = enable;
			PintaCore.Actions.Edit.InvertSelection.Sensitive = enable;
			PintaCore.Actions.Edit.SelectAll.Sensitive = enable;
			PintaCore.Actions.Edit.Deselect.Sensitive = enable;

			PintaCore.Actions.View.ActualSize.Sensitive = enable;
			PintaCore.Actions.View.ZoomIn.Sensitive = enable;
			PintaCore.Actions.View.ZoomOut.Sensitive = enable;
			PintaCore.Actions.View.ZoomToSelection.Sensitive = enable;
			PintaCore.Actions.View.ZoomToWindow.Sensitive = enable;
			PintaCore.Actions.View.ZoomComboBox.Sensitive = enable;

			PintaCore.Actions.Image.CropToSelection.Sensitive = enable;
			PintaCore.Actions.Image.AutoCrop.Sensitive = enable;
			PintaCore.Actions.Image.CanvasSize.Sensitive = enable;
			PintaCore.Actions.Image.Resize.Sensitive = enable;
			PintaCore.Actions.Image.FlipHorizontal.Sensitive = enable;
			PintaCore.Actions.Image.FlipVertical.Sensitive = enable;
			PintaCore.Actions.Image.Rotate180.Sensitive = enable;
			PintaCore.Actions.Image.RotateCCW.Sensitive = enable;
			PintaCore.Actions.Image.RotateCW.Sensitive = enable;

			PintaCore.Actions.Layers.AddNewLayer.Sensitive = enable;
			PintaCore.Actions.Layers.DuplicateLayer.Sensitive = enable;
			PintaCore.Actions.Layers.FlipHorizontal.Sensitive = enable;
			PintaCore.Actions.Layers.FlipVertical.Sensitive = enable;
			PintaCore.Actions.Layers.ImportFromFile.Sensitive = enable;
			PintaCore.Actions.Layers.Properties.Sensitive = enable;
			PintaCore.Actions.Layers.RotateZoom.Sensitive = enable;

			PintaCore.Actions.Adjustments.ToggleActionsSensitive (enable);
			PintaCore.Actions.Effects.ToggleActionsSensitive (enable);

			PintaCore.Actions.Window.SaveAll.Sensitive = enable;
			PintaCore.Actions.Window.CloseAll.Sensitive = enable;
		}
	}
}

