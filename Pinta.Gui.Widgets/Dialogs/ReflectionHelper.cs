using System;
using System.Reflection;

namespace Pinta.Gui.Widgets;

internal static class ReflectionHelper
{
	public static object? GetValue (object o, string name)
	{
		Type objectType = o.GetType ();

		if (objectType.GetField (name) is FieldInfo fi)
			return fi.GetValue (o);

		if (objectType.GetProperty (name) is PropertyInfo pi)
			return pi.GetGetMethod ()?.Invoke (o, Array.Empty<object> ());

		throw new ArgumentException ($"Member '{name}' is not supported or does not exist. Only fields and properties are supported");
	}
}
