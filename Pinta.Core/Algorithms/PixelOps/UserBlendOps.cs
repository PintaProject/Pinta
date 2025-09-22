/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;

namespace Pinta.Core;

/// <summary>
/// This class contains all the render ops that can be used by the user
/// to configure a layer's blending mode. It also contains helper
/// functions to aid in enumerating and using these blend ops.
/// 
/// Credit for mathematical descriptions of many of the blend modes goes to
/// a page on Pegtop Software's website called, "Blend Modes"
/// http://www.pegtop.net/delphi/articles/blendmodes/
/// </summary>
public sealed partial class UserBlendOps
{
	private static readonly IReadOnlyDictionary<string, BlendMode> blend_modes;

	static UserBlendOps ()
	{
		blend_modes = new Dictionary<string, BlendMode> {
			[Translations.GetString ("Normal")] = BlendMode.Normal,
			[Translations.GetString ("Multiply")] = BlendMode.Multiply,
			[Translations.GetString ("Color Burn")] = BlendMode.ColorBurn,
			[Translations.GetString ("Color Dodge")] = BlendMode.ColorDodge,
			[Translations.GetString ("Overlay")] = BlendMode.Overlay,
			[Translations.GetString ("Difference")] = BlendMode.Difference,
			[Translations.GetString ("Lighten")] = BlendMode.Lighten,
			[Translations.GetString ("Darken")] = BlendMode.Darken,
			[Translations.GetString ("Screen")] = BlendMode.Screen,
			[Translations.GetString ("Xor")] = BlendMode.Xor,
			[Translations.GetString ("Hard Light")] = BlendMode.HardLight,
			[Translations.GetString ("Soft Light")] = BlendMode.SoftLight,
			[Translations.GetString ("Color")] = BlendMode.Color,
			[Translations.GetString ("Luminosity")] = BlendMode.Luminosity,
			[Translations.GetString ("Hue")] = BlendMode.Hue,
			[Translations.GetString ("Saturation")] = BlendMode.Saturation,
		};
	}

	private UserBlendOps ()
	{
	}

	public static IEnumerable<string> GetAllBlendModeNames ()
	{
		return blend_modes.Keys;
	}

	public static BlendMode GetBlendModeByName (string name)
	{
		return blend_modes[name];
	}

	public static string GetBlendModeName (BlendMode mode)
	{
		return blend_modes.Where (p => p.Value == mode).First ().Key;
	}
}
