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
using Mono.Unix;
using Pinta.Gui.Widgets;

namespace Pinta
{
	public class JpegCompressionDialog : Dialog
	{
		private HScale compressionLevel;
	
		public JpegCompressionDialog (int defaultQuality, Gtk.Window parent)
			: base (Catalog.GetString ("JPEG Quality"), parent, DialogFlags.Modal | DialogFlags.DestroyWithParent,
				Stock.Cancel, ResponseType.Cancel, Stock.Ok, ResponseType.Ok)
		{
			this.BorderWidth = 6;
			this.VBox.Spacing = 3;
			VBox content = new VBox ();
			content.Spacing = 5;

			DefaultResponse = ResponseType.Ok;
			
			Label label = new Label (Catalog.GetString ("Quality: "));
			label.Xalign = 0;
			content.PackStart (label, false, false, 0);
			
			compressionLevel = new HScale (1, 100, 1);
			compressionLevel.Value = defaultQuality;
			content.PackStart (compressionLevel, false, false, 0);

			content.ShowAll ();
			this.VBox.Add (content);
			AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };
		}
		
		public int GetCompressionLevel ()
		{
			return (int) compressionLevel.Value;
		}
	}
}
