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
    public class DockNotebook : Gtk.Notebook
    {
        public DockNotebook()
        {
            EnablePopup = true;
        }

        public void InsertTab(IDockNotebookItem item, int position)
        {
            var tab_layout = new HBox();
            var label_widget = new Label(item.Label);
            item.LabelChanged += (o, args) => { label_widget.Text = item.Label; };

            var close_button = new Button("window-close-symbolic", IconSize.SmallToolbar) {
                Relief = ReliefStyle.None
            };

            tab_layout.PackStart(label_widget, false, false, 0);
            tab_layout.PackStart(close_button, false, false, 0);
            tab_layout.ShowAll();

            InsertPage(item.Widget, tab_layout, position);
            SetTabReorderable(item.Widget, true);
        }
    }
}
