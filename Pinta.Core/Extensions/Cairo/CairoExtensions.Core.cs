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

// Some functions are from Paint.NET:

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core;

// For core utilities and fundamental operations directly tied to Cairo functionality
// but not fitting specific themes like geometry or drawing
partial class CairoExtensions
{
	public static ImageSurface Clone (this ImageSurface surf)
	{
		if (PintaCore.Workspace.HasOpenDocuments)
			PintaCore.Workspace.ActiveDocument.SignalSurfaceCloned ();

		ImageSurface newsurf = CreateImageSurface (
			surf.Format,
			surf.Width,
			surf.Height);

		using Context g = new (newsurf);

		g.SetSourceSurface (surf, 0, 0);
		g.Paint ();

		return newsurf;
	}

	public static Path Clone (this Path path)
	{
		Document doc = PintaCore.Workspace.ActiveDocument;
		using Context g = new (doc.Layers.CurrentUserLayer.Surface);
		g.AppendPath (path);
		return g.CopyPath ();
	}

	/// <summary>
	/// Wrapper method to create an ImageSurface and handle allocation failures.
	/// </summary>
	public static ImageSurface CreateImageSurface (
		Format format,
		int width,
		int height)
	{
		ImageSurface surf = new (format, width, height);

		if (surf == null || surf.Status == Cairo.Status.NoMemory)
			throw new OutOfMemoryException ("Unable to allocate memory for image");

		return surf;
	}

	public static ImageSurface CreateTransparentBackgroundSurface (int size)
	{
		ImageSurface surface = CreateImageSurface (Format.Argb32, size, size);

		// Draw the checkerboard
		using Context g = new (surface);

		// Fill white
		g.FillRectangle (new RectangleD (0, 0, size, size), new Color (1, 1, 1));

		Color color = new (0.78, 0.78, 0.78);
		int half_size = size / 2;

		// Draw gray squares
		g.FillRectangle (new RectangleD (0, 0, half_size, half_size), color);
		g.FillRectangle (new RectangleD (half_size, half_size, half_size, half_size), color);

		return surface;
	}

	public static void SetSourceSurface (
		this Context g,
		Surface surface,
		ResamplingMode resamplingMode)
	{
		SurfacePattern src_pattern = new (surface) {
			Filter = resamplingMode.ToCairoFilter (),
		};

		g.SetSource (src_pattern);
	}
}
