using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Gdk;

namespace Pinta.Core
{
	public class ToolMouseEventArgs : EventArgs
	{
		/// <summary>
		/// Specifies whether the Alt key is currently pressed.
		/// </summary>
		public bool IsAltPressed => State.HasFlag (ModifierType.Mod1Mask);

		/// <summary>
		/// Specifies whether the Control key is currently pressed.
		/// </summary>
		public bool IsControlPressed => State.HasFlag (ModifierType.ControlMask);

		/// <summary>
		/// Specifies whether the Shift key is currently pressed.
		/// </summary>
		public bool IsShiftPressed => State.HasFlag (ModifierType.ShiftMask);

		public ModifierType State { get; init; }

		/// <summary>
		/// The mouse button being pressed or released, when applicable.
		/// </summary>
		public MouseButton MouseButton { get; init; }

		/// <summary>
		/// The cursor location in canvas coordinates.
		/// </summary>
		public Cairo.Point Point => new Cairo.Point ((int)PointDouble.X, (int)PointDouble.Y);

		/// <summary>
		/// The cursor location in canvas coordinates.
		/// </summary>
		public PointD PointDouble { get; init; }

		public PointD Root { get; init; }

		/// <summary>
		/// The cursor location in window coordinates.
		/// </summary>
		public PointD WindowPoint { get; init; }
	}

	public enum MouseButton
	{
		None,
		Left,
		Middle,
		Right
	}
}
