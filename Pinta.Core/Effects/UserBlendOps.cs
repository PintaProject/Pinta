/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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
	}
}
