using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Pinta.Core;

public static class AsyncEventArgs<TSender, TArgs>
{
	public delegate Task<TResult> Returning<TResult> (TSender sender, TArgs args);
	public delegate Task Simple (TSender sender, TArgs args);
}

public static class AsyncEventArgsExtensions
{
	public static async Task<ImmutableArray<TResult>> InvokeSequential<TSender, TArgs, TResult> (
		this AsyncEventArgs<TSender, TArgs>.Returning<TResult> handlerBundle,
		TSender sender,
		TArgs args)
	{
		var invocationList =
			handlerBundle
			.GetInvocationList ()
			.Cast<AsyncEventArgs<TSender, TArgs>.Returning<TResult>> ()
			.ToArray ();

		var builder = ImmutableArray.CreateBuilder<TResult> (invocationList.Length);

		foreach (var item in invocationList) {
			TResult itemResult = await item (sender, args);
			builder.Add (itemResult);
		}

		return builder.ToImmutable ();
	}

	public static async Task InvokeSequential<TSender, TArgs, TResult> (
		this AsyncEventArgs<TSender, TArgs>.Simple handlerBundle,
		TSender sender,
		TArgs args)
	{
		var invocationList =
			handlerBundle
			.GetInvocationList ()
			.Cast<AsyncEventArgs<TSender, TArgs>.Simple> ()
			.ToArray ();

		foreach (var item in invocationList)
			await item (sender, args);
	}
}
