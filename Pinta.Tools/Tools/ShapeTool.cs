// 
// ShapeTool.cs
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
using Pinta.Core;
using Mono.Unix;
using System.Collections.Generic;

namespace Pinta.Tools
{
	public class ShapeTool : BaseTool
	{
        protected BaseEditEngine editEngine;

		public ShapeTool()
		{
		}

		static ShapeTool()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("ShapeTool.Outline.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ShapeTool.Outline.png")));
			fact.Add ("ShapeTool.Fill.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ShapeTool.Fill.png")));
			fact.Add ("ShapeTool.OutlineFill.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ShapeTool.OutlineFill.png")));
			fact.AddDefault ();
		}
        
		#region Properties
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.O; } }
        protected override bool ShowAntialiasingButton { get { return true; } }
		#endregion

        protected override void OnBuildToolBar(Gtk.Toolbar tb)
        {
            base.OnBuildToolBar(tb);

            editEngine.HandleBuildToolBar(tb);
        }

		protected override void OnMouseDown(Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
            editEngine.HandleMouseDown(canvas, args, point);
		}

		protected override void OnMouseUp(Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
            editEngine.HandleMouseUp(canvas, args, point);
		}

		protected override void OnMouseMove(object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
            editEngine.HandleMouseMove(o, args, point);
		}

        protected override void OnActivated()
        {
            editEngine.HandleActivated();

            base.OnActivated();
        }

        protected override void OnDeactivated()
        {
            editEngine.HandleDeactivated();

            base.OnDeactivated();
        }

        protected override void OnCommit()
        {
            editEngine.HandleCommit();

            base.OnCommit();
        }

        protected override void OnKeyDown(Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
        {
            if (!editEngine.HandleKeyDown(canvas, args))
            {
                base.OnKeyDown(canvas, args);
            }
        }

        protected override void OnKeyUp(Gtk.DrawingArea canvas, Gtk.KeyReleaseEventArgs args)
        {
            if (!editEngine.HandleKeyUp(canvas, args))
            {
                base.OnKeyUp(canvas, args);
            }
        }

        public override void AfterUndo()
        {
            editEngine.HandleAfterUndo();

            base.AfterUndo();
        }

        public override void AfterRedo()
        {
            editEngine.HandleAfterRedo();

            base.AfterRedo();
        }
	}
}
