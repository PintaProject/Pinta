// 
// DialogAttributes.cs
//  
// Initial version from:
// http://github.com/migueldeicaza/MonoTouch.Dialog/blob/master/MonoTouch.Dialog/Reflect.cs
// 
// Author:
//	 Miguel de Icaza (miguel@gnome.org)
//
// Copyright 2010, Novell, Inc.
//
// Code licensed under the MIT X11 license

using System;

namespace Pinta.Gui.Widgets
{
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class SkipAttribute : Attribute
	{
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class CaptionAttribute : Attribute
	{
		public CaptionAttribute (string caption) => Caption = caption;

		public string Caption { get; set; }
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class DigitsValueAttribute : Attribute
	{
		public DigitsValueAttribute (int value) => Value = value;

		public int Value { get; set; }
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class IncrementValueAttribute : Attribute
	{
		public IncrementValueAttribute (double value) => Value = value;

		public double Value { get; set; }
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class MinimumValueAttribute : Attribute
	{
		public MinimumValueAttribute (int value) => Value = value;

		public int Value { get; set; }
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class MaximumValueAttribute : Attribute
	{
		public MaximumValueAttribute (int value) => Value = value;

		public int Value { get; set; }
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class HintAttribute : Attribute
	{
		public HintAttribute (string caption) => Hint = caption;

		public string Hint { get; set; }
	}

	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class StaticListAttribute : Attribute
	{
		public StaticListAttribute (string dict) => DictionaryName = dict;

		public string DictionaryName { get; set; }
	}
}
