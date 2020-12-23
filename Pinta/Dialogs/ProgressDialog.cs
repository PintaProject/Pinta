// 
// ProgressDialog.cs
//  
// Author:
//       Greg Lowe <greg@vis.net.nz>
// 
// Copyright (c) 2010 Greg Lowe
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
using Pinta.Core;
using Gtk;
using System.Diagnostics.CodeAnalysis;

namespace Pinta
{
    public class ProgressDialog : Dialog, IProgressDialog
    {
        private Label label;
        private ProgressBar progress_bar;
        uint timeout_id;

        public ProgressDialog ()
            : base (string.Empty, PintaCore.Chrome.MainWindow, DialogFlags.Modal)
        {
            WindowPosition = WindowPosition.CenterOnParent;

            this.Build ();
            timeout_id = 0;
            Hide ();
        }

        public string Text
        {
            get { return label.Text; }
            set { label.Text = value; }
        }

        public double Progress
        {
            get { return progress_bar.Fraction; }
            set { progress_bar.Fraction = value; }
        }

        public event EventHandler<EventArgs>? Canceled;

        void IProgressDialog.Show ()
        {
            timeout_id = GLib.Timeout.Add (500, () => {
                this.ShowAll ();
                timeout_id = 0;
                return false;
            });
        }

        void IProgressDialog.Hide ()
        {
            if (timeout_id != 0)
                GLib.Source.Remove (timeout_id);
            this.Hide ();
        }

        protected override void OnResponse (Gtk.ResponseType response_id)
        {
            if (Canceled != null)
                Canceled (this, EventArgs.Empty);
        }

	[MemberNotNull (nameof (label), nameof (progress_bar))]
        private void Build ()
        {
            ContentArea.BorderWidth = 2;
            ContentArea.Spacing = 6;

            label = new Label ();
            ContentArea.Add (label);

            progress_bar = new ProgressBar ();
            ContentArea.Add (progress_bar);

            AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);

            DefaultWidth = 400;
            DefaultHeight = 114;
        }
    }
}
