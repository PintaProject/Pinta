using System;
using System.Threading.Tasks;
using Mono.Addins.Localization;
using Pinta.Core;

namespace Pinta.Effects;

internal sealed class MockChromeManager : IChromeService
{
	public Gtk.Window MainWindow => throw new NotImplementedException ();

	public Task<bool> LaunchSimpleEffectDialog (
		Gtk.Window parent,
		BaseEffect effect,
		IAddinLocalizer localizer,
		IWorkspaceService workspace)
	{
		throw new NotImplementedException ();
	}
}
