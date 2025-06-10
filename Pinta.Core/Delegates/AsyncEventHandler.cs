using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Pinta.Core;

/// <summary>Contains delegates for asynchronous event handlers.</summary>
/// <typeparam name="TSender">Type of event sender.</typeparam>
/// <typeparam name="TArgs">Type of event arguments.</typeparam>
public static class AsyncEventHandler<TSender, TArgs>
{
	/// <summary>
	/// Represents an asynchronous event handler that returns a result.
	/// </summary>
	/// 
	/// <typeparam name="TResult">The type of the result returned by the handler.</typeparam>
	/// 
	/// <param name="sender">The source of the event.</param>
	/// <param name="args">An object that contains the event data.</param>
	/// 
	/// <returns>A task that represents the asynchronous operation, containing the result of the handler.</returns>
	/// 
	/// <remarks>
	/// For use as an event handler, consider looking into
	/// <see cref="AsyncEventHandlerExtensions.InvokeSequential{TSender, TArgs, TResult}(Returning{TResult}, TSender, TArgs)"/>,
	/// </remarks>
	public delegate Task<TResult> Returning<TResult> (TSender sender, TArgs args);

	/// <summary>
	/// Represents a simple asynchronous event handler that does not return a value.
	/// </summary>
	/// 
	/// <param name="sender">The source of the event.</param>
	/// <param name="args">An object that contains the event data.</param>
	/// 
	/// <returns>A task that represents the asynchronous operation.</returns>
	/// 
	/// <remarks>
	/// For use as an event handler, consider looking into
	/// <see cref="AsyncEventHandlerExtensions.InvokeSequential{TSender, TArgs}(Simple, TSender, TArgs)"/>,
	/// </remarks>
	public delegate Task Simple (TSender sender, TArgs args);
}

/// <summary>
/// Provides extension methods for invoking asynchronous event handlers sequentially.
/// </summary>
public static class AsyncEventHandlerExtensions
{
	/// <summary>
	/// Decomposes the asynchronous multi-cast delegate
	/// and executes its constituent delegates one after another
	/// </summary>
	/// 
	/// <returns>
	/// Task representing the asynchronous operation of awaiting one delegate after another.
	/// It completes when all its constituent delegates have been executed.
	/// Its result is an array with the results from each handler
	/// </returns>
	/// 
	/// <remarks>The aggregation/combination of the returned results depends on the use case</remarks>
	public static async Task<ImmutableArray<TResult>> InvokeSequential<TSender, TArgs, TResult> (
		this AsyncEventHandler<TSender, TArgs>.Returning<TResult> handlerBundle,
		TSender sender,
		TArgs args)
	{
		var invocationList =
			handlerBundle
			.GetInvocationList ()
			.Cast<AsyncEventHandler<TSender, TArgs>.Returning<TResult>> ()
			.ToImmutableArray ();

		var builder = ImmutableArray.CreateBuilder<TResult> (invocationList.Length);

		foreach (var item in invocationList) {
			TResult itemResult = await item (sender, args);
			builder.Add (itemResult);
		}

		return builder.ToImmutable ();
	}

	/// <summary>
	/// Decomposes the asynchronous multi-cast delegate
	/// and executes its constituent delegates one after another
	/// </summary>
	/// 
	/// <returns>
	/// Task representing the asynchronous operation of awaiting one delegate after another.
	/// It completes when all its individual components have been executed.
	/// </returns>
	public static async Task InvokeSequential<TSender, TArgs> (
		this AsyncEventHandler<TSender, TArgs>.Simple handlerBundle,
		TSender sender,
		TArgs args)
	{
		var invocationList =
			handlerBundle
			.GetInvocationList ()
			.Cast<AsyncEventHandler<TSender, TArgs>.Simple> ()
			.ToImmutableArray ();

		foreach (var item in invocationList)
			await item (sender, args);
	}
}
