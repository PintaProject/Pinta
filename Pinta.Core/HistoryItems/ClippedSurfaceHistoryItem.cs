// 
// ClippedSurfaceHistoryItem.cs
//  
// Author:
//       Olivier Dufour <Olivier (dot) duff [at] gmail (dot) com>
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
using System.IO;
using System.Collections.Generic;

namespace Pinta.Core
{
	public class ClippedSurfaceHistoryItem : BaseHistoryItem
	{
		IrregularSurface old_surface;
		int layer_index;

		public ClippedSurfaceHistoryItem (string icon, string text, IrregularSurface oldSurface, int layerIndex) : base (icon, text)
		{
			old_surface = (IrregularSurface)oldSurface.Clone();
			layer_index = layerIndex;
		}

		public ClippedSurfaceHistoryItem (string icon, string text) : base (icon, text)
		{
		}

		public override void Undo ()
		{
			// Grab the original surface
			IrregularSurface new_surf = new IrregularSurface(PintaCore.Layers[layer_index].Surface, old_surface.Region);
			
			// Undo to the "old" surface
			old_surface.Draw(PintaCore.Layers[layer_index].Surface);
			
			// Store the original surface for Redo
			old_surface = new_surf;
			
			PintaCore.Workspace.Invalidate (old_surface.Region.Clipbox);
		}

		public override void Redo ()
		{
			// Grab the original surface
			IrregularSurface new_surf = new IrregularSurface(PintaCore.Layers[layer_index].Surface, old_surface.Region);
			
			// Undo to the "old" surface
			old_surface.Draw(PintaCore.Layers[layer_index].Surface);
			
			// Store the original surface for Redo
			old_surface = new_surf;
			
			PintaCore.Workspace.Invalidate (old_surface.Region.Clipbox);
		}

		public override void Dispose ()
		{
			// Free up native surface
			(old_surface as IDisposable).Dispose ();
		}

		public override void LoadInternal (BinaryReader reader)
		{
			base.LoadInternal (reader);

			layer_index = reader.ReadInt32 ();
			int len = reader.ReadInt32 ();
			Gdk.Rectangle[] rects = new Gdk.Rectangle[len];
			for (int i = 0; i < len; i++)
				rects[i] = new Gdk.Rectangle (reader.ReadInt32 (), reader.ReadInt32 (), reader.ReadInt32 (), reader.ReadInt32 ());
			Gdk.Region roi = Utility.RectanglesToRegion(rects);

			List<PlacedSurface> lst = new List<PlacedSurface> ();
			len = reader.ReadInt32 ();
			for (int j = 0; j < len; j++)
			{
				int leng = reader.ReadInt32 ();
				int width = reader.ReadInt32 ();
				int height = reader.ReadInt32 ();
				int stride = reader.ReadInt32 ();
				ImageSurface surf = new ImageSurface (reader.ReadBytes (leng), Format.Argb32, width, height, stride);
				Gdk.Point p = new Gdk.Point(reader.ReadInt32 (), reader.ReadInt32 ());
				lst.Add ( new PlacedSurface () {What = surf, Where = p} );
			}
			old_surface = new IrregularSurface (lst, roi);
		}
		//TODO move code in to irregularsurface/imagesurface (extention)/placedsurface

		public override void Save (BinaryWriter writer)
		{
			base.Save (writer);
			writer.Write (layer_index);
			Gdk.Rectangle[] rects = old_surface.Region.GetRectangles();
			writer.Write (rects.Length);
			foreach (Gdk.Rectangle rect in rects)
			{
				writer.Write(rect.X);
				writer.Write(rect.Y);
				writer.Write(rect.Width);
				writer.Write(rect.Height);
			}

			writer.Write (old_surface.PlacedSurfaces.Count);
			foreach (PlacedSurface placedsurf in old_surface.PlacedSurfaces)
			{
				writer.Write (placedsurf.What.Data.Length);
				writer.Write (placedsurf.What.Width);
				writer.Write (placedsurf.What.Height);
				writer.Write (placedsurf.What.Stride);
				writer.Write (placedsurf.What.Data, 0, placedsurf.What.Data.Length);
				writer.Write (placedsurf.Where.X);
				writer.Write (placedsurf.Where.Y);
			}
		}

	}
}