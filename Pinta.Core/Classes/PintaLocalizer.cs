using Mono.Addins.Localization;

namespace Pinta.Core;

/// <summary>Wrapper around Pinta's translation template.</summary>
public sealed class PintaLocalizer : IAddinLocalizer
{
	public string GetString (string msgid)
		=> Translations.GetString (msgid);
};
