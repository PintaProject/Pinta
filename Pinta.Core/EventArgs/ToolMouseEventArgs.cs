// 
// ToolMouseEventArgs.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2020 Jonathan Pobst
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
using Gdk;
using Gtk;

namespace Pinta.Core
{
	public class ToolMouseEventArgs : EventArgs
	{
		/// <summary>
		/// Specifies whether the Alt key is currently pressed.
		/// </summary>
		public bool IsAltPressed => State.IsAltPressed ();

		/// <summary>
		/// Specifies whether the Control key is currently pressed.
		/// </summary>
		public bool IsControlPressed => State.IsControlPressed ();

		/// <summary>
		/// Specifies whether the left mouse button is currently pressed.
		/// </summary>
		public bool IsLeftMousePressed => State.IsLeftMousePressed ();

		/// <summary>
		/// Specifies whether the right mouse button is currently pressed.
		/// </summary>
		public bool IsRightMousePressed => State.IsRightMousePressed ();

		/// <summary>
		/// Specifies whether the Shift key is currently pressed.
		/// </summary>
		public bool IsShiftPressed => State.IsShiftPressed ();

		public ModifierType State { get; init; }

		/// <summary>
		/// The mouse button being pressed or released, when applicable.
		/// </summary>
		public MouseButton MouseButton { get; init; }

		/// <summary>
		/// The cursor location in canvas coordinates.
		/// </summary>
		public PointI Point => new ((int) PointDouble.X, (int) PointDouble.Y);

		/// <summary>
		/// The cursor location in canvas coordinates.
		/// </summary>
		public PointD PointDouble { get; init; }

		/// <summary>
		/// The cursor location in the root canvas window.
		/// Unlike WindowPoint, this is outside the scroll area and remains the same while scrolling.
		/// </summary>
		public PointD RootPoint { get; init; }

		/// <summary>
		/// The cursor location in window coordinates.
		/// </summary>
		public PointD WindowPoint { get; init; }
	}
}
