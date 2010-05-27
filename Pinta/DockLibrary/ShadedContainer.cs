// 
// ShadedContainer.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Components;
using Pinta.Core;

namespace MonoDevelop.Components.Docking
{
	public class ShadedContainer
	{
		struct Section {
			public int Offset;
			public int Size;
		}
		
		Gdk.Color lightColor;
		Gdk.Color darkColor;
		int shadowSize = 2;
		
		List<Widget> widgets = new List<Widget> ();
		Dictionary<Widget, Gdk.Rectangle[]> allocations = new Dictionary<Widget, Gdk.Rectangle[]> ();
			
		public ShadedContainer ()
		{
		}
		
		public int ShadowSize {
			get { return this.shadowSize; }
			set { this.shadowSize = value; RedrawAll (); }
		}
		
		public Gdk.Color LightColor {
			get { return this.lightColor; }
			set { this.lightColor = value; RedrawAll (); }
		}

		public Gdk.Color DarkColor {
			get { return this.darkColor; }
			set { this.darkColor = value; RedrawAll (); }
		}
		
		public void Add (Gtk.Widget w)
		{
			widgets.Add (w);
			UpdateAllocation (w);
			w.Destroyed += HandleWDestroyed;
			w.Shown += HandleWShown;
			w.Hidden += HandleWHidden;
			w.SizeAllocated += HandleWSizeAllocated;
			w.Realized += HandleWRealized;
			IShadedWidget sw = w as IShadedWidget;
			if (sw != null)
				sw.AreasChanged += HandleSwAreasChanged;
			RedrawAll ();
		}

		public void Remove (Widget w)
		{
			widgets.Remove (w);
			allocations.Remove (w);
			w.Destroyed -= HandleWDestroyed;
			w.Shown -= HandleWShown;
			w.Hidden -= HandleWHidden;
			w.Realized -= HandleWRealized;
			IShadedWidget sw = w as IShadedWidget;
			if (sw != null)
				sw.AreasChanged -= HandleSwAreasChanged;
			RedrawAll ();
		}
		
		bool UpdateAllocation (Widget w)
		{
			if (w.IsRealized) {
				IShadedWidget sw = w as IShadedWidget;
				Gdk.Rectangle[] newAllocations;
				if (sw != null) {
					List<Gdk.Rectangle> rects = new List<Gdk.Rectangle> ();
					foreach (Gdk.Rectangle ar in sw.GetShadedAreas ())
						rects.Add (ar);
					newAllocations = rects.ToArray ();
				} else {
					newAllocations = new Gdk.Rectangle [] { w.Allocation };
				}
				Gdk.Rectangle[] oldAllocations;
				if (allocations.TryGetValue (w, out oldAllocations)) {
					if (oldAllocations.Length == newAllocations.Length) {
						bool changed = false;
						for (int n=0; n<oldAllocations.Length; n++) {
							if (newAllocations[n] != oldAllocations[n]) {
								changed = true;
								break;
							}
						}
						if (!changed)
							return false;
					}
				}
				allocations [w] = newAllocations;
				return true;
			}
			else {
				if (!allocations.ContainsKey (w))
					return false;
				allocations.Remove (w);
				return true;
			}
		}

		void HandleWRealized (object sender, EventArgs e)
		{
			if (UpdateAllocation ((Widget) sender))
				RedrawAll ();
		}

		void HandleSwAreasChanged (object sender, EventArgs e)
		{
			if (UpdateAllocation ((Gtk.Widget)sender))
				RedrawAll ();
		}
		
		void HandleWSizeAllocated (object o, SizeAllocatedArgs args)
		{
			if (UpdateAllocation ((Widget) o))
				RedrawAll ();
		}

		void HandleWHidden (object sender, EventArgs e)
		{
			RedrawAll ();
		}

		void HandleWShown (object sender, EventArgs e)
		{
			RedrawAll ();
		}

		void HandleWDestroyed (object sender, EventArgs e)
		{
			Remove ((Widget)sender);
		}
		
		void RedrawAll ()
		{
			foreach (Widget w in widgets) {
				if (!w.Visible)
					continue;
				IShadedWidget sw = w as IShadedWidget;
				if (sw != null) {
					foreach (Gdk.Rectangle rect in sw.GetShadedAreas ())
						w.QueueDrawArea (rect.X, rect.Y, rect.Width, rect.Height);
				}
				else
					w.QueueDraw ();
			}
		}
		
		public void DrawBackground (Gtk.Widget w)
		{
			DrawBackground (w, w.Allocation);
		}
		
		public void DrawBackground (Gtk.Widget w, Gdk.Rectangle allocation)
		{
			if (shadowSize == 0) {
				Gdk.Rectangle wr = new Gdk.Rectangle (allocation.X, allocation.Y, allocation.Width, allocation.Height);
				using (Cairo.Context ctx = Gdk.CairoHelper.Create (w.GdkWindow)) {
					ctx.Rectangle (wr.X, wr.Y, wr.Width, wr.Height);
					ctx.Color = lightColor.ToCairoColor ();
					ctx.Fill ();
				}
				return;
			}
			
			List<Section> secsT = new List<Section> ();
			List<Section> secsB = new List<Section> ();
			List<Section> secsR = new List<Section> ();
			List<Section> secsL = new List<Section> ();
			
			int x, y;
			w.GdkWindow.GetOrigin (out x, out y);
			Gdk.Rectangle rect = new Gdk.Rectangle (x + allocation.X, y + allocation.Y, allocation.Width, allocation.Height);
			
			Section s = new Section ();
			s.Size = rect.Width;
			secsT.Add (s);
			secsB.Add (s);
			s.Size = rect.Height;
			secsL.Add (s);
			secsR.Add (s);
				
			foreach (var rects in allocations) {
				int sx, sy;
				rects.Key.GdkWindow.GetOrigin (out sx, out sy);
				foreach (Gdk.Rectangle srt in rects.Value) {
					if (srt == rect)
						continue;
					Gdk.Rectangle sr = srt;
					sr.Offset (sx, sy);
					if (sr.Right == rect.X)
						RemoveSection (secsL, sr.Y - rect.Y, sr.Height);
					if (sr.Bottom == rect.Y)
						RemoveSection (secsT, sr.X - rect.X, sr.Width);
					if (sr.X == rect.Right)
						RemoveSection (secsR, sr.Y - rect.Y, sr.Height);
					if (sr.Y == rect.Bottom)
						RemoveSection (secsB, sr.X - rect.X, sr.Width);
				}
			}			
			
			Gdk.Rectangle r = new Gdk.Rectangle (allocation.X, allocation.Y, allocation.Width, allocation.Height);
			using (Cairo.Context ctx = Gdk.CairoHelper.Create (w.GdkWindow)) {
				ctx.Rectangle (r.X, r.Y, r.Width, r.Height);
				ctx.Color = lightColor.ToCairoColor ();
				ctx.Fill ();
				
				DrawShadow (ctx, r, PositionType.Left, secsL);
				DrawShadow (ctx, r, PositionType.Top, secsT);
				DrawShadow (ctx, r, PositionType.Right, secsR);
				DrawShadow (ctx, r, PositionType.Bottom, secsB);
			}
		}
		
		void DrawShadow (Cairo.Context ctx, Gdk.Rectangle ar, PositionType pos, List<Section> secs)
		{
			foreach (Section s in secs) {
				Cairo.Gradient pat = null;
				Gdk.Rectangle r = ar;
				switch (pos) {
					case PositionType.Top: 
						r.Height = shadowSize > r.Height ? r.Height / 2 : shadowSize;
						r.X += s.Offset;
						r.Width = s.Size;
						pat = new Cairo.LinearGradient (r.X, r.Y, r.X, r.Bottom);
						break;
					case PositionType.Bottom: 
						r.Y = r.Bottom - shadowSize;
						r.Height = shadowSize > r.Height ? r.Height / 2 : shadowSize;
						r.X = r.X + s.Offset;
						r.Width = s.Size;
						pat = new Cairo.LinearGradient (r.X, r.Bottom, r.X, r.Y);
						break;
					case PositionType.Left: 
						r.Width = shadowSize > r.Width ? r.Width / 2 : shadowSize; 
						r.Y += s.Offset;
						r.Height = s.Size;
						pat = new Cairo.LinearGradient (r.X, r.Y, r.Right, r.Y);
						break;
					case PositionType.Right: 
						r.X = r.Right - shadowSize;
						r.Width = shadowSize > r.Width ? r.Width / 2 : shadowSize; 
						r.Y += s.Offset;
						r.Height = s.Size;
						pat = new Cairo.LinearGradient (r.Right, r.Y, r.X, r.Y);
						break;
				}
				Cairo.Color c = darkColor.ToCairoColor ();
				pat.AddColorStop (0, c);
				c.A = 0;
				pat.AddColorStop (1, c);
				ctx.NewPath ();
				ctx.Rectangle (r.X, r.Y, r.Width, r.Height);
				ctx.Pattern = pat;
				ctx.Fill ();
				pat.Destroy ();
			}
		}
		
		void RemoveSection (List<Section> secs, int offset, int size)
		{
			if (offset < 0) {
				size += offset;
				offset = 0;
			}
			if (size <= 0 || secs.Count == 0)
				return;
			Section last = secs [secs.Count - 1];
			int rem = (last.Offset + last.Size) - (offset + size);
			if (rem < 0) {
				size += rem;
				if (size <= 0)
					return;
			}
			for (int n=0; n<secs.Count; n++) {
				Section s = secs [n];
				if (s.Offset >= offset + size)
					continue;
				if (offset >= s.Offset + s.Size)
					continue;
				if (offset <= s.Offset && offset + size >= s.Offset + s.Size) {
					// Remove the whole section
					secs.RemoveAt (n);
					n--;
					continue;
				}
				if (offset <= s.Offset) {
					int newOfs = offset + size;
					s.Size = s.Size - (newOfs - s.Offset);
					s.Offset = newOfs;
					secs [n] = s;
					// Nothing else to remove
					return;
				}
				if (offset + size >= s.Offset + s.Size) {
					s.Size = offset - s.Offset;
					secs [n] = s;
					continue;
				}
				// Split section
				Section s2 = new Section ();
				s2.Offset = offset + size;
				s2.Size = (s.Offset + s.Size) - (offset + size);
				secs.Insert (n + 1, s2);
				s.Size = offset - s.Offset;
				secs [n] = s;
			}
		}
	}
	
	public interface IShadedWidget
	{
		IEnumerable<Gdk.Rectangle> GetShadedAreas ();
		event EventHandler AreasChanged;
	}
}

