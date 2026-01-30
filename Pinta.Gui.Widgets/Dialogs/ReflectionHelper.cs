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

	/// <summary>
	/// Evaluates a condition from a property or method of an object.
	/// </summary>
	/// <param name="source">The object containing the property or method.</param>
	/// <param name="methodName">The name of the property or method to evaluate.</param>
	/// <returns>The boolean result of the property or method.</returns>
	public static bool EvaluateCondition (object source, string methodName)
	{
		Type type = source.GetType ();

		// Try to find a property first
		PropertyInfo? property = type.GetProperty (methodName, BindingFlags.Public | BindingFlags.Instance);
		if (property is not null && property.PropertyType == typeof (bool)) {
			return (bool) property.GetValue (source)!;
		}
		// If we couldn't find a property, try to find a method
		MethodInfo? method = type.GetMethod (methodName, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
		if (method is not null && method.ReturnType == typeof (bool)) {
			return (bool) method.Invoke (source, null)!;
		}

		System.Diagnostics.Debug.WriteLine ($"Warning: Could not find condition property/method '{methodName}' on type '{type.Name}'.");
		return true; // Fallback to true
	}
}
