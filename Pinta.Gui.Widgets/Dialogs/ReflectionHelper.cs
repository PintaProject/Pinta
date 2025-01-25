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
			return pi.GetGetMethod ()?.Invoke (o, []);

		if (objectType.GetMember (name).Length > 0)
			throw new ArgumentException ($"Can't get value from member \'{name}\'. Only fields and attributes are supported");
		else
			throw new ArgumentException ($"Member \'{name}\' does not exist");
	}
}
