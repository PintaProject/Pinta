using static Pinta.Docking.DockPanel;

namespace Pinta.Docking;

internal sealed class SettingNames
{
	internal const string DOCK_RIGHT_SPLITPOS = "dock-right-splitpos";

	private static string BaseSettingKey (DockPanelItem item)
		=> $"dock-{item.Item.UniqueName.ToLower ()}";

	internal static string MinimizeKey (DockPanelItem item)
		=> BaseSettingKey (item) + "-minimized";

	internal static string SplitPosKey (DockPanelItem item)
		=> BaseSettingKey (item) + "-splitpos";
}
