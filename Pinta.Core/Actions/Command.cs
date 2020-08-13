// 
// Command.cs
//  
// Author:
//       Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2010 Cameron White
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
using GLib;

namespace Pinta.Core
{
    /// <summary>
    /// Wrapper around a Glib.SimpleAction to store additional data such as the label.
    /// </summary>
    public class Command
    {
        public GLib.SimpleAction Action { get; private set; }
        public string Name
        {
            get { return Action.Name; }
        }
        public ActivatedHandler Activated;
        public void Activate()
        {
            Action.Activate(null);
        }

        public string Label { get; private set; }
        public string ShortLabel { get; set; }
        public string Tooltip { get; private set; }
        public string StockId { get; private set; }
        public string FullName { get { return string.Format("app.{0}", Name); } }
        public bool IsImportant { get; set; } = false;

        public bool Sensitive { get { return Action.Enabled; } set { Action.Enabled = value; } }

        public Command(string name, string label, string tooltip, string stock_id)
        {
            Action = new SimpleAction(name, null);
            Action.Activated += (o, args) =>
            {
                Activated?.Invoke(o, args);
            };

            Label = label;
            Tooltip = tooltip;
            StockId = stock_id;
        }

        public GLib.MenuItem CreateMenuItem()
        {
            return new GLib.MenuItem(Label, FullName);
        }
    }
}
