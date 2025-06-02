using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace Pinta.Gui.Widgets;

internal sealed class MemberReflector
{
	public MemberInfo OriginalMemberInfo { get; }
	public Type MemberType { get; }
	public ImmutableArray<Attribute> Attributes { get; }

	private readonly Func<object, object?> getter;
	private readonly Action<object, object> setter;

	public MemberReflector (MemberInfo memberInfo)
	{
		ImmutableArray<Attribute> attributes = [.. memberInfo.GetCustomAttributes<Attribute> (inherit: false)];

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

	private static Action<object, object> CreateSetter (MemberInfo memberInfo)
	{
		Type memberType = GetTypeForMember (memberInfo);
		ParameterExpression selfParam = Expression.Parameter (typeof (object), "self");
		ParameterExpression valueParam = Expression.Parameter (typeof (object), "new_value");
		UnaryExpression selfDowncast = Expression.Convert (selfParam, memberInfo.DeclaringType!);
		UnaryExpression valueDownCast =
			memberType.IsValueType
			? Expression.Unbox (valueParam, memberType)
			: Expression.Convert (valueParam, memberType);
		MemberExpression memberAccessParam = Expression.MakeMemberAccess (selfDowncast, memberInfo);
		BinaryExpression assignExpression = Expression.Assign (memberAccessParam, valueDownCast);
		LambdaExpression lambda = Expression.Lambda<Action<object, object>> (assignExpression, [selfParam, valueParam]);
		Action<object, object> compiled = (Action<object, object>) lambda.Compile ();
		return compiled;
	}

	private static Func<object, object?> CreateGetter (MemberInfo memberInfo)
	{
		switch (memberInfo) {
			case FieldInfo f:
				return f.GetValue;
			case PropertyInfo p:
				MethodInfo getter = p.GetGetMethod () ?? throw new ArgumentException ("Property has no 'get' method", nameof (memberInfo));
				return o => getter.Invoke (o, []);
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
