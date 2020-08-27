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

using System;
using Gtk;

namespace Pinta.Docking
{
    /// <summary>
    /// A dock item contains a single child widget, and can be docked at
    /// various locations.
    /// </summary>
    public class DockItem : VBox
    {
        private Label label_widget;

        /// <summary>
        /// Unique identifier for the dock item. Used e.g. when saving the dock layout to disk.
        /// </summary>
        public string UniqueName { get; private set; }

        /// <summary>
        /// Visible label for the dock item.
        /// </summary>
        public string Label { get => label_widget.Text; set => label_widget.Text = value; }

        /// <summary>
        /// Triggered when the minimize button is pressed.
        /// </summary>
        public event EventHandler? Minimized;

        /// <summary>
        /// Triggered when the maximize button is pressed.
        /// </summary>
        public event EventHandler? Maximized;

        public DockItem(Widget child, string unique_name, bool locked = false)
        {
            UniqueName = unique_name;

            label_widget = new Label();
            if (!locked)
            {
                const int padding = 8;
                var title_layout = new HBox();
                title_layout.PackStart(label_widget, false, false, padding);

                var minimize_button = new Button("window-minimize-symbolic", IconSize.Button) { Relief = ReliefStyle.None };
                var maximize_button = new Button("window-maximize-symbolic", IconSize.Button) { Relief = ReliefStyle.None };

                var button_stack = new Stack();
                button_stack.Add(minimize_button);
                button_stack.Add(maximize_button);
                title_layout.PackEnd(button_stack, false, false, 0);

                minimize_button.Clicked += (o, args) =>
                {
                    button_stack.VisibleChild = maximize_button;
                    Minimized?.Invoke(this, new EventArgs());
                };

                maximize_button.Clicked += (o, args) =>
                {
                    button_stack.VisibleChild = minimize_button;
                    Maximized?.Invoke(this, new EventArgs());
                };

                PackStart(title_layout, false, false, 0);
            }

            PackStart(child, true, true, 0);

            // TODO - support dragging into floating panel?
        }

        /// <summary>
        /// Create a toolbar and add it to the bottom of the dock item.
        /// </summary>
        public Toolbar AddToolBar()
        {
            var toolbar = new Toolbar();
            PackStart(toolbar, false, false, 0);
            return toolbar;
        }
    }
}
