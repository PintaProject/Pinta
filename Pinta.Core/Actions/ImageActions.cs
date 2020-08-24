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
using Mono.Unix;

namespace Pinta.Core
{
	public class ImageActions
	{
		public Gtk.Action CropToSelection { get; private set; }
		public Gtk.Action AutoCrop { get; private set; }
		public Gtk.Action Resize { get; private set; }
		public Gtk.Action CanvasSize { get; private set; }
		public Gtk.Action FlipHorizontal { get; private set; }
		public Gtk.Action FlipVertical { get; private set; }
		public Gtk.Action RotateCW { get; private set; }
		public Gtk.Action RotateCCW { get; private set; }
		public Gtk.Action Rotate180 { get; private set; }
		public Gtk.Action Flatten { get; private set; }

		public ImageActions ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Menu.Image.CanvasSize.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Image.CanvasSize.png")));
			fact.Add ("Menu.Image.Crop.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Image.Crop.png")));
			fact.Add ("Menu.Image.Flatten.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Image.Flatten.png")));
			fact.Add ("Menu.Image.FlipHorizontal.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Image.FlipHorizontal.png")));
			fact.Add ("Menu.Image.FlipVertical.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Image.FlipVertical.png")));
			fact.Add ("Menu.Image.Resize.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Image.Resize.png")));
			fact.Add ("Menu.Image.Rotate180CW.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Image.Rotate180CW.png")));
			fact.Add ("Menu.Image.Rotate90CCW.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Image.Rotate90CCW.png")));
			fact.Add ("Menu.Image.Rotate90CW.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Image.Rotate90CW.png")));
			fact.AddDefault ();
			
			CropToSelection = new Gtk.Action ("CropToSelection", Catalog.GetString ("Crop to Selection"), null, "Menu.Image.Crop.png");
			AutoCrop = new Gtk.Action ("AutoCrop", Catalog.GetString ("Auto Crop"), null, "Menu.Image.Crop.png");
			Resize = new Gtk.Action ("Resize", Catalog.GetString ("Resize Image..."), null, "Menu.Image.Resize.png");
			CanvasSize = new Gtk.Action ("CanvasSize", Catalog.GetString ("Resize Canvas..."), null, "Menu.Image.CanvasSize.png");
			FlipHorizontal = new Gtk.Action ("FlipHorizontal", Catalog.GetString ("Flip Horizontal"), null, "Menu.Image.FlipHorizontal.png");
			FlipVertical = new Gtk.Action ("FlipVertical", Catalog.GetString ("Flip Vertical"), null, "Menu.Image.FlipVertical.png");
			RotateCW = new Gtk.Action ("RotateCW", Catalog.GetString ("Rotate 90° Clockwise"), null, "Menu.Image.Rotate90CW.png");
			RotateCCW = new Gtk.Action ("RotateCCW", Catalog.GetString ("Rotate 90° Counter-Clockwise"), null, "Menu.Image.Rotate90CCW.png");
			Rotate180 = new Gtk.Action ("Rotate180", Catalog.GetString ("Rotate 180°"), null, "Menu.Image.Rotate180CW.png");
			Flatten = new Gtk.Action ("Flatten", Catalog.GetString ("Flatten"), null, "Menu.Image.Flatten.png");
			
			CropToSelection.Sensitive = false;
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Append (CropToSelection.CreateAcceleratedMenuItem (Gdk.Key.X, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (AutoCrop.CreateAcceleratedMenuItem (Gdk.Key.X, Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.ControlMask));
			menu.Append (Resize.CreateAcceleratedMenuItem (Gdk.Key.R, Gdk.ModifierType.ControlMask));
			menu.Append (CanvasSize.CreateAcceleratedMenuItem (Gdk.Key.R, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.AppendSeparator ();
			menu.Append (FlipHorizontal.CreateMenuItem ());
			menu.Append (FlipVertical.CreateMenuItem ());
			menu.AppendSeparator ();
			menu.Append (RotateCW.CreateAcceleratedMenuItem (Gdk.Key.H, Gdk.ModifierType.ControlMask));
			menu.Append (RotateCCW.CreateAcceleratedMenuItem (Gdk.Key.G, Gdk.ModifierType.ControlMask));
			menu.Append (Rotate180.CreateAcceleratedMenuItem (Gdk.Key.J, Gdk.ModifierType.ControlMask));
			menu.AppendSeparator ();
			menu.Append (Flatten.CreateAcceleratedMenuItem (Gdk.Key.F, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
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

			var oldBottomSurface = doc.UserLayers[0].Surface.Clone ();

			CompoundHistoryItem hist = new CompoundHistoryItem ("Menu.Image.Flatten.png", Catalog.GetString ("Flatten"));

			for (int i = doc.UserLayers.Count - 1; i >= 1; i--)
				hist.Push (new DeleteLayerHistoryItem (string.Empty, string.Empty, doc.UserLayers[i], i));

			doc.FlattenImage ();

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

		private void HandlePintaCoreActionsImageAutoCropActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

            using (var image = doc.GetFlattenedImage ())
            {
                Gdk.Rectangle rect = image.GetBounds ();

                Cairo.Color borderColor = image.GetPixel (0, 0);
                bool cropSide = true;
                int depth = -1;

                //From the top down
                while (cropSide) {
                    depth++;
                    for (int i = 0; i < image.Width; i++) {
                        if (!borderColor.Equals(image.GetPixel (i, depth))) {
                            cropSide = false;
                            break;
                        }
                    }
                    //Check if the image is blank/mono-coloured, only need to do it on this scan
                    if (depth == image.Height)
                        return;
                }

                rect = new Gdk.Rectangle (rect.X, rect.Y + depth, rect.Width, rect.Height - depth);

                depth = image.Height;
                cropSide = true;
                //From the bottom up
                while (cropSide) {
                    depth--;
                    for (int i = 0; i < image.Width; i++) {
                        if (!borderColor.Equals(image.GetPixel (i, depth))) {
                            cropSide = false;
                            break;
                        }
                    }

                }

                rect = new Gdk.Rectangle (rect.X, rect.Y, rect.Width, depth - rect.Y);

                depth = 0;
                cropSide = true;
                //From left to right
                while (cropSide) {
                    depth++;
                    for (int i = 0; i < image.Height; i++) {
                        if (!borderColor.Equals(image.GetPixel (depth, i))) {
                            cropSide = false;
                            break;
                        }
                    }

                }

                rect = new Gdk.Rectangle (rect.X + depth, rect.Y, rect.Width - depth, rect.Height);

                depth = image.Width;
                cropSide = true;
                //From right to left
                while (cropSide) {
                    depth--;
                    for (int i = 0; i < image.Height; i++) {
                        if (!borderColor.Equals(image.GetPixel (depth, i))) {
                            cropSide = false;
                            break;
                        }
                    }

                }

                rect = new Gdk.Rectangle (rect.X, rect.Y, depth - rect.X, rect.Height);

                // Ignore the current selection when auto-cropping.
                CropImageToRectangle (doc, rect, /*selection*/ null);
            }
		}
		#endregion

		static void CropImageToRectangle (Document doc, Gdk.Rectangle rect, Path selection)
		{
			if (rect.Width > 0 && rect.Height > 0)
			{
				ResizeHistoryItem hist = new ResizeHistoryItem(doc.ImageSize);

				hist.Icon = "Menu.Image.Crop.png";
				hist.Text = Catalog.GetString("Crop to Selection");
				hist.StartSnapshotOfImage();
				hist.RestoreSelection = doc.Selection.Clone();

				doc.Workspace.Canvas.GdkWindow.FreezeUpdates();

				double original_scale = doc.Workspace.Scale;
				doc.ImageSize = rect.Size;
				doc.Workspace.CanvasSize = rect.Size;
				doc.Workspace.Scale = original_scale;

				PintaCore.Actions.View.UpdateCanvasScale();

				doc.Workspace.Canvas.GdkWindow.ThawUpdates();

				foreach (var layer in doc.UserLayers)
                    layer.Crop (rect, selection);

				hist.FinishSnapshotOfImage();

				doc.History.PushNewItem(hist);
				doc.ResetSelectionPaths();

				doc.Workspace.Invalidate();
			}
		}
	}
}
