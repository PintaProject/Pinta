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
		private static UserBlendOp[] cached_ops;
		private static Dictionary<string, BlendMode> blend_modes = new Dictionary<string,BlendMode> ();

		static UserBlendOps ()
		{
			cached_ops = new UserBlendOp[] {
				new NormalBlendOp (),
				new MultiplyBlendOp (),
				new AdditiveBlendOp (),
				new ColorBurnBlendOp (),
				new ColorDodgeBlendOp (),
				new ReflectBlendOp (),
				new GlowBlendOp (),
				new OverlayBlendOp (),
				new DifferenceBlendOp (),
				new NegationBlendOp (),
				new LightenBlendOp (),
				new DarkenBlendOp (),
				new ScreenBlendOp (),
				new XorBlendOp ()
			};

			blend_modes.Add (Catalog.GetString ("Normal"), BlendMode.Normal);
			blend_modes.Add (Catalog.GetString ("Multiply"), BlendMode.Multiply);
			blend_modes.Add (Catalog.GetString ("Additive"), BlendMode.Additive);
			blend_modes.Add (Catalog.GetString ("Color Burn"), BlendMode.ColorBurn);
			blend_modes.Add (Catalog.GetString ("Color Dodge"), BlendMode.ColorDodge);
			blend_modes.Add (Catalog.GetString ("Reflect"), BlendMode.Reflect);
			blend_modes.Add (Catalog.GetString ("Glow"), BlendMode.Glow);
			blend_modes.Add (Catalog.GetString ("Overlay"), BlendMode.Overlay);
			blend_modes.Add (Catalog.GetString ("Difference"), BlendMode.Difference);
			blend_modes.Add (Catalog.GetString ("Negation"), BlendMode.Negation);
			blend_modes.Add (Catalog.GetString ("Lighten"), BlendMode.Lighten);
			blend_modes.Add (Catalog.GetString ("Darken"), BlendMode.Darken);
			blend_modes.Add (Catalog.GetString ("Screen"), BlendMode.Screen);
			blend_modes.Add (Catalog.GetString ("Xor"), BlendMode.Xor);
		}

		private UserBlendOps ()
		{
		}

		/// <summary>
		/// Returns an array of Type objects that lists all of the pixel ops contained
		/// within this class. You can then use Utility.GetStaticName to retrieve the
		/// value of the StaticName property.
		/// </summary>
		/// <returns></returns>
		public static Type[] GetBlendOps ()
		{
			Type[] allTypes = typeof (UserBlendOps).GetNestedTypes ();
			List<Type> types = new List<Type> (allTypes.Length);

			foreach (Type type in allTypes) {
				if (type.IsSubclassOf (typeof (UserBlendOp)) && !type.IsAbstract) {
					types.Add (type);
				}
			}

			return types.ToArray ();
		}

		public static string GetBlendOpFriendlyName (Type opType)
		{
			return Utility.GetStaticName (opType);
		}

		public static UserBlendOp CreateBlendOp (Type opType)
		{
			ConstructorInfo ci = opType.GetConstructor (System.Type.EmptyTypes);
			UserBlendOp op = (UserBlendOp)ci.Invoke (null);
			return op;
		}

		public static UserBlendOp CreateDefaultBlendOp ()
		{
			return new NormalBlendOp ();
		}

		public static Type GetDefaultBlendOp ()
		{
			return typeof (NormalBlendOp);
		}

		public static UserBlendOp GetBlendOp (BlendMode mode, double opacity)
		{
			if (opacity == 1.0)
				return cached_ops[(int)mode];

			return cached_ops[(int)mode].CreateWithOpacity ((int)(opacity * 255));
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
