using Gio;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class ColorSchemeChangedAction : IActionHandler
{
	private readonly ViewActions view_actions;
	internal ColorSchemeChangedAction (ViewActions viewActions)
	{
		view_actions = viewActions;
	}

	void IActionHandler.Initialize ()
	{
		view_actions.ColorScheme.OnActivate += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		view_actions.ColorScheme.OnActivate -= Activated;
	}

	private void Activated (SimpleAction action, SimpleAction.ActivateSignalArgs args)
	{
		action.ChangeState (args.Parameter!);

		Adw.ColorScheme scheme = args.Parameter!.GetInt32 () switch {
			1 => Adw.ColorScheme.ForceLight,
			2 => Adw.ColorScheme.ForceDark,
			_ => Adw.ColorScheme.Default,
		};

		Adw.StyleManager.GetDefault ().SetColorScheme (scheme);
	}
}

