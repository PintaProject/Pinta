using Mono.Addins.Localization;

namespace Pinta.Core;

/// <summary>Wrapper around Pinta's translation template.</summary>
internal sealed class TemporaryLocalizer : IAddinLocalizer
{
	public string GetString (string msgid)
		=> Translations.GetString (msgid);
};
