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
			PintaCore.Actions.Adjustments.BrightnessContrast.Activated += HandleEffectActivated <BrightnessContrastEffect>;
			PintaCore.Actions.Adjustments.Curves.Activated += HandleAdjustmentsCurvesActivated;
			PintaCore.Actions.Adjustments.Levels.Activated += HandleAdjustmentsLevelsActivated;
			PintaCore.Actions.Adjustments.Posterize.Activated += HandleAdjustmentsPosterizeActivated;
			PintaCore.Actions.Adjustments.HueSaturation.Activated += HandleEffectActivated <HueSaturationEffect>;
			PintaCore.Actions.Effects.InkSketch.Activated += HandleEffectActivated <InkSketchEffect>;
			PintaCore.Actions.Effects.OilPainting.Activated += HandleEffectActivated <OilPaintingEffect>;
			PintaCore.Actions.Effects.PencilSketch.Activated += HandleEffectActivated <PencilSketchEffect>;
			PintaCore.Actions.Effects.Fragment.Activated += HandleEffectActivated <FragmentEffect>;
			PintaCore.Actions.Effects.GaussianBlur.Activated += HandleEffectActivated <GaussianBlurEffect>;
			PintaCore.Actions.Effects.RadialBlur.Activated += HandleEffectRadialBlurActivated;
			PintaCore.Actions.Effects.MotionBlur.Activated += HandleEffectActivated <MotionBlurEffect>;
			PintaCore.Actions.Effects.Glow.Activated += HandleEffectActivated <GlowEffect>;
			PintaCore.Actions.Effects.RedEyeRemove.Activated += HandleEffectActivated <RedEyeRemoveEffect>;
			PintaCore.Actions.Effects.Sharpen.Activated += HandleEffectActivated <SharpenEffect>;
			PintaCore.Actions.Effects.SoftenPortrait.Activated += HandleEffectActivated <SoftenPortraitEffect>;
			PintaCore.Actions.Effects.EdgeDetect.Activated += HandleEffectActivated <EdgeDetectEffect>;
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
				PintaCore.History.PushNewItem (new BaseHistoryItem ("gtk-new", "New Image"));
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
			var dialog = new LayerPropertiesDialog ();
			
			int response = dialog.Run ();		
			
			if (response == (int)Gtk.ResponseType.Ok
			    && dialog.AreLayerPropertiesUpdated) {
				
				var historyMessage = GetLayerPropertyUpdateMessage(
						dialog.InitialLayerProperties,
						dialog.UpdatedLayerProperties);				
				
				var historyItem = new UpdateLayerPropertiesHistoryItem(
					"Menu.Layers.LayerProperties.png",
					historyMessage,
					PintaCore.Layers.CurrentLayerIndex,
					dialog.InitialLayerProperties,
					dialog.UpdatedLayerProperties);
				
				PintaCore.History.PushNewItem (historyItem);
				
				PintaCore.Workspace.Invalidate ();
				
			} else {
				
				var layer = PintaCore.Layers.CurrentLayer;
				var initial = dialog.InitialLayerProperties;
				initial.SetProperties (layer);
				
				if (layer.Opacity != initial.Opacity)
					PintaCore.Workspace.Invalidate ();
			}
				
			dialog.Destroy ();
		}
		
		private string GetLayerPropertyUpdateMessage (
			LayerProperties initial,
			LayerProperties updated)
		{
			string ret = null;
			int count = 0;
			
			if (updated.Opacity != initial.Opacity) {
				ret = "Layer Opacity";
				count++;
			}
				
			if (updated.Name != initial.Name) {
				ret = "Rename Layer";
				count++;
			}
			
			if (updated.Hidden != initial.Hidden) {
				ret = (updated.Hidden) ? "Hide Layer" : "Show Layer";
				count++;
			}
			
			if (ret == null || count > 1)
				ret = "Layer Properties";
			
			return ret;
		}		
		
		private void HandleEffectActivated<T> (object sender, EventArgs e)
			where T : BaseEffect, new ()
		{
			var effect = new T ();
			PintaCore.LivePreview.Start (effect);
		}
				
		private void HandleAdjustmentsPosterizeActivated (object sender, EventArgs e)
		{
			PintaCore.Actions.Adjustments.PerformEffect (new PosterizeEffect ());
		}
		
		private void HandleAdjustmentsCurvesActivated (object sender, EventArgs e)
		{
			PintaCore.Actions.Adjustments.PerformEffect (new CurvesEffect ());	
		}

		private void HandleAdjustmentsLevelsActivated (object sender, EventArgs e)
		{
			PintaCore.Actions.Adjustments.PerformEffect (new LevelsEffect ());
		}

		private void HandleEffectRadialBlurActivated (object sender, EventArgs e)
		{
			PintaCore.Actions.Adjustments.PerformEffect (new RadialBlurEffect ());
		}
		#endregion
	}
}

