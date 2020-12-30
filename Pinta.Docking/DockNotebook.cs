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
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace Pinta.Docking
{
    public class TabClosedEventArgs : CancelEventArgs
    {
        public TabClosedEventArgs(IDockNotebookItem item) { Item = item; }

        public IDockNotebookItem Item { get; private set; }
    }

    public class TabEventArgs : EventArgs
    {
        public TabEventArgs(IDockNotebookItem? item) { Item = item; }

        public IDockNotebookItem? Item { get; private set; }
    }

    public class DockNotebook : Gtk.Notebook
    {
        private HashSet<IDockNotebookItem> items = new HashSet<IDockNotebookItem>();

        // TODO-GTK3 (docking) - add support for dragging tabs into separate notebooks?

        public DockNotebook()
        {
            // Emit an event when the current tab is changed.
            SwitchPage += (o, args) =>
            {
                var widget = args.Page;
                IDockNotebookItem? item = items.Where(i => i.Widget == widget).FirstOrDefault();
                ActiveTabChanged?.Invoke(this, new TabEventArgs(item));
            };
        }

        /// <summary>
        /// Emitted when a tab is closed by the user.
        /// </summary>
        public event EventHandler<TabClosedEventArgs>? TabClosed;

        /// <summary>
        /// Emitted when switching to a different tab.
        /// </summary>
        public event EventHandler<TabEventArgs>? ActiveTabChanged;

        /// <summary>
        /// The items currently in the notebook.
        /// </summary>
        public IEnumerable<IDockNotebookItem> Items { get { return items; } }

        /// <summary>
        /// Returns the active notebook item.
        /// </summary>
        public IDockNotebookItem? ActiveItem
        {
            get
            {
                var current = CurrentPageWidget;
                return items.Where(i => i.Widget == current).FirstOrDefault();
            }
            set
            {
                var idx = PageNum(value!.Widget);
                if (idx >= 0)
                    CurrentPage = idx;
            }
        }

        public void InsertTab(IDockNotebookItem item, int position)
        {
            var tab_layout = new HBox();
            var label_widget = new Label(item.Label);
            item.LabelChanged += (o, args) => { label_widget.Text = item.Label; };

            var close_button = new Button("window-close-symbolic", IconSize.SmallToolbar) {
                Relief = ReliefStyle.None
            };
            close_button.Clicked += (sender, args) =>
            {
                var e = new TabClosedEventArgs(item);

                TabClosed?.Invoke(this, e);
                if (!e.Cancel && PageNum(item.Widget) >= 0)
                    RemoveTab(item);
            };

            tab_layout.PackStart(label_widget, false, false, 0);
            tab_layout.PackStart(close_button, false, false, 0);
            tab_layout.ShowAll();

            InsertPage(item.Widget, tab_layout, position);
            SetTabReorderable(item.Widget, true);

            items.Add(item);
        }

        public void RemoveTab(IDockNotebookItem item)
        {
            int idx = PageNum(item.Widget);
            if (idx < 0)
                throw new ArgumentException("The item is not in the notebook", nameof(item));

            RemovePage(idx);

            items.Remove(item);
        }
    }
}
