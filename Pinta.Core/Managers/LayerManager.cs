// 
// LayerManager.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
using System.ComponentModel;
using System.Collections.Specialized;
using System.Linq;
using Cairo;

namespace Pinta.Core
{
	public class LayerManager
	{
		#region Protected Methods
		protected internal void OnLayerAdded ()
		{
			if (LayerAdded != null)
				LayerAdded.Invoke (this, EventArgs.Empty);
		}

		protected internal void OnLayerRemoved ()
		{
			if (LayerRemoved != null)
				LayerRemoved.Invoke (this, EventArgs.Empty);
		}

		protected internal void OnSelectedLayerChanged ()
		{
			if (SelectedLayerChanged != null)
				SelectedLayerChanged.Invoke (this, EventArgs.Empty);
		}	
		#endregion

		#region Private Methods
		internal void RaiseLayerPropertyChangedEvent (object? sender, PropertyChangedEventArgs e)
		{
			if (LayerPropertyChanged != null)
				LayerPropertyChanged (sender, e);
			
			//TODO Get the workspace to subscribe to this event, and invalidate itself.
			PintaCore.Workspace.Invalidate ();
		}
		#endregion

		#region Events
		public event EventHandler? LayerAdded;
		public event EventHandler? LayerRemoved;
		public event EventHandler? SelectedLayerChanged;
		public event PropertyChangedEventHandler? LayerPropertyChanged;
		#endregion
	}
}
