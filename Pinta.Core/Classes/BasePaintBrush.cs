//
// BasePaintBrush.cs
//  
// Author:
//       Aaron Bockover <abockover@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using Mono.Addins;
using Cairo;

namespace Pinta.Core
{
	/// <summary>
	/// The base class for all brushes.
	/// </summary>
	[TypeExtensionPoint]
	public abstract class BasePaintBrush
	{
		private static Random random = new Random ();

		/// <summary>
		/// The name of the brush.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Priority value for ordering brushes. If the priority is zero, then
		/// alphabetical ordering is used.
		/// </summary>
		public virtual int Priority {
			get { return 0; }
		}

		/// <summary>
		/// Random number generator. This can be used to implement brushes with
		/// random effects.
		/// </summary>
		public Random Random {
			get { return random; }
		}

		/// <summary>
		/// Used to multiply the alpha value of the stroke color by a
		/// constant factor.
		/// </summary>
		public virtual double StrokeAlphaMultiplier {
			get { return 1; }
		}

		public virtual void DoMouseUp ()
		{
			OnMouseUp ();
		}

		public virtual void DoMouseDown ()
		{
			OnMouseDown ();
		}

		public virtual Gdk.Rectangle DoMouseMove (Context g, Color strokeColor, ImageSurface surface,
		                                          int x, int y, int lastX, int lastY)
		{
			return OnMouseMove (g, strokeColor, surface, x, y, lastX, lastY);
		}

		/// <summary>
		/// Event handler called when the mouse is released.
		/// </summary>
		protected virtual void OnMouseUp ()
		{
		}

		/// <summary>
		/// Event handler called when the mouse is pressed down.
		/// </summary>
		protected virtual void OnMouseDown ()
		{
		}

		/// <summary>
		/// Event handler called when the mouse is moved. This method is where
		/// the brush should perform its drawing.
		/// </summary>
		/// <param name="g">The current Cairo drawing context.</param>
		/// <param name="strokeColor">The current stroke color.</param>
		/// <param name="surface">Image surface to draw on.</param>
		/// <param name="x">The current x coordinate of the mouse.</param>
		/// <param name="y">The current y coordinate of the mouse.</param>
		/// <param name="lastX">The previous x coordinate of the mouse.</param>
		/// <param name="lastY">The previous y coordinate of the mouse.</param>
		/// <returns>A rectangle containing the area of the canvas that should be redrawn.</returns>
		protected abstract Gdk.Rectangle OnMouseMove (Context g, Color strokeColor, ImageSurface surface,
		                                              int x, int y, int lastX, int lastY);
	}
}