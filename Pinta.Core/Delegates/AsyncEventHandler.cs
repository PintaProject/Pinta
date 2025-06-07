using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Pinta.Core;

public static class AsyncEventHandler<TSender, TArgs>
{
	public delegate Task<TResult> Returning<TResult> (TSender sender, TArgs args);
	public delegate Task Simple (TSender sender, TArgs args);
}

public static class AsyncEventHandlerExtensions
{
	public static async Task<ImmutableArray<TResult>> InvokeSequential<TSender, TArgs, TResult> (
		this AsyncEventHandler<TSender, TArgs>.Returning<TResult> handlerBundle,
		TSender sender,
		TArgs args)
	{
		var invocationList =
			handlerBundle
			.GetInvocationList ()
			.Cast<AsyncEventHandler<TSender, TArgs>.Returning<TResult>> ()
			.ToArray ();

		var builder = ImmutableArray.CreateBuilder<TResult> (invocationList.Length);

		foreach (var item in invocationList) {
			TResult itemResult = await item (sender, args);
			builder.Add (itemResult);
		}

		return builder.ToImmutable ();
	}

	public static async Task InvokeSequential<TSender, TArgs, TResult> (
		this AsyncEventHandler<TSender, TArgs>.Simple handlerBundle,
		TSender sender,
		TArgs args)
	{
		var invocationList =
			handlerBundle
			.GetInvocationList ()
			.Cast<AsyncEventHandler<TSender, TArgs>.Simple> ()
			.ToArray ();

		foreach (var item in invocationList)
			await item (sender, args);
	}
}
