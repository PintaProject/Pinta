// 
// ToolKeyEventArgs.cs
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
using Gdk;
using Gtk;

namespace Pinta.Core
{
	public class ToolKeyEventArgs : EventArgs
	{
		public EventKey? Event { get; init; }

		/// <summary>
		/// Specifies whether the Alt key is currently pressed.
		/// </summary>
		public bool IsAltPressed => State.HasFlag (ModifierType.Mod1Mask);

		/// <summary>
		/// Specifies whether the Control key is currently pressed.
		/// </summary>
		public bool IsControlPressed => State.HasFlag (ModifierType.ControlMask);

		/// <summary>
		/// Specifies whether the left mouse button is currently pressed.
		/// </summary>
		public bool IsLeftMousePressed => State.HasFlag (ModifierType.Button1Mask);

		/// <summary>
		/// Specifies whether the right mouse button is currently pressed.
		/// </summary>
		public bool IsRightMousePressed => State.HasFlag (ModifierType.Button3Mask);

		/// <summary>
		/// Specifies whether the Shift key is currently pressed.
		/// </summary>
		public bool IsShiftPressed => State.HasFlag (ModifierType.ShiftMask);

		/// <summary>
		/// Specifies the key that has been pressed or released.
		/// </summary>
		public Gdk.Key Key { get; init; }

		public ModifierType State { get; init; }

		public static ToolKeyEventArgs FromKeyPressEventArgs (KeyPressEventArgs args)
		{
			return new ToolKeyEventArgs {
				Event = args.Event,
				Key = args.Event.Key,
				State = args.Event.State
			};
		}

		public static ToolKeyEventArgs FromKeyReleaseEventArgs (KeyReleaseEventArgs args)
		{
			return new ToolKeyEventArgs {
				Event = args.Event,
				Key = args.Event.Key,
				State = args.Event.State
			};
		}
	}
}
