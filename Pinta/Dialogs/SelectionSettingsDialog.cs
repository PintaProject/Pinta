using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta;

public sealed class SelectionSettingsDialog : Gtk.Dialog
{
	public SelectionSettingsDialog (
		ChromeManager chrome,
		bool selectionAnimation)
	{
		var checkbox = Gtk.CheckButton.NewWithLabel (
			Translations.GetString ("Marching Ants"));

		checkbox.Active = selectionAnimation;

		var label = Gtk.Label.New (
			Translations.GetString (
			"Restart Pinta to apply this setting."));

		var box = this.GetContentAreaBox ();
		box.SetAllMargins (12);
		box.Append (checkbox);
		box.Append (label);


		Title = Translations.GetString ("Selection Settings");
		TransientFor = chrome.MainWindow;
		Modal = true;


		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);


		OnResponse += (o,args) =>
		{
			if (args.ResponseId == (int) Gtk.ResponseType.Ok)
			{
				PintaCore.Settings.PutSetting (
					Pinta.Core.SettingNames.SELECTION_ANIMATION,
					checkbox.Active);
			}

			Destroy();
		};
	}
}
