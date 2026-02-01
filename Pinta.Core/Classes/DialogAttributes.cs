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

namespace Pinta.Core;

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class SkipAttribute : Attribute
{
}

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class CaptionAttribute : Attribute
{
	public CaptionAttribute (string caption) => Caption = caption;

	public string Caption { get; set; }
}

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class DigitsValueAttribute : Attribute
{
	public DigitsValueAttribute (int value) => Value = value;

	public int Value { get; set; }
}

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class IncrementValueAttribute : Attribute
{
	public IncrementValueAttribute (double value) => Value = value;

	public double Value { get; set; }
}

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class MinimumValueAttribute : Attribute
{
	public MinimumValueAttribute (int value) => Value = value;

	public int Value { get; set; }
}

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class MaximumValueAttribute : Attribute
{
	public MaximumValueAttribute (int value) => Value = value;

	public int Value { get; set; }
}

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class HintAttribute : Attribute
{
	public HintAttribute (string caption) => Hint = caption;

	public string Hint { get; set; }
}

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class StaticListAttribute : Attribute
{
	public StaticListAttribute (string dict) => DictionaryName = dict;

	public string DictionaryName { get; set; }
}

/// <summary>
/// Attribute for controlling the visibility of a control based on a condition.
/// The control will be hidden when the condition evaluates to false.
/// </summary>
[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class VisibleWhenAttribute : Attribute
{
	public VisibleWhenAttribute (string conditionMethodName) => ConditionMethodName = conditionMethodName;

	public string ConditionMethodName { get; }
}

/// <summary>
/// Attribute for controlling the enabled state of a control based on a condition.
/// The control will be disabled when the condition evaluates to false.
/// </summary>
[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class EnabledWhenAttribute : Attribute
{
	public EnabledWhenAttribute (string conditionMethodName) => ConditionMethodName = conditionMethodName;

	public string ConditionMethodName { get; }
}

