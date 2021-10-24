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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pinta.Core
{
	public interface IServiceManager
	{
		T AddService<T> (T implementation) where T : class;
		T GetService<T> () where T : class;
		T? GetOptionalService<T> () where T : class;
	}

	public class ServiceManager : IServiceManager
	{
		private Dictionary<Type, object> services = new Dictionary<Type, object> ();

		public T AddService<T> (T implementation) where T : class
		{
			services.Add (typeof (T), implementation);

			return implementation;
		}

		public T GetService<T> () where T : class
		{
			if (services.TryGetValue (typeof (T), out var implementation))
				return (T) implementation;

			throw new ApplicationException ($"Could not resolve service type {typeof (T)}");
		}

		public T? GetOptionalService<T> () where T : class
		{
			if (services.TryGetValue (typeof (T), out var implementation))
				return (T) implementation;

			return null;
		}
	}
}
