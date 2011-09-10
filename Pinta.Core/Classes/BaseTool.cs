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
			SetCursor (DefaultCursor);
		}
		
		protected virtual void OnDeactivated ()
		{
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
		
		protected void SetCursor (Gdk.Cursor cursor)
		{
			//PintaCore.Chrome.DrawingArea.GdkWindow.Cursor = cursor;
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
		#endregion
	}
}
