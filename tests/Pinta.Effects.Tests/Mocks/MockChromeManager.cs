using System;
using Gtk;
using Mono.Addins.Localization;
using Pinta.Core;

namespace Pinta.Effects;

internal sealed class MockChromeManager : IChromeService
{
	public Window MainWindow => throw new NotImplementedException ();

	public void LaunchSimpleEffectDialog (BaseEffect effect, IAddinLocalizer localizer)
	{
		throw new System.NotImplementedException ();
	}
}
