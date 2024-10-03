using Mono.Addins.Localization;
using Pinta.Core;

namespace PintaBenchmarks;

internal sealed class MockChromeManager : IChromeService
{
	public Gtk.Window MainWindow => throw new NotImplementedException ();

	public Task<bool> LaunchSimpleEffectDialog (BaseEffect effect, IAddinLocalizer localizer)
	{
		throw new NotImplementedException ();
	}
}
