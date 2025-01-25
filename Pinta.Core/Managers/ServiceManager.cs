// 
// ServiceManager.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2020 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace Pinta.Core;

public static class ServiceProviderExtensions
{
	public static T GetService<T> (this IServiceProvider services) where T : class
	{
		object? implementation = services.GetService (typeof (T));
		if (implementation is not null)
			return (T) implementation;
		throw new ApplicationException ($"Could not resolve service type {typeof (T)}");
	}

	public static T? GetOptionalService<T> (this IServiceProvider services) where T : class
	{
		object? implementation = services.GetService (typeof (T));
		if (implementation is not null)
			return (T) implementation;
		return null;
	}
}

public interface IServiceManager : IServiceProvider
{
	T AddService<T> (T implementation) where T : class;
}

public sealed class ServiceManager : IServiceManager
{
	private readonly Dictionary<Type, object> services = [];

	public T AddService<T> (T implementation) where T : class
	{
		services.Add (typeof (T), implementation);
		return implementation;
	}

	public object? GetService (Type serviceType)
	{
		if (services.TryGetValue (serviceType, out var implementation))
			return implementation;
		else
			return null;
	}
}
