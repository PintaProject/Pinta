// 
// OpenImagesPad.cs
//  
// Author:
//       Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2011 2011
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

using MonoDevelop.Components.Docking;
using Mono.Unix;
using Gtk;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta
{
	public class OpenImagesPad : IDockPad
	{
		public void Initialize (DockFrame workspace, Menu padMenu)
		{
			const string pad_name = "Open Images";

			DockItem open_images_item = workspace.AddItem (pad_name);
			open_images_item.Label = Catalog.GetString (pad_name);
			open_images_item.Content = new OpenImagesListWidget ();

			ToggleAction show_open_images = padMenu.AppendToggleAction (pad_name, Catalog.GetString (pad_name), null, null);

			show_open_images.Activated += delegate {
				open_images_item.Visible = show_open_images.Active;
			};

			open_images_item.VisibleChanged += delegate {
				show_open_images.Active = open_images_item.Visible;
			};

			show_open_images.Active = open_images_item.Visible;
		}
	}
}

