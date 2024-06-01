// 
// JpegCompressionDialog.cs
//  
// Author:
//       Maia Kozheva <sikon@ubuntu.com>
// 
// Copyright (c) 2010 Maia Kozheva
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

using Pinta.Core;

namespace Pinta;

public sealed class JpegCompressionDialog : Gtk.Dialog
{
	private readonly Gtk.Scale compression_level;

	public JpegCompressionDialog (int defaultQuality, Gtk.Window parent)
	{
		Title = Translations.GetString ("JPEG Quality");
		TransientFor = parent;
		Modal = true;

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		Gtk.Label label = CreateQualityLabel ();
		compression_level = CreateCompressionScale (defaultQuality);

		Gtk.Box content_area = this.GetContentAreaBox ();
		content_area.SetAllMargins (6);
		content_area.Spacing = 3;
		content_area.Append (label);
		content_area.Append (compression_level);
	}

	private static Gtk.Label CreateQualityLabel ()
	{
		Gtk.Label result = Gtk.Label.New (Translations.GetString ("Quality: "));
		result.Xalign = 0;
		return result;
	}

	private static Gtk.Scale CreateCompressionScale (int defaultQuality)
	{
		Gtk.Scale result = Gtk.Scale.NewWithRange (Gtk.Orientation.Horizontal, 1, 100, 1);
		result.SetValue (defaultQuality);
		result.DrawValue = true;
		return result;
	}

	public int CompressionLevel
		=> (int) compression_level.GetValue ();
}
