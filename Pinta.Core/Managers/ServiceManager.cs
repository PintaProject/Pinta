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
