using System.Threading.Tasks;
using Mono.Addins.Localization;

namespace Pinta.Core;

public interface ILivePreviewSession
{
	Gtk.Window ParentWindow { get; }
	void NotifyChanged ();
	Task<bool> LaunchSimpleEffectDialog (IAddinLocalizer? localizer = null);
}
