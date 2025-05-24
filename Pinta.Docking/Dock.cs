//  
// Author:
//       Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2020 Cameron White
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Pinta.Core;

namespace Pinta.Docking;

/// <summary>
/// The root widget, containing all dock items underneath it.
/// </summary>
public sealed class Dock : Gtk.Box
{
	private readonly Gtk.Paned pane;

	public DockPanel RightPanel { get; } = new ();

	public Dock ()
	{
		Gtk.Paned pane = Gtk.Paned.New (Gtk.Orientation.Horizontal);
		pane.EndChild = RightPanel;
		pane.ResizeEndChild = false;
		pane.ShrinkEndChild = false;

		// --- Initialization (Gtk.Box)

		SetOrientation (Gtk.Orientation.Horizontal);
		Append (pane);

		// --- References to keep

		this.pane = pane;
	}

	public void AddItem (DockItem item, DockPlacement placement)
	{
		switch (placement) {
			case DockPlacement.Center:
				pane.StartChild = item;
				pane.ResizeStartChild = true;
				pane.ShrinkStartChild = false;
				break;
			case DockPlacement.Right:
				RightPanel.AddItem (item);
				break;
		}
	}

	public void SaveSettings (ISettingsService settings)
	{
#if false
		settings.PutSetting (SettingNames.DOCK_RIGHT_SPLITPOS, pane.Position);
#endif
		RightPanel.SaveSettings (settings);
	}

	public void LoadSettings (ISettingsService settings)
	{
		// TODO-GTK3(docking) Disabled for now, as the size isn't quite restored properly (gradually increases over time)
#if false
		pane.Position = settings.GetSetting<int> (SettingNames.DOCK_RIGHT_SPLITPOS, pane.Position);
#endif
		RightPanel.LoadSettings (settings);
	}
}
