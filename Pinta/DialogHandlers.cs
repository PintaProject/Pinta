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
using Pinta.Core;

namespace Pinta
{
	public class DialogHandlers
	{
		private MainWindow main_window;

		public DialogHandlers (MainWindow window)
		{
			main_window = window;
			
			PintaCore.Actions.File.New.Activated += HandlePintaCoreActionsFileNewActivated;
			PintaCore.Actions.Image.Resize.Activated += HandlePintaCoreActionsImageResizeActivated;
			PintaCore.Actions.Image.CanvasSize.Activated += HandlePintaCoreActionsImageCanvasSizeActivated;
			PintaCore.Actions.Layers.Properties.Activated += HandlePintaCoreActionsLayersPropertiesActivated;
			PintaCore.Actions.Adjustments.BrightnessContrast.Activated += HandleAdjustmentsBrightnessContrastActivated;
			PintaCore.Actions.Adjustments.Posterize.Activated += HandleAdjustmentsPosterizeActivated;
			PintaCore.Actions.Adjustments.HueSaturation.Activated += HandleAdjustmentsHueSaturationActivated;
		}

		#region Handlers
		private void HandlePintaCoreActionsFileNewActivated (object sender, EventArgs e)
		{
			NewImageDialog dialog = new NewImageDialog ();

			dialog.ParentWindow = main_window.GdkWindow;
			dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				PintaCore.Workspace.ImageSize = new Cairo.Point (dialog.NewImageWidth, dialog.NewImageHeight);
				PintaCore.Workspace.CanvasSize = new Cairo.Point (dialog.NewImageWidth, dialog.NewImageHeight);
				
				PintaCore.Layers.Clear ();
				PintaCore.History.Clear ();
				PintaCore.Layers.DestroySelectionLayer ();
				PintaCore.Layers.ResetSelectionPath ();

				// Start with an empty white layer
				Layer background = PintaCore.Layers.AddNewLayer ("Background");

				using (Cairo.Context g = new Cairo.Context (background.Surface)) {
					g.SetSourceRGB (255, 255, 255);
					g.Paint ();
				}

				PintaCore.Workspace.Filename = "Untitled1";
				PintaCore.Workspace.IsDirty = false;
				PintaCore.Actions.View.ZoomToWindow.Activate ();
			}

			dialog.Destroy ();
		}

		private void HandlePintaCoreActionsImageResizeActivated (object sender, EventArgs e)
		{
			ResizeImageDialog dialog = new ResizeImageDialog ();

			dialog.ParentWindow = main_window.GdkWindow;
			dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok)
				dialog.SaveChanges ();

			dialog.Destroy ();
		}
		
		private void HandlePintaCoreActionsImageCanvasSizeActivated (object sender, EventArgs e)
		{
			ResizeCanvasDialog dialog = new ResizeCanvasDialog ();

			dialog.ParentWindow = main_window.GdkWindow;
			dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok)
				dialog.SaveChanges ();

			dialog.Destroy ();
		}

		private void HandlePintaCoreActionsLayersPropertiesActivated (object sender, EventArgs e)
		{
			LayerPropertiesDialog dialog = new LayerPropertiesDialog ();
			
			int response = dialog.Run ();
			
			if (response == (int)Gtk.ResponseType.Ok) {
				dialog.SaveChanges ();
				PintaCore.Workspace.Invalidate ();
			}
				
			dialog.Destroy ();
		}
		
		private void HandleAdjustmentsHueSaturationActivated (object sender, EventArgs e)
		{
			PintaCore.Actions.Adjustments.PerformEffect (new HueSaturationEffect ());
		}
		
		private void HandleAdjustmentsBrightnessContrastActivated (object sender, EventArgs e)
		{
			PintaCore.Actions.Adjustments.PerformEffect (new BrightnessContrastEffect ());
		}
		
		private void HandleAdjustmentsPosterizeActivated (object sender, EventArgs e)
		{
			PintaCore.Actions.Adjustments.PerformEffect (new PosterizeEffect ());
		}
		#endregion
	}
}

