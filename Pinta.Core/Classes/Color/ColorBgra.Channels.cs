using System;

namespace Pinta.Core;

partial struct ColorBgra
{
	public const int BLUE_CHANNEL = 0;
	public const int GREEN_CHANNEL = 1;
	public const int RED_CHANNEL = 2;
	public const int ALPHA_CHANNEL = 3;

	public const int SizeOf = 4;

	/// <summary>
	/// Gets or sets the byte value of the specified color channel.
	/// </summary>
	public unsafe byte this[int channel] {
		get {
			if (channel < 0 || channel > 3)
				throw new ArgumentOutOfRangeException (nameof (channel), channel, "valid range is [0,3]");

			fixed (byte* p = &B)
				return p[channel];
		}

		set {
			if (channel < 0 || channel > 3)
				throw new ArgumentOutOfRangeException (nameof (channel), channel, "valid range is [0,3]");

			fixed (byte* p = &B)
				p[channel] = value;
		}
	}
}
