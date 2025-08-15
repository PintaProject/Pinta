// 
// Palette.cs
//  
// Author:
//       Maia Kozheva <sikon@ubuntu.com>
// 
// Copyright (c) 2010 Maia Kozheva <sikon@ubuntu.com>
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cairo;

namespace Pinta.Core;

public sealed class Palette
{
	private readonly List<Color> colors;
	public ReadOnlyCollection<Color> Colors { get; }

	public Palette (IEnumerable<Color> initialColors)
	{
		List<Color> backing = [.. initialColors];
		colors = backing;
		Colors = new ReadOnlyCollection<Color> (backing);
	}

	public event EventHandler? PaletteChanged;

	private void OnPaletteChanged ()
	{
		PaletteChanged?.Invoke (this, EventArgs.Empty);
	}

	public void SetColor (int index, Color value)
	{
		colors[index] = value;
		OnPaletteChanged ();
	}

	public void Resize (int newSize)
	{
		int difference = newSize - Colors.Count;

		if (difference > 0) {
			for (int i = 0; i < difference; i++)
				colors.Add (new Color (1, 1, 1));
		} else {
			colors.RemoveRange (newSize, -difference);
		}

		colors.TrimExcess ();
		OnPaletteChanged ();
	}

	/// <summary>
	/// Replaces existing colors with new colors
	/// </summary>
	public void Load (IEnumerable<Color> newColors)
	{
		colors.Clear ();
		colors.AddRange (newColors);
		colors.TrimExcess ();
		OnPaletteChanged ();
	}
}
