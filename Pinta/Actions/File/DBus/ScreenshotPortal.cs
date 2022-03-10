/// This file is generated automatically and manually adjusted.
/// dotnet tool install -g Tmds.DBus.Tool
/// dotnet dbus codegen --bus session --service org.freedesktop.portal.Desktop --interface org.freedesktop.portal.Screenshot

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Requests.DBus;
using Tmds.DBus;

[assembly: InternalsVisibleTo (Tmds.DBus.Connection.DynamicAssemblyName)]
namespace ScreenshotPortal.DBus
{
	[DBusInterface ("org.freedesktop.portal.Screenshot")]
	interface IScreenshot : IDBusObject
	{
		Task<IRequest> ScreenshotAsync (string ParentWindow, IDictionary<string, object> Options);
		Task<IRequest> PickColorAsync (string ParentWindow, IDictionary<string, object> Options);
		Task<T> GetAsync<T> (string prop);
		Task<ScreenshotProperties> GetAllAsync ();
		Task SetAsync (string prop, object val);
		Task<IDisposable> WatchPropertiesAsync (Action<PropertyChanges> handler);
	}

	[Dictionary]
	class ScreenshotProperties
	{
		private uint _version = default (uint);
		public uint Version {
			get {
				return _version;
			}

			set {
				_version = (value);
			}
		}
	}

	static class ScreenshotExtensions
	{
		public static Task<uint> GetVersionAsync (this IScreenshot o) => o.GetAsync<uint> ("version");
	}
}
