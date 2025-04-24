// 
// ToolBoxButton.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2020 Jonathan Pobst
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

namespace Pinta.Core;

/// <summary>
/// Buttons used by the ToolBoxWidget.
/// </summary>
public sealed class ToolBoxButton : ToggleButton
{
	public BaseTool Tool { get; }

	// Hello, this is a run-once "static" constructor, you may remember me from documentation such as https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors
	static ToolBoxButton ()
	{
		// Force icons to have a specific size regardless of size of the application canvas.
		// Discussion #1374 where icons seems to be getting smaller when the screen gets bigger.
		Gtk.CssProvider css = Gtk.CssProvider.New ();
		css.LoadFromString (".ToolBoxButton { -gtk-icon-size: 2rem; }"); // Works well for high resolution and low resolution canvases across various DPI's
		Gdk.Display? display = Gdk.Display.GetDefault () ?? null;
		if (display is not null) {
			Gtk.StyleContext.AddProviderForDisplay (display, css, 1);
		}
	}

	public ToolBoxButton (BaseTool tool)
	{
		Tool = tool;
		IconName = tool.Icon;
		Name = tool.Name;
		CanFocus = false;


		SetCssClasses (["ToolBoxButton", AdwaitaStyles.Flat]);

		Show ();

		string shortcutText = "";
		if (tool.ShortcutKey != Gdk.Key.Invalid) {
			var shortcutLabel = Translations.GetString ("Shortcut key");
			shortcutText = $"{shortcutLabel}: {tool.ShortcutKey.ToUpper ().Name ()}\n";
		}

		TooltipText = $"{tool.Name}\n{shortcutText}\n{tool.StatusBarText}";
	}
}
