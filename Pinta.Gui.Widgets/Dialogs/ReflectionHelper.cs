using System;
using System.Reflection;

namespace Pinta.Gui.Widgets;

internal static class ReflectionHelper
{
	public static object? GetValue (object o, string name)
	{
		if (o.GetType ().GetField (name) is FieldInfo fi)
			return fi.GetValue (o);

		if (o.GetType ().GetProperty (name) is PropertyInfo pi)
			return pi.GetGetMethod ()?.Invoke (o, Array.Empty<object> ());

		return null;
	}
}
