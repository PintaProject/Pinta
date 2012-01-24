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
			action_handlers.Add (new CloseDocumentAction ());
			action_handlers.Add (new ExitProgramAction ());
			action_handlers.Add (new ModifyCompressionAction ());
			action_handlers.Add (new SaveDocumentImplmentationAction ());

			// Edit
			action_handlers.Add (new PasteIntoNewLayerAction ());
			action_handlers.Add (new PasteIntoNewImageAction ());
			action_handlers.Add (new ResizePaletteAction ());

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
			action_handlers.Add (new ExtensionManagerAction ());

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
				PintaCore.Actions.File.Close.Sensitive = false;
				PintaCore.Actions.File.Save.Sensitive = false;
				PintaCore.Actions.File.SaveAs.Sensitive = false;
				PintaCore.Actions.Edit.Copy.Sensitive = false;
				PintaCore.Actions.Edit.Cut.Sensitive = false;
				PintaCore.Actions.Edit.Paste.Sensitive = false;
				PintaCore.Actions.Edit.PasteIntoNewLayer.Sensitive = false;
				PintaCore.Actions.Edit.SelectAll.Sensitive = false;

				PintaCore.Actions.View.ActualSize.Sensitive = false;
				PintaCore.Actions.View.ZoomIn.Sensitive = false;
				PintaCore.Actions.View.ZoomOut.Sensitive = false;
				PintaCore.Actions.View.ZoomToSelection.Sensitive = false;
				PintaCore.Actions.View.ZoomToWindow.Sensitive = false;
				PintaCore.Actions.View.ZoomComboBox.Sensitive = false;

				PintaCore.Actions.Image.CropToSelection.Sensitive = false;
				PintaCore.Actions.Image.AutoCrop.Sensitive = false;
				PintaCore.Actions.Image.CanvasSize.Sensitive = false;
				PintaCore.Actions.Image.Resize.Sensitive = false;
				PintaCore.Actions.Image.FlipHorizontal.Sensitive = false;
				PintaCore.Actions.Image.FlipVertical.Sensitive = false;
				PintaCore.Actions.Image.Rotate180.Sensitive = false;
				PintaCore.Actions.Image.RotateCCW.Sensitive = false;
				PintaCore.Actions.Image.RotateCW.Sensitive = false;

				PintaCore.Actions.Layers.AddNewLayer.Sensitive = false;
				PintaCore.Actions.Layers.DuplicateLayer.Sensitive = false;
				PintaCore.Actions.Layers.FlipHorizontal.Sensitive = false;
				PintaCore.Actions.Layers.FlipVertical.Sensitive = false;
				PintaCore.Actions.Layers.ImportFromFile.Sensitive = false;
				PintaCore.Actions.Layers.Properties.Sensitive = false;
				PintaCore.Actions.Layers.RotateZoom.Sensitive = false;

				PintaCore.Actions.Adjustments.ToggleActionsSensitive (false);
				PintaCore.Actions.Effects.ToggleActionsSensitive (false);
			}
		}

		private void Workspace_DocumentCreated (object sender, DocumentEventArgs e)
		{
			PintaCore.Actions.Window.AddDocument (e.Document);

			PintaCore.Actions.File.Close.Sensitive = true;
			PintaCore.Actions.File.Save.Sensitive = true;
			PintaCore.Actions.File.SaveAs.Sensitive = true;
			PintaCore.Actions.Edit.Copy.Sensitive = true;
			PintaCore.Actions.Edit.Cut.Sensitive = true;
			PintaCore.Actions.Edit.Paste.Sensitive = true;
			PintaCore.Actions.Edit.PasteIntoNewLayer.Sensitive = true;
			PintaCore.Actions.Edit.SelectAll.Sensitive = true;

			PintaCore.Actions.View.ActualSize.Sensitive = true;
			PintaCore.Actions.View.ZoomIn.Sensitive = true;
			PintaCore.Actions.View.ZoomOut.Sensitive = true;
			PintaCore.Actions.View.ZoomToSelection.Sensitive = true;
			PintaCore.Actions.View.ZoomToWindow.Sensitive = true;
			PintaCore.Actions.View.ZoomComboBox.Sensitive = true;

			PintaCore.Actions.Image.AutoCrop.Sensitive = true;
			PintaCore.Actions.Image.CanvasSize.Sensitive = true;
			PintaCore.Actions.Image.Resize.Sensitive = true;
			PintaCore.Actions.Image.FlipHorizontal.Sensitive = true;
			PintaCore.Actions.Image.FlipVertical.Sensitive = true;
			PintaCore.Actions.Image.Rotate180.Sensitive = true;
			PintaCore.Actions.Image.RotateCCW.Sensitive = true;
			PintaCore.Actions.Image.RotateCW.Sensitive = true;

			PintaCore.Actions.Layers.AddNewLayer.Sensitive = true;
			PintaCore.Actions.Layers.DuplicateLayer.Sensitive = true;
			PintaCore.Actions.Layers.FlipHorizontal.Sensitive = true;
			PintaCore.Actions.Layers.FlipVertical.Sensitive = true;
			PintaCore.Actions.Layers.ImportFromFile.Sensitive = true;
			PintaCore.Actions.Layers.Properties.Sensitive = true;
			PintaCore.Actions.Layers.RotateZoom.Sensitive = true;

			PintaCore.Actions.Adjustments.ToggleActionsSensitive (true);
			PintaCore.Actions.Effects.ToggleActionsSensitive (true);
		}
	}
}

