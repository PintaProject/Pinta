using System.Threading.Tasks;
using Mono.Addins;
using Mono.Addins.Localization;

namespace Pinta.Core;

public static class AddinUtilities
{
	/// <summary>
	/// Launch an effect dialog using specified localizer
	/// </summary>
	public static Task<bool> LaunchSimpleEffectDialog (
		this IChromeService chrome,
		IWorkspaceService workspace,
		BaseEffect effect,
		AddinLocalizer localizer)
	{
		return chrome.LaunchSimpleEffectDialog (
			chrome.MainWindow,
			effect,
			CreateWrapper (localizer),
			workspace);
	}

	private static IAddinLocalizer CreateWrapper (AddinLocalizer localizer)
		=> new AddinLocalizerWrapper (localizer);

	/// <summary>
	/// Wrapper around the AddinLocalizer of an add-in.
	/// </summary>
	private sealed class AddinLocalizerWrapper : IAddinLocalizer
	{
		private readonly AddinLocalizer localizer;
		internal AddinLocalizerWrapper (AddinLocalizer localizer)
		{
			this.localizer = localizer;
		}

		public string GetString (string msgid)
			=> localizer.GetString (msgid);
	}
}
