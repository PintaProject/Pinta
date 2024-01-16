using Gtk;
using Mono.Addins.Localization;
using Pinta.Core;

namespace PintaBenchmarks;

internal sealed class MockChromeManager : IChromeService
{
	public Window MainWindow => throw new NotImplementedException ();

	public void LaunchSimpleEffectDialog (BaseEffect effect, IAddinLocalizer localizer)
	{
		throw new NotImplementedException ();
	}
}
