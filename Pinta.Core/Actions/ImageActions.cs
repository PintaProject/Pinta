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
using Mono.Unix;

namespace Pinta.Core
{
	public class ImageActions
	{
		public Gtk.Action CropToSelection { get; private set; }
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
			Resize = new Gtk.Action ("Resize", Catalog.GetString ("Resize..."), null, "Menu.Image.Resize.png");
			CanvasSize = new Gtk.Action ("CanvasSize", Catalog.GetString ("Canvas Size..."), null, "Menu.Image.CanvasSize.png");
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
		}
		#endregion

		#region Action Handlers
		private void HandlePintaCoreActionsImageRotateCCWActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			PintaCore.Layers.RotateImageCCW ();
			PintaCore.History.PushNewItem (new InvertHistoryItem (InvertType.Rotate90CCW));
		}

		private void HandlePintaCoreActionsImageRotateCWActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			PintaCore.Layers.RotateImageCW ();
			PintaCore.History.PushNewItem (new InvertHistoryItem (InvertType.Rotate90CW));
		}

		private void HandlePintaCoreActionsImageFlattenActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			CompoundHistoryItem hist = new CompoundHistoryItem ("Menu.Image.Flatten.png", Catalog.GetString ("Flatten"));
			SimpleHistoryItem h1 = new SimpleHistoryItem (string.Empty, string.Empty, PintaCore.Layers[0].Surface.Clone (), 0);
			hist.Push (h1);

			for (int i = 1; i < PintaCore.Layers.Count; i++)
				hist.Push (new DeleteLayerHistoryItem (string.Empty, string.Empty, PintaCore.Layers[i], i));

			PintaCore.Layers.FlattenImage ();

			PintaCore.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsImageRotate180Activated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			PintaCore.Layers.RotateImage180 ();
			PintaCore.History.PushNewItem (new InvertHistoryItem (InvertType.Rotate180));
		}

		private void HandlePintaCoreActionsImageFlipVerticalActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			PintaCore.Layers.FlipImageVertical ();
			PintaCore.History.PushNewItem (new InvertHistoryItem (InvertType.FlipVertical));
		}

		private void HandlePintaCoreActionsImageFlipHorizontalActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			PintaCore.Layers.FlipImageHorizontal ();
			PintaCore.History.PushNewItem (new InvertHistoryItem (InvertType.FlipHorizontal));
		}

		private void HandlePintaCoreActionsImageCropToSelectionActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			Gdk.Rectangle rect = PintaCore.Layers.SelectionPath.GetBounds ();

			int width = rect.Width;
			int height = rect.Height;
			
			ResizeHistoryItem hist = new ResizeHistoryItem (PintaCore.Workspace.ImageSize.Width, PintaCore.Workspace.ImageSize.Height);
			hist.Icon = "Menu.Image.Crop.png";
			hist.Text = Catalog.GetString ("Crop to Selection");
			hist.TakeSnapshotOfImage ();
			hist.RestorePath = PintaCore.Layers.SelectionPath.Clone ();

			PintaCore.Workspace.ImageSize = new Gdk.Size (width, height);
			PintaCore.Workspace.CanvasSize = new Gdk.Size (width, height);

			foreach (var layer in PintaCore.Layers)
				layer.Crop (rect);

			PintaCore.History.PushNewItem (hist);

			PintaCore.Layers.ResetSelectionPath ();
			PintaCore.Workspace.Invalidate ();
		}
		#endregion
	}
}
