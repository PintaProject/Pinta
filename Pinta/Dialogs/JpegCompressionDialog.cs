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

using Gtk;
using Pinta.Core;

namespace Pinta
{
	public class JpegCompressionDialog : Dialog
	{
		private readonly Scale compressionLevel;

		public JpegCompressionDialog (int defaultQuality, Gtk.Window parent)
		{
			Title = Translations.GetString ("JPEG Quality");
			TransientFor = parent;
			Modal = true;
			this.AddCancelOkButtons ();
			this.SetDefaultResponse (ResponseType.Ok);

			var content_area = this.GetContentAreaBox ();
			content_area.SetAllMargins (6);
			content_area.Spacing = 3;

			var label = Label.New (Translations.GetString ("Quality: "));
			label.Xalign = 0;
			content_area.Append (label);

			compressionLevel = Scale.NewWithRange (Orientation.Horizontal, 1, 100, 1);
			compressionLevel.SetValue (defaultQuality);
			compressionLevel.DrawValue = true;
			content_area.Append (compressionLevel);
		}

		public int GetCompressionLevel ()
		{
			return (int) compressionLevel.GetValue ();
		}
	}
}
