// 
// PaintBucketTool.cs
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
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	public class PaintBucketTool : FloodTool
	{
		private Color fill_color;
		
		public override string Name {
			get { return Catalog.GetString ("Paint Bucket"); }
		}
		public override string Icon {
			get { return "Tools.PaintBucket.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Left click to fill a region with the primary color, right click to fill with the secondary color."); }
		}
		public override Gdk.Cursor DefaultCursor {
            get { return new Gdk.Cursor (Gdk.Display.Default, PintaCore.Resources.GetIcon ("Cursor.PaintBucket.png"), 21, 21); }
		}
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.F; } }
		public override int Priority { get { return 21; } }
		protected override bool CalculatePolygonSet { get { return false; } }

		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, PointD point)
		{
			if (args.Event.Button == 1)
				fill_color = PintaCore.Palette.PrimaryColor;
			else
				fill_color = PintaCore.Palette.SecondaryColor;
			
			base.OnMouseDown (canvas, args, point);
		}

		protected unsafe override void OnFillRegionComputed (IBitVector2D stencil)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			ImageSurface surf = doc.ToolLayer.Surface;

			using (var g = new Context (surf)) {
				g.Operator = Operator.Source;
				g.SetSource (doc.CurrentUserLayer.Surface);
				g.Paint ();
			}

			SimpleHistoryItem hist = new SimpleHistoryItem (Icon, Name);
			hist.TakeSnapshotOfLayer (doc.CurrentUserLayer);

			ColorBgra color = fill_color.ToColorBgra ().ToPremultipliedAlpha ();
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;
			int width = surf.Width;

			surf.Flush ();

			// Color in any pixel that the stencil says we need to fill
			Parallel.For (0, stencil.Height, y =>
			{
				int stencil_width = stencil.Width;
				for (int x = 0; x < stencil_width; ++x) {
					if (stencil.GetUnchecked (x, y)) {
						surf.SetColorBgraUnchecked (dstPtr, width, color, x, y);
					}
				}
			});

			surf.MarkDirty ();

			// Transfer the temp layer to the real one,
			// respecting any selection area
			using (var g = doc.CreateClippedContext ()) {
				g.Operator = Operator.Source;
				g.SetSource (surf);
				g.Paint ();
			}

			doc.ToolLayer.Clear ();

			doc.History.PushNewItem (hist); 
			doc.Workspace.Invalidate ();
		}
	}
}
