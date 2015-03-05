//
// DockVisualStyle.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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

namespace MonoDevelop.Components.Docking
{
	public sealed class DockVisualStyle
	{
		public Gdk.Color? PadBackgroundColor { get; set; }
		public Gdk.Color? PadTitleLabelColor { get; set; }
		public DockTabStyle? TabStyle { get; set; }
		public Gdk.Color? TreeBackgroundColor { get; set; }
		public bool? ShowPadTitleIcon { get; set; }
		public bool? UppercaseTitles { get; set; }
		public bool? ExpandedTabs { get; set; }
		public Gdk.Color? InactivePadBackgroundColor { get; set; }
		public int? PadTitleHeight { get; set; }

		// When set, pads in a region with this style can't be stacked horizontally
		public bool? SingleColumnMode { get; set; }

		// When set, pads in a region with this style can't be stacked vertically
		public bool? SingleRowMode { get; set; }

		public DockVisualStyle ()
		{
		}

		public DockVisualStyle Clone ()
		{
			return (DockVisualStyle) MemberwiseClone ();
		}

		public void CopyValuesFrom (DockVisualStyle style)
		{
			if (style.PadBackgroundColor != null)
				PadBackgroundColor = style.PadBackgroundColor;
			if (style.PadTitleLabelColor != null)
				PadTitleLabelColor = style.PadTitleLabelColor;
			if (style.TabStyle != null)
				TabStyle = style.TabStyle;
			if (style.TreeBackgroundColor != null)
				TreeBackgroundColor = style.TreeBackgroundColor;
			if (style.ShowPadTitleIcon != null)
				ShowPadTitleIcon = style.ShowPadTitleIcon;
			if (style.UppercaseTitles != null)
				UppercaseTitles = style.UppercaseTitles;
			if (style.ExpandedTabs != null)
				ExpandedTabs = style.ExpandedTabs;
			if (style.InactivePadBackgroundColor != null)
				InactivePadBackgroundColor = style.InactivePadBackgroundColor;
			if (style.PadTitleHeight != null)
				PadTitleHeight = style.PadTitleHeight;
			if (style.SingleColumnMode != null)
				SingleColumnMode = style.SingleColumnMode;
			if (style.SingleRowMode != null)
				SingleRowMode = style.SingleRowMode;
		}

		public static DockVisualStyle CreateDefaultStyle ()
		{
			DockVisualStyle s = new DockVisualStyle ();
			s.PadBackgroundColor = new Gdk.Color (0,0,0);
			s.PadTitleLabelColor = new Gdk.Color (0,0,0);
			s.TabStyle = DockTabStyle.Normal;
			s.TreeBackgroundColor = null;
			s.ShowPadTitleIcon = true;
			s.UppercaseTitles = false;
			s.ExpandedTabs = false;
			s.InactivePadBackgroundColor = new Gdk.Color (0,0,0);
			s.PadTitleHeight = -1;
			s.SingleRowMode = false;
			s.SingleColumnMode = false;
			return s;
		}
	}

	public enum DockTabStyle
	{
		Normal,
		Simple
	}
}

