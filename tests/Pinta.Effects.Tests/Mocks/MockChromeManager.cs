using System;
using System.Threading.Tasks;
using Mono.Addins.Localization;
using Pinta.Core;

namespace Pinta.Effects;

internal sealed class MockChromeManager : IChromeService
{
	public Gtk.Window MainWindow => throw new NotImplementedException ();

	public Task<Gtk.ResponseType> LaunchSimpleEffectDialog (BaseEffect effect, IAddinLocalizer localizer)
	{
		throw new NotImplementedException ();
	}
}
