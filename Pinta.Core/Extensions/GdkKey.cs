namespace Gdk;

using Pinta.Core;

/// <summary>
/// Wrapper for the uint key values, e.g. Gdk.Constants.Key_Delete
/// </summary>
public readonly record struct Key (uint Value)
{
	public static Key Invalid { get; } = new (Gdk.Constants.KEY_VoidSymbol);

	public Key ToUpper ()
		=> new (Gdk.Functions.KeyvalToUpper (Value));

	/// <summary>
	/// Returns whether this key is a Ctrl key (or the Cmd key on macOS).
	/// </summary>
	public bool IsControlKey ()
	{
		if (PintaCore.System.OperatingSystem == OS.Mac)
			return Value == Gdk.Constants.KEY_Meta_L || Value == Gdk.Constants.KEY_Meta_R;
		else
			return Value == Gdk.Constants.KEY_Control_L || Value == Gdk.Constants.KEY_Control_R;
	}
}
