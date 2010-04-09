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

namespace Pinta.Core
{
	public abstract class BaseTool
	{
		protected const int DEFAULT_BRUSH_WIDTH = 2;
	    
		protected static Point point_empty = new Point (-500, -500);
	    
		protected ToggleToolButton tool_item;
		protected ToolItem tool_label;
		protected ToolItem tool_image;
		protected ToolItem tool_sep;
		
		protected BaseTool ()
		{
		}
		
		public virtual string Name { get { throw new ApplicationException ("Tool didn't override Name"); } }
		public virtual string Icon { get { throw new ApplicationException ("Tool didn't override Icon"); } }		
		public virtual string ToolTip { get { throw new ApplicationException ("Tool didn't override ToolTip"); } }
		public virtual string StatusBarText { get { return string.Empty; } }
		public virtual ToggleToolButton ToolItem { get { if (tool_item == null) tool_item = CreateToolButton (); return tool_item; } }
		public virtual bool Enabled { get { return true; } }
		public virtual Gdk.Cursor DefaultCursor { get { return null; } }
		public virtual Gdk.Key ShortcutKey { get { return (Gdk.Key)0; } }
		
		#region Public Methods
		public void DoMouseMove (object o, MotionNotifyEventArgs args, Cairo.PointD point)
		{
			OnMouseMove (o, args, point);
		}

		public void DoBuildToolBar (Toolbar tb)
		{
			OnBuildToolBar (tb);
		}

		public void DoClearToolBar (Toolbar tb)
		{
			OnClearToolBar (tb);
		}

		public void DoMouseDown (DrawingArea canvas, ButtonPressEventArgs args, Cairo.PointD point)
		{
			OnMouseDown (canvas, args, point);
		}

		public void DoMouseUp (DrawingArea canvas, ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			OnMouseUp (canvas, args, point);
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

		protected virtual void OnBuildToolBar (Toolbar tb)
		{
			if (tool_label == null)
				tool_label = new ToolBarLabel (" Tool:  ");
			
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
				tool_item.TooltipText = string.Format ("{0}\nShortcut key: {1}", Name, ShortcutKey.ToString ().ToUpperInvariant ());
			else
				tool_item.TooltipText = Name;
			
			return tool_item;
		}
		
		protected void SetCursor (Gdk.Cursor cursor)
		{
			PintaCore.Chrome.DrawingArea.GdkWindow.Cursor = cursor;
		}
		#endregion
	}
}
