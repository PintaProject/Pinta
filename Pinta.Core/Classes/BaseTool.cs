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
using System.IO;
using Mono.Unix;
using Mono.Addins;

namespace Pinta.Core
{
	public delegate void MouseHandler (double x, double y, Gdk.ModifierType state);

	[TypeExtensionPoint]
	public abstract class BaseTool
	{
		protected const int DEFAULT_BRUSH_WIDTH = 2;
	    
		protected static Point point_empty = new Point (-500, -500);
	    
		protected ToggleToolButton tool_item;
		protected ToolItem tool_label;
		protected ToolItem tool_image;
		protected ToolItem tool_sep;
		protected ToolBarDropDownButton antialiasing_button;
		protected ToolBarDropDownButton alphablending_button;
		public event MouseHandler MouseMoved;
		public event MouseHandler MousePressed;
		public event MouseHandler MouseReleased;

		protected BaseTool ()
		{
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
		public virtual bool UseAntialiasing { get { return ShowAntialiasingButton && (bool)antialiasing_button.SelectedItem.Tag; } }
		public virtual bool UseAlphaBlending { get { return ShowAlphaBlendingButton && (bool)alphablending_button.SelectedItem.Tag; } }
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

		public void DoActivated ()
		{
			OnActivated ();
		}
		
		public void DoDeactivated ()
		{
			OnDeactivated ();
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
				BuildAlphaBlending (tb);			
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

		protected virtual void OnActivated ()
		{
			PintaCore.Workspace.CanvasSizeChanged += new EventHandler(Workspace_CanvasSizeChanged);

			SetCursor (DefaultCursor);
		}
		
		protected virtual void OnDeactivated ()
		{
			PintaCore.Workspace.CanvasSizeChanged -= new EventHandler(Workspace_CanvasSizeChanged);

			SetCursor (null);
		}

		protected virtual ToggleToolButton CreateToolButton ()
		{
			Image i2 = new Image (PintaCore.Resources.GetIcon (Icon));
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
			PintaCore.Chrome.Canvas.GdkWindow.Cursor = cursor;
		}

		/// <summary>
		/// Create a cursor icon with an ellipse that visually represents the tool's thickness.
		/// </summary>
		/// <param name="name">A string containing the name of the tool's icon to use.</param>
		/// <param name="brushWidth">The current thickness of the tool.</param>
		/// <param name="cursorWidth">The width of the tool's icon.</param>
		/// <param name="cursorHeight">The height of the tool's icon.</param>
		/// <param name="cursorOffsetX">The X position in the tool's icon where the center of the affected area will be.</param>
		/// <param name="cursorOffsetY">The Y position in the tool's icon where the center of the affected area will be.</param>
		/// <param name="ellipseColor1">The inner color of the ellipse.</param>
		/// <param name="ellipseColor2">The outer color of the ellipse.</param>
		/// <param name="ellipseThickness">The thickness of the ellipse that will be drawn.</param>
		/// <param name="iconOffsetX">The X position in the returned Pixbuf that will be the center of the new cursor icon.</param>
		/// <param name="iconOffsetY">The Y position in the returned Pixbuf that will be the center of the new cursor icon.</param>
		/// <returns>The new cursor icon with an ellipse that represents the tool's thickness.</returns>
		protected Gdk.Pixbuf CreateEllipticalThicknessIcon(string name, int brushWidth, int cursorWidth, int cursorHeight,
		                                                   int cursorOffsetX, int cursorOffsetY, Color ellipseColor1,
		                                                   Color ellipseColor2, int ellipseThickness,
		                                                   out int iconOffsetX, out int iconOffsetY)
		{
			double zoom = 1d;

			if (PintaCore.Workspace.HasOpenDocuments)
			{
				zoom = Math.Min(30d, PintaCore.Workspace.ActiveDocument.Workspace.Scale);
			}

			brushWidth = (int)Math.Min(800d, ((double)brushWidth) * zoom);
			cursorWidth = (int)((double)cursorWidth * zoom);
			cursorHeight = (int)((double)cursorHeight * zoom);


			int halfOfEllipseThickness = (int)Math.Ceiling(ellipseThickness / 2d);
			int halfOfBrushWidth = brushWidth / 2;

			int iconWidth = Math.Max(cursorWidth + cursorOffsetX + halfOfBrushWidth + halfOfEllipseThickness,
				brushWidth + ellipseThickness + (int)(16d / zoom));
			int iconHeight = Math.Max(cursorHeight + cursorOffsetY + halfOfBrushWidth + halfOfEllipseThickness,
				brushWidth + ellipseThickness + (int)(16d / zoom));

			iconOffsetX = Math.Max(cursorOffsetX, halfOfBrushWidth + halfOfEllipseThickness);
			iconOffsetY = Math.Max(cursorOffsetY, halfOfBrushWidth + halfOfEllipseThickness);

			ImageSurface i = new ImageSurface(Format.ARGB32, iconWidth, iconHeight);

			using (Context g = new Context(i))
			{
				g.DrawEllipse(new Rectangle(iconOffsetX - halfOfBrushWidth,
					iconOffsetY - halfOfBrushWidth,
					brushWidth,
					brushWidth),
					ellipseColor2, ellipseThickness);

				g.DrawEllipse(new Rectangle(iconOffsetX - halfOfBrushWidth + ellipseThickness,
					iconOffsetY - halfOfBrushWidth + ellipseThickness,
					brushWidth - ellipseThickness - 1,
					brushWidth - ellipseThickness - 1),
					ellipseColor1, ellipseThickness);

				g.DrawPixbuf(PintaCore.Resources.GetIcon(name),
					new Point(iconOffsetX - cursorOffsetX, iconOffsetY - cursorOffsetY));
			}

			return CairoExtensions.ToPixbuf(i);
		}

		/// <summary>
		/// Create a cursor icon with an rectangle that visually represents the tool's thickness.
		/// </summary>
		/// <param name="name">A string containing the name of the tool's icon to use.</param>
		/// <param name="brushWidth">The current thickness of the tool.</param>
		/// <param name="cursorWidth">The width of the tool's icon.</param>
		/// <param name="cursorHeight">The height of the tool's icon.</param>
		/// <param name="cursorOffsetX">The X position in the tool's icon where the center of the affected area will be.</param>
		/// <param name="cursorOffsetY">The Y position in the tool's icon where the center of the affected area will be.</param>
		/// <param name="rectangleColor1">The inner color of the rectangle.</param>
		/// <param name="rectangleColor2">The outer color of the rectangle.</param>
		/// <param name="rectangleThickness">The thickness of the rectangle that will be drawn.</param>
		/// <param name="iconOffsetX">The X position in the returned Pixbuf that will be the center of the new cursor icon.</param>
		/// <param name="iconOffsetY">The Y position in the returned Pixbuf that will be the center of the new cursor icon.</param>
		/// <returns>The new cursor icon with an rectangle that represents the tool's thickness.</returns>
		protected Gdk.Pixbuf CreateRectangularThicknessIcon(string name, int brushWidth, int cursorWidth, int cursorHeight,
		                                                    int cursorOffsetX, int cursorOffsetY, Color rectangleColor1,
		                                                    Color rectangleColor2, int rectangleThickness,
		                                                    out int iconOffsetX, out int iconOffsetY)
		{
			double zoom = 1d;

			if (PintaCore.Workspace.HasOpenDocuments)
			{
				zoom = Math.Min(30d, PintaCore.Workspace.ActiveDocument.Workspace.Scale);
			}

			brushWidth = (int)Math.Min(800d, ((double)brushWidth) * zoom);
			cursorWidth = (int)((double)cursorWidth * zoom);
			cursorHeight = (int)((double)cursorHeight * zoom);


			int halfOfRectangleThickness = (int)Math.Ceiling(rectangleThickness / 2d);
			int halfOfBrushWidth = brushWidth / 2;

			int iconWidth = Math.Max(cursorWidth + cursorOffsetX + halfOfBrushWidth + halfOfRectangleThickness,
				brushWidth + rectangleThickness + (int)(16d / zoom));
			int iconHeight = Math.Max(cursorHeight + cursorOffsetY + halfOfBrushWidth + halfOfRectangleThickness,
				brushWidth + rectangleThickness + (int)(16d / zoom));

			iconOffsetX = Math.Max(cursorOffsetX, halfOfBrushWidth + halfOfRectangleThickness);
			iconOffsetY = Math.Max(cursorOffsetY, halfOfBrushWidth + halfOfRectangleThickness);

			ImageSurface i = new ImageSurface(Format.ARGB32, iconWidth, iconHeight);

			using (Context g = new Context(i))
			{
				g.DrawRectangle(new Rectangle(iconOffsetX - halfOfBrushWidth,
					iconOffsetY - halfOfBrushWidth,
					brushWidth,
					brushWidth),
					rectangleColor2, rectangleThickness);

				g.DrawRectangle(new Rectangle(iconOffsetX - halfOfBrushWidth + rectangleThickness,
					iconOffsetY - halfOfBrushWidth + rectangleThickness,
					brushWidth - rectangleThickness - 1,
					brushWidth - rectangleThickness - 1),
					rectangleColor1, rectangleThickness);

				g.DrawPixbuf(PintaCore.Resources.GetIcon(name),
					new Point(iconOffsetX - cursorOffsetX, iconOffsetY - cursorOffsetY));
			}

			return CairoExtensions.ToPixbuf(i);
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

			alphablending_button.AddItem (Catalog.GetString ("Normal Blending"), "Toolbar.BlendingEnabledIcon.png", true);
			alphablending_button.AddItem (Catalog.GetString ("Overwrite"), "Toolbar.BlendingOverwriteIcon.png", false);

			tb.AppendItem (alphablending_button);
		}

		private void BuildAntialiasingTool (Toolbar tb)
		{
			if (antialiasing_button != null) {
				tb.AppendItem (antialiasing_button);
				return;
			}

			antialiasing_button = new ToolBarDropDownButton ();

			antialiasing_button.AddItem (Catalog.GetString ("Antialiasing On"), "Toolbar.AntiAliasingEnabledIcon.png", true);
			antialiasing_button.AddItem (Catalog.GetString ("Antialiasing Off"), "Toolbar.AntiAliasingDisabledIcon.png", false);

			tb.AppendItem (antialiasing_button);
		}

		private void Workspace_CanvasSizeChanged(object sender, EventArgs e)
		{
			SetCursor (DefaultCursor);
		}
		#endregion
	}
}
