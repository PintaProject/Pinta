// 
// ImageActions.cs
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

namespace Pinta.Core
{
	public class ImageActions
	{
		public Command CropToSelection { get; private set; }
		public Command AutoCrop { get; private set; }
		public Command Resize { get; private set; }
		public Command CanvasSize { get; private set; }
		public Command FlipHorizontal { get; private set; }
		public Command FlipVertical { get; private set; }
		public Command RotateCW { get; private set; }
		public Command RotateCCW { get; private set; }
		public Command Rotate180 { get; private set; }
		public Command Flatten { get; private set; }

		public ImageActions ()
		{
			CropToSelection = new Command ("croptoselection", Translations.GetString ("Crop to Selection"), null, Resources.Icons.ImageCrop);
			AutoCrop = new Command ("autocrop", Translations.GetString ("Auto Crop"), null, Resources.Icons.ImageCrop);
			Resize = new Command ("resize", Translations.GetString ("Resize Image..."), null, Resources.Icons.ImageResize);
			CanvasSize = new Command ("canvassize", Translations.GetString ("Resize Canvas..."), null, Resources.Icons.ImageResizeCanvas);
			FlipHorizontal = new Command ("fliphorizontal", Translations.GetString ("Flip Horizontal"), null, Resources.Icons.ImageFlipHorizontal);
			FlipVertical = new Command ("flipvertical", Translations.GetString ("Flip Vertical"), null, Resources.Icons.ImageFlipVertical);
			RotateCW = new Command ("rotatecw", Translations.GetString ("Rotate 90° Clockwise"), null, Resources.Icons.ImageRotate90CW);
			RotateCCW = new Command ("rotateccw", Translations.GetString ("Rotate 90° Counter-Clockwise"), null, Resources.Icons.ImageRotate90CCW);
			Rotate180 = new Command ("rotate180", Translations.GetString ("Rotate 180°"), null, Resources.Icons.ImageRotate180);
			Flatten = new Command ("flatten", Translations.GetString ("Flatten"), null, Resources.Icons.ImageFlatten);
		}

		#region Initialization
		public void RegisterActions(Gtk.Application app, GLib.Menu menu)
		{
			app.AddAccelAction(CropToSelection, "<Primary><Shift>X");
			menu.AppendItem(CropToSelection.CreateMenuItem());

			app.AddAccelAction(AutoCrop, "<Ctrl><Alt>X");
			menu.AppendItem(AutoCrop.CreateMenuItem());

			app.AddAccelAction(Resize, "<Primary>R");
			menu.AppendItem(Resize.CreateMenuItem());

			app.AddAccelAction(CanvasSize, "<Primary><Shift>R");
			menu.AppendItem(CanvasSize.CreateMenuItem());

			var flip_section = new GLib.Menu();
			menu.AppendSection(null, flip_section);

			app.AddAction(FlipHorizontal);
			flip_section.AppendItem(FlipHorizontal.CreateMenuItem());

			app.AddAction(FlipVertical);
			flip_section.AppendItem(FlipVertical.CreateMenuItem());

			var rotate_section = new GLib.Menu();
			menu.AppendSection(null, rotate_section);

			app.AddAccelAction(RotateCW, "<Primary>H");
			rotate_section.AppendItem(RotateCW.CreateMenuItem());

			app.AddAccelAction(RotateCCW, "<Primary>G");
			rotate_section.AppendItem(RotateCCW.CreateMenuItem());

			app.AddAccelAction(Rotate180, "<Primary>J");
			rotate_section.AppendItem(Rotate180.CreateMenuItem());

			var flatten_section = new GLib.Menu();
			menu.AppendSection(null, flatten_section);

			app.AddAccelAction(Flatten, "<Primary><Shift>F");
			flatten_section.AppendItem(Flatten.CreateMenuItem());
		}
				
		public void RegisterHandlers ()
		{
			FlipHorizontal.Activated += HandlePintaCoreActionsImageFlipHorizontalActivated;
			FlipVertical.Activated += HandlePintaCoreActionsImageFlipVerticalActivated;
			Rotate180.Activated += HandlePintaCoreActionsImageRotate180Activated;
			Flatten.Activated += HandlePintaCoreActionsImageFlattenActivated;
			RotateCW.Activated += HandlePintaCoreActionsImageRotateCWActivated;
			RotateCCW.Activated += HandlePintaCoreActionsImageRotateCCWActivated;
			CropToSelection.Activated += HandlePintaCoreActionsImageCropToSelectionActivated;
			AutoCrop.Activated += HandlePintaCoreActionsImageAutoCropActivated;

			PintaCore.Workspace.SelectionChanged += (o, _) => {
				var visible = false;
				if (PintaCore.Workspace.HasOpenDocuments)
					visible = PintaCore.Workspace.ActiveDocument.Selection.Visible;

				CropToSelection.Sensitive = visible;
			};
		}
		#endregion

		#region Action Handlers
		private void HandlePintaCoreActionsImageRotateCCWActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();
			doc.RotateImageCCW ();

			doc.ResetSelectionPaths ();

			doc.History.PushNewItem (new InvertHistoryItem (InvertType.Rotate90CCW));
		}

		private void HandlePintaCoreActionsImageRotateCWActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();
			doc.RotateImageCW ();

			doc.ResetSelectionPaths ();

			doc.History.PushNewItem (new InvertHistoryItem (InvertType.Rotate90CW));
		}

		private void HandlePintaCoreActionsImageFlattenActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			var oldBottomSurface = doc.Layers.UserLayers[0].Surface.Clone ();

			CompoundHistoryItem hist = new CompoundHistoryItem (Resources.Icons.ImageFlatten, Translations.GetString ("Flatten"));

			for (int i = doc.Layers.UserLayers.Count - 1; i >= 1; i--)
				hist.Push (new DeleteLayerHistoryItem (string.Empty, string.Empty, doc.Layers.UserLayers[i], i));

			doc.Layers.FlattenLayers ();

			hist.Push (new SimpleHistoryItem (string.Empty, string.Empty, oldBottomSurface, 0));
			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsImageRotate180Activated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();
			doc.RotateImage180 ();

			doc.ResetSelectionPaths ();

			doc.History.PushNewItem (new InvertHistoryItem (InvertType.Rotate180));
		}

		private void HandlePintaCoreActionsImageFlipVerticalActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();
			doc.FlipImageVertical ();

			doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipVertical));
		}

		private void HandlePintaCoreActionsImageFlipHorizontalActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();
			doc.FlipImageHorizontal ();

			doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipHorizontal));
		}

		private void HandlePintaCoreActionsImageCropToSelectionActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			Gdk.Rectangle rect = doc.GetSelectedBounds (true);

			CropImageToRectangle (doc, rect, doc.Selection.SelectionPath);
		}

		/// <summary>
        /// Checks if all of the pixels in the row match the specified color.
        /// </summary>
        private static bool IsConstantRow (ImageSurface surf, Cairo.Color color, int y)
        {
            for (int x = 0; x < surf.Width; ++x) {
                if (!color.Equals (surf.GetPixel (x, y)))
                    return false;
            }

            return true;
        }

		/// <summary>
		/// Checks if all of the pixels in the column (within the bounds of the rectangle) match the specified color.
		/// </summary>
		private static bool IsConstantColumn (ImageSurface surf, Cairo.Color color, Gdk.Rectangle rect, int x)
		{
			for (int y = rect.Top; y < rect.Bottom; ++y) {
				if (!color.Equals (surf.GetPixel (x, y)))
					return false;
			}

			return true;
		}

		private void HandlePintaCoreActionsImageAutoCropActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

            using (var image = doc.GetFlattenedImage ())
            {
				Gdk.Rectangle rect = image.GetBounds ();
                Cairo.Color border_color = image.GetPixel (0, 0);

				// Top down.
                for (int y = 0; y < image.Height; ++y) {
                    if (!IsConstantRow (image, border_color, y))
                        break;

                    ++rect.Y;
                    --rect.Height;
                }

				// Bottom up.
				for (int y = rect.Bottom; y >= rect.Top; --y) {
					if (!IsConstantRow (image, border_color, y))
						break;

					--rect.Height;
				}

				// Left side.
				for (int x = 0; x < image.Width; ++x) {
					if (!IsConstantColumn (image, border_color, rect, x))
						break;

					++rect.X;
					--rect.Width;
				}

				// Right side.
				for (int x = rect.Right; x >= rect.Left; --x) {
					if (!IsConstantColumn (image, border_color, rect, x))
						break;

					--rect.Width;
				}

				// Ignore the current selection when auto-cropping.
				CropImageToRectangle (doc, rect, /*selection*/ null);
            }
		}
#endregion

		static void CropImageToRectangle (Document doc, Gdk.Rectangle rect, Path? selection)
		{
			if (rect.Width > 0 && rect.Height > 0)
			{
				ResizeHistoryItem hist = new ResizeHistoryItem(doc.ImageSize);

				hist.Icon = Resources.Icons.ImageCrop;
				hist.Text = Translations.GetString("Crop to Selection");
				hist.StartSnapshotOfImage();
				hist.RestoreSelection = doc.Selection.Clone();

				doc.Workspace.Canvas.Window.FreezeUpdates();

				double original_scale = doc.Workspace.Scale;
				doc.ImageSize = rect.Size;
				doc.Workspace.CanvasSize = rect.Size;
				doc.Workspace.Scale = original_scale;

				PintaCore.Actions.View.UpdateCanvasScale();

				doc.Workspace.Canvas.Window.ThawUpdates();

				foreach (var layer in doc.Layers.UserLayers)
                    layer.Crop (rect, selection);

				hist.FinishSnapshotOfImage();

				doc.History.PushNewItem(hist);
				doc.ResetSelectionPaths();

				doc.Workspace.Invalidate();
			}
		}
	}
}
