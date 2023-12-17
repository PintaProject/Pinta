using Gio;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class ColorSchemeChangedAction : IActionHandler
{
	#region IActionHandler Members
	public void Initialize ()
	{
		PintaCore.Actions.View.ColorScheme.OnActivate += Activated;
	}

	public void Uninitialize ()
	{
		PintaCore.Actions.View.ColorScheme.OnActivate -= Activated;
	}
	#endregion

	private void Activated (SimpleAction action, SimpleAction.ActivateSignalArgs args)
	{
		action.ChangeState (args.Parameter!);

		Adw.ColorScheme scheme = args.Parameter!.GetInt32 () switch {
			1 => Adw.ColorScheme.ForceLight,
			2 => Adw.ColorScheme.ForceDark,
			_ => Adw.ColorScheme.PreferDark, // Use dark unless the system prefers light
		};

		Adw.StyleManager.GetDefault ().SetColorScheme (scheme);
	}
}

