/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Unix;

namespace Pinta.Core
{
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
		private static Dictionary<string, BlendMode> blend_modes = new Dictionary<string,BlendMode> ();

		static UserBlendOps ()
		{
			blend_modes.Add (Catalog.GetString ("Normal"), BlendMode.Normal);
			blend_modes.Add (Catalog.GetString ("Multiply"), BlendMode.Multiply);
			blend_modes.Add (Catalog.GetString ("Color Burn"), BlendMode.ColorBurn);
			blend_modes.Add (Catalog.GetString ("Color Dodge"), BlendMode.ColorDodge);
			blend_modes.Add (Catalog.GetString ("Overlay"), BlendMode.Overlay);
			blend_modes.Add (Catalog.GetString ("Difference"), BlendMode.Difference);
			blend_modes.Add (Catalog.GetString ("Lighten"), BlendMode.Lighten);
			blend_modes.Add (Catalog.GetString ("Darken"), BlendMode.Darken);
			blend_modes.Add (Catalog.GetString ("Screen"), BlendMode.Screen);
            blend_modes.Add (Catalog.GetString ("Xor"), BlendMode.Xor);
            blend_modes.Add (Catalog.GetString ("Hard Light"), BlendMode.HardLight);
            blend_modes.Add (Catalog.GetString ("Soft Light"), BlendMode.SoftLight);
            blend_modes.Add (Catalog.GetString ("Color"), BlendMode.Color);
            blend_modes.Add (Catalog.GetString ("Luminosity"), BlendMode.Luminosity);
            blend_modes.Add (Catalog.GetString ("Hue"), BlendMode.Hue);
            blend_modes.Add (Catalog.GetString ("Saturation"), BlendMode.Saturation);
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
}
