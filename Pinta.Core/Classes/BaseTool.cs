// 
// BaseTool.cs
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
using Gtk;
using Gdk;
using System.IO;
using Mono.Unix;
using Mono.Addins;
using System.Collections.Generic;

namespace Pinta.Core
{
	public delegate void MouseHandler (double x, double y, Gdk.ModifierType state);

	[TypeExtensionPoint]
	public abstract class BaseTool
	{
		public const int DEFAULT_BRUSH_WIDTH = 2;
	    
		protected static Cairo.Point point_empty = new Cairo.Point (-500, -500);

	    
		protected ToggleToolButton tool_item;
		protected ToolItem tool_label;
		protected ToolItem tool_image;
		protected ToolItem tool_sep;
		protected ToolBarDropDownButton antialiasing_button;
		private ToolBarItem aaOn, aaOff;
		protected ToolBarDropDownButton alphablending_button;
		private ToolBarItem abOn, abOff;
		public event MouseHandler MouseMoved;
		public event MouseHandler MousePressed;
		public event MouseHandler MouseReleased;

        public Cursor CurrentCursor { get; private set; }

		protected BaseTool ()
		{
            CurrentCursor = DefaultCursor;

            PintaCore.Workspace.ActiveDocumentChanged += Workspace_ActiveDocumentChanged;
		}

		static BaseTool ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Toolbar.AntiAliasingEnabledIcon.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.AntiAliasingEnabledIcon.png")));
			fact.Add ("Toolbar.AntiAliasingDisabledIcon.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.AntiAliasingDisabledIcon.png")));
			fact.Add ("Toolbar.BlendingEnabledIcon.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.BlendingEnabledIcon.png")));
			fact.Add ("Toolbar.BlendingOverwriteIcon.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.BlendingOverwriteIcon.png")));
			fact.Add ("Tools.FreeformShape.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Tools.FreeformShape.png")));
			fact.AddDefault ();
		}

		public virtual string Name { get { throw new ApplicationException ("Tool didn't override Name"); } }
		public virtual string Icon { get { throw new ApplicationException ("Tool didn't override Icon"); } }		
		public virtual string ToolTip { get { throw new ApplicationException ("Tool didn't override ToolTip"); } }
		public virtual string StatusBarText { get { return string.Empty; } }
		public virtual ToggleToolButton ToolItem { get { if (tool_item == null) tool_item = CreateToolButton (); return tool_item; } }
		public virtual bool Enabled { get { return true; } }
		public virtual Gdk.Cursor DefaultCursor { get { return null; } }
		public virtual Gdk.Key ShortcutKey { get { return (Gdk.Key)0; } }

		//Whether or not the tool is an editable ShapeTool.
		public bool IsEditableShapeTool = false;

		public virtual bool UseAntialiasing
		{
			get
			{
                return (antialiasing_button != null) &&
                        ShowAntialiasingButton &&
                        (bool)antialiasing_button.SelectedItem.Tag;
			}

			set
			{
			    if (!ShowAntialiasingButton || antialiasing_button == null)
                    return;

			    antialiasing_button.SelectedItem = value ? aaOn : aaOff;
			}
		}

		public virtual bool UseAlphaBlending
		{
			get
			{
				return alphablending_button != null &&
					   ShowAlphaBlendingButton &&
					   (bool)alphablending_button.SelectedItem.Tag;
			}
			set
			{
				if (!ShowAlphaBlendingButton || alphablending_button == null)
					return;

				alphablending_button.SelectedItem = value ? abOn : abOff;
			}
		}

		public virtual int Priority { get { return 75; } }

		public virtual bool CursorChangesOnZoom { get { return false; } }

		protected virtual bool ShowAntialiasingButton { get { return false; } }
		protected virtual bool ShowAlphaBlendingButton { get { return false; } }
		
		#region Public Methods
		public void DoMouseMove (object o, MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (MouseMoved != null)
				MouseMoved (point.X, point.Y, args.Event.State);
			OnMouseMove (o, args, point);
		}

		public void DoBuildToolBar (Toolbar tb)
		{
			OnBuildToolBar (tb);
			BuildRasterizationToolItems (tb);
		}

		public void DoClearToolBar (Toolbar tb)
		{
			OnClearToolBar (tb);
		}

		public void DoMouseDown (DrawingArea canvas, ButtonPressEventArgs args, Cairo.PointD point)
		{
			if (MousePressed != null)
				MousePressed (point.X, point.Y, args.Event.State);
			OnMouseDown (canvas, args, point);
		}

		public void DoMouseUp (DrawingArea canvas, ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			if (MouseReleased != null)
				MouseReleased (point.X, point.Y, args.Event.State);
			OnMouseUp (canvas, args, point);
		}

		public void DoCommit ()
		{
			OnCommit ();
		}

		public void DoAfterSave()
		{
			AfterSave();
		}

		public void DoActivated ()
		{
			OnActivated ();
		}
		
		public void DoDeactivated (BaseTool newTool)
		{
			OnDeactivated(newTool);
		}		
		
		// Return true if the key was consumed.
		public void DoKeyPress (DrawingArea canvas, KeyPressEventArgs args)
		{
			OnKeyDown (canvas, args);
		}

		public void DoKeyRelease (DrawingArea canvas, KeyReleaseEventArgs args)
		{
			OnKeyUp (canvas, args);
		}

		public virtual bool TryHandlePaste (Clipboard cb)
		{
			return false;
		}

		public virtual bool TryHandleCut (Clipboard cb)
		{
			return false;
		}

		public virtual bool TryHandleCopy (Clipboard cb)
		{
			return false;
		}

		public virtual bool TryHandleUndo ()
		{
			return false;
		}

		public virtual bool TryHandleRedo ()
		{
			return false;
		}

		public virtual void AfterUndo()
		{

		}

		public virtual void AfterRedo()
		{

		}

		#endregion

		#region Protected Methods
		protected virtual void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
		}

		protected virtual void BuildRasterizationToolItems (Toolbar tb)
		{
			if (ShowAlphaBlendingButton || ShowAntialiasingButton)
				tb.AppendItem (new SeparatorToolItem ());
			
			if (ShowAntialiasingButton)
				BuildAntialiasingTool (tb);
			if (ShowAlphaBlendingButton)
				BuildAlphaBlending(tb);

			AfterBuildRasterization();
		}

		protected virtual void AfterBuildRasterization()
		{

		}

		protected virtual void OnBuildToolBar (Toolbar tb)
		{
			if (tool_label == null)
				tool_label = new ToolBarLabel (string.Format (" {0}:  ", Catalog.GetString ("Tool")));
			
			tb.AppendItem (tool_label);
			
			if (tool_image == null)
				tool_image = new ToolBarImage (Icon);
			
			tb.AppendItem (tool_image);
			
			if (tool_sep == null)
				tool_sep = new SeparatorToolItem ();
			
			tb.AppendItem (tool_sep);
		}

		protected virtual void OnClearToolBar (Toolbar tb)
		{
			while (tb.NItems > 0)
				tb.Remove (tb.Children[tb.NItems - 1]);
		}

		protected virtual void OnMouseDown (DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
		}

		protected virtual void OnMouseUp (DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
		}
		
		protected virtual void OnKeyDown (DrawingArea canvas, Gtk.KeyPressEventArgs args)
		{
		}

		protected virtual void OnKeyUp (DrawingArea canvas, Gtk.KeyReleaseEventArgs args)
		{
		}
		
		/// <summary>
		/// This is called whenever a menu option is called, for
		/// tools that are in a temporary state while being used, and
		/// need to commit their work when another option is selected.
		/// </summary>
		protected virtual void OnCommit ()
		{
		}

		protected virtual void AfterSave()
		{

		}

		protected virtual void OnActivated ()
		{
			SetCursor (DefaultCursor);
		}

		protected virtual void OnDeactivated(BaseTool newTool)
		{
			SetCursor (null);
		}

		protected virtual ToggleToolButton CreateToolButton ()
		{
			Gtk.Image i2 = new Gtk.Image (PintaCore.Resources.GetIcon (Icon));
			i2.Show ();
			
			ToggleToolButton tool_item = new ToggleToolButton ();
			tool_item.IconWidget = i2;
			tool_item.Show ();
			tool_item.Label = Name;
			
			if (ShortcutKey != (Gdk.Key)0)
				tool_item.TooltipText = string.Format ("{0}\n{2}: {1}\n\n{3}", Name, ShortcutKey.ToString ().ToUpperInvariant (), Catalog.GetString ("Shortcut key"), StatusBarText);
			else
				tool_item.TooltipText = Name;
			
			return tool_item;
		}
		
		public void SetCursor (Gdk.Cursor cursor)
		{
            CurrentCursor = cursor;

            if (PintaCore.Workspace.HasOpenDocuments && PintaCore.Workspace.ActiveWorkspace.Canvas.GdkWindow != null)
			    PintaCore.Workspace.ActiveWorkspace.Canvas.GdkWindow.Cursor = cursor;
		}

		/// <summary>
		/// Create a cursor icon with a shape that visually represents the tool's thickness.
		/// </summary>
		/// <param name="imgName">A string containing the name of the tool's icon image to use.</param>
		/// <param name="shape">The shape to draw.</param>
		/// <param name="shapeWidth">The width of the shape.</param>
		/// <param name="imgToShapeX">The horizontal distance between the image's top-left corner and the shape center.</param>
		/// <param name="imgToShapeY">The verical distance between the image's top-left corner and the shape center.</param>
		/// <param name="shapeX">The X position in the returned Pixbuf that will be the center of the shape.</param>
		/// <param name="shapeY">The Y position in the returned Pixbuf that will be the center of the shape.</param>
		/// <returns>The new cursor icon with an shape that represents the tool's thickness.</returns>
		protected Gdk.Pixbuf CreateIconWithShape(string imgName, CursorShape shape, int shapeWidth,
		                                          int imgToShapeX, int imgToShapeY,
		                                          out int shapeX, out int shapeY)
		{
			Gdk.Pixbuf img = PintaCore.Resources.GetIcon(imgName);

			double zoom = 1d;
			if (PintaCore.Workspace.HasOpenDocuments)
			{
				zoom = Math.Min(30d, PintaCore.Workspace.ActiveDocument.Workspace.Scale);
			}

			shapeWidth = (int)Math.Min(800d, ((double)shapeWidth) * zoom);
			int halfOfShapeWidth = shapeWidth / 2;

			// Calculate bounding boxes around the both image and shape
			// relative to the image top-left corner.
			Gdk.Rectangle imgBBox = new Gdk.Rectangle(0, 0, img.Width, img.Height);
			Gdk.Rectangle shapeBBox = new Gdk.Rectangle(
				imgToShapeX - halfOfShapeWidth,
				imgToShapeY - halfOfShapeWidth,
				shapeWidth,
				shapeWidth);

			// Inflate shape bounding box to allow for anti-aliasing
			shapeBBox.Inflate(2, 2);

			// To determine required size of icon,
			// find union of the image and shape bounding boxes
			// (still relative to image top-left corner)
			Gdk.Rectangle iconBBox = imgBBox.Union (shapeBBox);

			// Image top-left corner in icon co-ordinates
			int imgX = imgBBox.Left - iconBBox.Left;
			int imgY = imgBBox.Top - iconBBox.Top;

			// Shape center point in icon co-ordinates
			shapeX = imgToShapeX - iconBBox.Left;
			shapeY = imgToShapeY - iconBBox.Top;

			using (ImageSurface i = new ImageSurface (Format.ARGB32, iconBBox.Width, iconBBox.Height)) {
				using (Context g = new Context (i)) {
					// Don't show shape if shapeWidth less than 3,
					if (shapeWidth > 3) {
						int diam = Math.Max (1, shapeWidth - 2);
						Cairo.Rectangle shapeRect = new Cairo.Rectangle (shapeX - halfOfShapeWidth,
												 shapeY - halfOfShapeWidth,
												 diam,
												 diam);

						Cairo.Color outerColor = new Cairo.Color (255, 255, 255, 0.75);
						Cairo.Color innerColor = new Cairo.Color (0, 0, 0);

						switch (shape) {
							case CursorShape.Ellipse:
								g.DrawEllipse (shapeRect, outerColor, 2);
								shapeRect = shapeRect.Inflate (-1, -1);
								g.DrawEllipse (shapeRect, innerColor, 1);
								break;
							case CursorShape.Rectangle:
								g.DrawRectangle (shapeRect, outerColor, 1);
								shapeRect = shapeRect.Inflate (-1, -1);
								g.DrawRectangle (shapeRect, innerColor, 1);
								break;
						}
					}

					// Draw the image
					g.DrawPixbuf (img, new Cairo.Point (imgX, imgY));
				}

				return CairoExtensions.ToPixbuf (i);
			}
		}
		#endregion

		#region Private Methods
		private void BuildAlphaBlending (Toolbar tb)
		{
			if (alphablending_button != null) {
				tb.AppendItem (alphablending_button);
				return;
			}

			alphablending_button = new ToolBarDropDownButton ();

			abOn = alphablending_button.AddItem (Catalog.GetString ("Normal Blending"), "Toolbar.BlendingEnabledIcon.png", true);
			abOff = alphablending_button.AddItem (Catalog.GetString ("Overwrite"), "Toolbar.BlendingOverwriteIcon.png", false);

			tb.AppendItem (alphablending_button);
		}

		private void BuildAntialiasingTool (Toolbar tb)
		{
			if (antialiasing_button != null) {
				tb.AppendItem (antialiasing_button);
				return;
			}

			antialiasing_button = new ToolBarDropDownButton ();

			aaOn = antialiasing_button.AddItem (Catalog.GetString ("Antialiasing On"), "Toolbar.AntiAliasingEnabledIcon.png", true);
			aaOff = antialiasing_button.AddItem (Catalog.GetString ("Antialiasing Off"), "Toolbar.AntiAliasingDisabledIcon.png", false);

			tb.AppendItem (antialiasing_button);
		}

		private void Workspace_ActiveDocumentChanged (object sender, EventArgs e)
		{
            if (PintaCore.Tools.CurrentTool == this)
			    SetCursor (DefaultCursor);
		}
		#endregion
	}
}
