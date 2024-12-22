using Pinta.Core;

namespace Pinta.Actions;

internal sealed class ToolWindowsToggledAction : IActionHandler
{
	private readonly ViewActions view;
	private readonly ChromeManager chrome;
	internal ToolWindowsToggledAction (
		ViewActions view,
		ChromeManager chrome)
	{
		this.view = view;
		this.chrome = chrome;
	}

	void IActionHandler.Initialize ()
	{
		view.ToolWindows.Toggled += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		view.ToolWindows.Toggled -= Activated;
	}

	private void Activated (bool value)
	{
		var dock = (Docking.Dock) chrome.Dock;
		dock.RightPanel.Visible = value;

		System.Console.WriteLine ($"visible {value}");
	}
}
