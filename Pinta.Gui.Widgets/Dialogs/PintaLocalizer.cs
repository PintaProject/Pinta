using Mono.Addins.Localization;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

/// <summary>
/// Wrapper around Pinta's translation template.
/// </summary>
public sealed class PintaLocalizer : IAddinLocalizer
{
	public string GetString (string msgid)
		=> Translations.GetString (msgid);
};
