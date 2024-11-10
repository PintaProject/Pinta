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
using Gtk;
using Pinta.Core;

namespace Pinta;

public sealed class ProgressDialog : Dialog, IProgressDialog
{
	private readonly Label label;
	private readonly ProgressBar progress_bar;
	uint timeout_id;

	public ProgressDialog (ChromeManager chrome)
	{
		TransientFor = chrome.MainWindow;
		Modal = true;

		OnResponse += (_, args) => Canceled?.Invoke (this, EventArgs.Empty);

		var content_area = this.GetContentAreaBox ();
		content_area.Spacing = 6;
		content_area.SetAllMargins (2);

		label = new Label ();
		content_area.Append (label);

		progress_bar = new ProgressBar ();
		content_area.Append (progress_bar);

		AddButton (Translations.GetString ("_Cancel"), (int) ResponseType.Cancel);

		DefaultWidth = 400;
		DefaultHeight = 114;

		timeout_id = 0;
		Hide ();
	}

	public new string Title {
		get => base.GetTitle ()!;
		set => SetTitle (value);
	}

	public string Text {
		get => label.GetText ();
		set => label.SetText (value);
	}

	public double Progress {
		get => progress_bar.Fraction;
		set => progress_bar.Fraction = value;
	}

	public event EventHandler<EventArgs>? Canceled;

	void IProgressDialog.Show ()
	{
		timeout_id = GLib.Functions.TimeoutAdd (
			0,
			500,
			() => {
				Show ();
				timeout_id = 0;
				return false;
			}
		);
	}

	void IProgressDialog.Hide ()
	{
		if (timeout_id != 0)
			GLib.Source.Remove (timeout_id);
		Hide ();
	}
}
