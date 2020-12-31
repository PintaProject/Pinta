using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Gdk;
using Gtk;

namespace Pinta.Core
{
	public class ToolKeyEventArgs : HandledEventArgs
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
				Key = args.Event.Key,
				State = args.Event.State
			};
		}

		public static ToolKeyEventArgs FromKeyReleaseEventArgs (KeyReleaseEventArgs args)
		{
			return new ToolKeyEventArgs {
				Key = args.Event.Key,
				State = args.Event.State
			};
		}
	}
}
