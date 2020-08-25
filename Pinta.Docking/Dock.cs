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

namespace Pinta.Docking
{
    /// <summary>
    /// The root widget, containing all dock items underneath it.
    /// </summary>
    public class Dock : HBox
    {
        private readonly DockPanel left_panel = new DockPanel();
        private readonly DockPanel right_panel = new DockPanel();
        private readonly Paned pane1 = new Paned(Orientation.Horizontal);
        private readonly Paned pane2 = new Paned(Orientation.Horizontal);

        public Dock()
        {
            pane1.Pack1(left_panel, resize: false, shrink: false);
            pane1.Add2(pane2);
            pane2.Pack2(right_panel, resize: false, shrink: false);
            Add(pane1);
        }

        public void AddItem(DockItem item, DockPlacement placement)
        {
            switch (placement)
            {
                case DockPlacement.Left:
                    left_panel.AddItem(item);
                    break;
                case DockPlacement.Center:
                    pane2.Pack1(item, resize: true, shrink: false);
                    break;
                case DockPlacement.Right:
                    right_panel.AddItem(item);
                    break;
            }
        }
    }
}
