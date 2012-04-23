//
// ProxyHistoryItem.cs
//  
// Author:
//       Olivier Dufour <olivier (dot) duff (at) gmail (dot) com>
// 
// Copyright (c) 2010 Olivier Dufour
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinta.Core
{
	public class ProxyHistoryItem : BaseHistoryItem
	{
		private BaseHistoryItem item;
		public long offset;

		public ProxyHistoryItem (BaseHistoryItem item) : base(item.Icon, item.Text)
		{
			this.item = item;
		}

		public override void Undo ()
		{
			if (item == null) {
				item = PintaCore.Workspace.ActiveWorkspace.History.LoadCachedItem (offset);
			}
			item.Undo ();
		}

		public override void Redo ()
		{
			if (item == null) {
				item = PintaCore.Workspace.ActiveWorkspace.History.LoadCachedItem (offset);
			}
			item.Redo ();
		}

		public override void Dispose ()
		{
			//TODO think if need to delete in the temp file
			if (item != null)
				item.Dispose ();
		}

		public override void Save (System.IO.BinaryWriter writer)
		{
			if (item != null) {
				offset = writer.BaseStream.Position;
				item.Save (writer);
				writer.Flush ();
				item.Dispose ();
				item = null;
			}
		}
	}
}
