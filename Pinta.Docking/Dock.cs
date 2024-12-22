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

using Gtk;
using Pinta.Core;

namespace Pinta.Docking;

/// <summary>
/// The root widget, containing all dock items underneath it.
/// </summary>
public class Dock : Box
{
	private readonly Paned pane = Paned.New (Orientation.Horizontal);

	public DockPanel RightPanel {get; private init; } = new();

	public Dock ()
	{
		SetOrientation (Orientation.Horizontal);

		pane.EndChild = RightPanel;
		pane.ResizeEndChild = false;
		pane.ShrinkEndChild = false;
		Append (pane);
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

	private const string RightSplitPosKey = "dock-right-splitpos";

	public void SaveSettings (ISettingsService settings)
	{
#if false
		settings.PutSetting (RightSplitPosKey, pane.Position);
#endif
		RightPanel.SaveSettings (settings);
	}

	public void LoadSettings (ISettingsService settings)
	{
		// TODO-GTK3(docking) Disabled for now, as the size isn't quite restored properly (gradually increases over time)
#if false
		pane.Position = settings.GetSetting<int> (RightSplitPosKey, pane.Position);
#endif
		RightPanel.LoadSettings (settings);
	}
}
