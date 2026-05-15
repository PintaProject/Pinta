using System;

using GdkPixbuf;

namespace Pinta.Core;

public sealed class WebPFormat : GdkPixbufFormat
{
	private const int DefaultQuality = 80;

	public WebPFormat ()
		: base ("webp")
	{
	}

	protected override void DoSave (Pixbuf pb, Gio.File file, string fileType, Gtk.Window parent)
	{
		int level = PintaCore.Settings.GetSetting<int> (SettingNames.WEBP_QUALITY, DefaultQuality);

		if (!PintaCore.Workspace.ActiveDocument.HasBeenSavedInSession) {
			level = PintaCore.Actions.File.RaiseModifyCompression (level, parent);

			if (level == -1)
				throw new OperationCanceledException ();
		}

		PintaCore.Settings.PutSetting (SettingNames.WEBP_QUALITY, level);

		using var stream = file.Replace ();
		try {
			pb.SaveToStreamv (stream, fileType, ["quality"], [level.ToString ()], null);
		} finally {
			stream.Close (null);
		}
	}
}
