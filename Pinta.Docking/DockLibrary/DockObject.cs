//
// DockObject.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Xml;
using Gtk;
using System.Globalization;

namespace MonoDevelop.Components.Docking
{
	internal abstract class DockObject
	{
		DockGroup parentGroup;
		DockFrame frame;
		Gdk.Rectangle rect;

		// The current size in pixels of this item
		double size = -1;

		// The current size in pixels of this item, but as an integer.
		// In general it is the same value as size, but it may change a bit due to rounding.
		int allocSize = -1;
		
		double defaultHorSize = -1;
		double defaultVerSize = -1;
		double prefSize = 0;
		
		// Those are the last known coordinates of the item. They are stored in StoreAllocation
		// and restored to rect in RestoreAllocation. This is needed for example when a layout
		// is cloned. It is convenient to have allocation information in the cloned layout, even
		// if the layout has never been displayed (e.g., to decide the autohide dock location)
		int ax=-1, ay=-1;
		
		public DockObject (DockFrame frame)
		{
			this.frame = frame;
		}
		
		internal DockGroup ParentGroup {
			get {
				return parentGroup;
			}
			set {
				parentGroup = value;
				if (size < 0)
					size = prefSize = DefaultSize;
			}
		}

		public double Size {
			get {
				return size;
			}
			set {
				size = value;
			}
		}
		
		public bool HasAllocatedSize {
			get { return allocSize != -1; }
		}
		
		public double DefaultSize {
			get {
				if (defaultHorSize < 0)
					InitDefaultSizes ();
				if (parentGroup != null) {
					if (parentGroup.Type == DockGroupType.Horizontal)
						return defaultHorSize;
					else if (parentGroup.Type == DockGroupType.Vertical)
						return defaultVerSize;
				}
				return 0;
			}
			set {
				if (parentGroup != null) {
					if (parentGroup.Type == DockGroupType.Horizontal)
						defaultHorSize = value;
					else if (parentGroup.Type == DockGroupType.Vertical)
						defaultVerSize = value;
				}
			}
		}

		public DockVisualStyle VisualStyle { get; set; }
		
		internal void ResetDefaultSize ()
		{
			defaultHorSize = -1;
			defaultVerSize = -1;
		}

		public int MinSize {
			get {
				int w,h;
				GetMinSize (out w, out h);
				if (parentGroup != null) {
					if (parentGroup.Type == DockGroupType.Horizontal)
						return w;
					else if (parentGroup.Type == DockGroupType.Vertical)
						return h;
				}
				return w;
			}
		}
		
		public abstract bool Expand { get; }
		
		public virtual void SizeAllocate (Gdk.Rectangle rect)
		{
			this.rect = rect;
		}

		internal Gdk.Rectangle Allocation {
			get {
				return rect;
			}
			set {
				rect = value; 
			}
		}

		public int AllocSize {
			get {
				return allocSize;
			}
			set {
				allocSize = value;
			}
		}
		
		public MonoDevelop.Components.Docking.DockFrame Frame {
			get {
				return frame;
			}
		}

		public double PrefSize {
			get {
				return prefSize;
			}
			set {
				prefSize = value;
			}
		}

		void InitDefaultSizes ()
		{
			int width, height;
			GetDefaultSize (out width, out height);
			if (width == -1)
				width = frame.DefaultItemWidth;
			if (height == -1)
				height = frame.DefaultItemHeight;
			defaultHorSize = (double) width;
			defaultVerSize = (double) height;
		}
		
		internal virtual void GetDefaultSize (out int width, out int height)
		{
			width = -1;
			height = -1;
		}

		internal virtual void GetMinSize (out int width, out int height)
		{
			width = 0;
			height = 0;
		}
			
		internal abstract void QueueResize ();
		
		internal abstract bool GetDockTarget (DockItem item, int px, int py, out DockDelegate dockDelegate, out Gdk.Rectangle rect);
		
		internal abstract Gtk.Requisition SizeRequest ();
		
		internal abstract bool Visible { get; }

		internal abstract void Dump (int ind);
		
		internal virtual void RestoreAllocation ()
		{
			if (parentGroup != null) {
				int x = ax != -1 ? ax : 0;
				int y = ay != -1 ? ay : 0;
				if (parentGroup.Type == DockGroupType.Horizontal)
					rect = new Gdk.Rectangle (x, y, (int)size, parentGroup.Allocation.Height);
				else if (parentGroup.Type == DockGroupType.Vertical)
					rect = new Gdk.Rectangle (x, y, parentGroup.Allocation.Width, (int)size);
			}
		}
		
		internal virtual void StoreAllocation ()
		{
			if (Visible) {
				if (parentGroup == null || parentGroup.Type == DockGroupType.Horizontal)
					size = prefSize = (int) rect.Width;
				else if (parentGroup.Type == DockGroupType.Vertical)
					size = prefSize = (int) rect.Height;
				ax = Allocation.X;
				ay = Allocation.Y;
			}
		}
		
		internal virtual void Write (XmlWriter writer)
		{
			writer.WriteAttributeString ("size", size.ToString (CultureInfo.InvariantCulture));
			writer.WriteAttributeString ("prefSize", prefSize.ToString (CultureInfo.InvariantCulture));
			writer.WriteAttributeString ("defaultHorSize", defaultHorSize.ToString (CultureInfo.InvariantCulture));
			writer.WriteAttributeString ("defaultVerSize", defaultVerSize.ToString (CultureInfo.InvariantCulture));
		}
		
		internal virtual void Read (XmlReader reader)
		{
			size = double.Parse (reader.GetAttribute ("size"), CultureInfo.InvariantCulture);
			prefSize = double.Parse (reader.GetAttribute ("prefSize"), CultureInfo.InvariantCulture);
			defaultHorSize = double.Parse (reader.GetAttribute ("defaultHorSize"), CultureInfo.InvariantCulture);
			defaultVerSize = double.Parse (reader.GetAttribute ("defaultVerSize"), CultureInfo.InvariantCulture);
		}
		
		public virtual void CopyFrom (DockObject ob)
		{
			parentGroup = null;
			frame = ob.frame;
			rect = ob.rect;
			size = ob.size;
			allocSize = ob.allocSize;
			defaultHorSize = ob.defaultHorSize;
			defaultVerSize = ob.defaultVerSize;
			prefSize = ob.prefSize;
		}
		
		public DockObject Clone ()
		{
			DockObject ob = (DockObject) this.MemberwiseClone ();
			ob.CopyFrom (this);
			return ob;
		}
		
		public virtual void CopySizeFrom (DockObject obj)
		{
			size = obj.size;
			allocSize = obj.allocSize;
			defaultHorSize = obj.defaultHorSize;
			defaultVerSize = obj.defaultVerSize;
			prefSize = obj.prefSize;
		}
		
		public virtual bool IsNextToMargin (Gtk.PositionType margin, bool visibleOnly)
		{
			if (ParentGroup == null)
				return true;
			if (!ParentGroup.IsNextToMargin (margin, visibleOnly))
				return false;
			return ParentGroup.IsChildNextToMargin (margin, this, visibleOnly);
		}
	}
}
