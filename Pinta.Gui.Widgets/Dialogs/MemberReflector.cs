using System;
using System.Collections.Immutable;
using System.Reflection;

using Setter = System.Action<object, object>;
using Getter = System.Func<object, object?>;

namespace Pinta.Gui.Widgets;

internal sealed class MemberReflector
{
	public MemberInfo OriginalMemberInfo { get; }
	public Type MemberType { get; }
	public ImmutableArray<Attribute> Attributes { get; }

	private readonly Getter getter;
	private readonly Setter setter;

	public MemberReflector (MemberInfo memberInfo)
	{
		ImmutableArray<Attribute> attributes =
			memberInfo
			.GetCustomAttributes<Attribute> (false)
			.ToImmutableArray ();

		OriginalMemberInfo = memberInfo;
		MemberType = GetTypeForMember (memberInfo);
		Attributes = attributes;

		getter = CreateGetter (memberInfo);
		setter = CreateSetter (memberInfo);
	}

	public object? GetValue (object o)
		=> getter (o);

	public void SetValue (object o, object val)
		=> setter (o, val);

	private static Setter CreateSetter (MemberInfo memberInfo)
	{
		switch (memberInfo) {
			case FieldInfo f:
				return f.SetValue;
			case PropertyInfo p:
				MethodInfo setter = p.GetSetMethod () ?? throw new ArgumentException ("Property has no 'set' method", nameof (memberInfo));
				return (o, v) => setter.Invoke (o, new[] { v });
			default:
				throw new ArgumentException ($"Member type {memberInfo.GetType ()} not supported", nameof (memberInfo));
		}
	}

	private static Getter CreateGetter (MemberInfo memberInfo)
	{
		switch (memberInfo) {
			case FieldInfo f:
				return f.GetValue;
			case PropertyInfo p:
				MethodInfo getter = p.GetGetMethod () ?? throw new ArgumentException ("Property has no 'get' method", nameof (memberInfo));
				return o => getter.Invoke (o, Array.Empty<object> ());
			default:
				throw new ArgumentException ($"Member type {memberInfo.GetType ()} not supported", nameof (memberInfo));
		}
	}

	private static Type GetTypeForMember (MemberInfo mi) =>
		mi switch {
			FieldInfo fi => fi.FieldType,
			PropertyInfo pi => pi.PropertyType,
			_ => throw new ArgumentException ("Invalid member type", nameof (mi)),
		};
}
