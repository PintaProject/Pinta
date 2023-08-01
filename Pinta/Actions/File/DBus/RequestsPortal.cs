/// This file is generated automatically and manually adjusted.
/// dotnet tool install -g Tmds.DBus.Tool
/// dotnet dbus codegen Requests.xml
/// https://github.com/flatpak/xdg-desktop-portal/blob/main/data/org.freedesktop.portal.Request.xml

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo (Tmds.DBus.Connection.DynamicAssemblyName)]
namespace Requests.DBus
{
	[DBusInterface ("org.freedesktop.portal.Request")]
	interface IRequest : IDBusObject
	{
		Task CloseAsync ();
		Task<IDisposable> WatchResponseAsync (Action<(uint response, IDictionary<string, object> results)> handler, Action<Exception>? onError = null);
	}
}
