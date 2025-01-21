// 
// PaintBrushManager.cs
//  
// Author:
//       Aaron Bockover <abockover@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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

namespace Pinta.Core;

public interface IPaintBrushService : IEnumerable<BasePaintBrush>
{
	event EventHandler<BrushEventArgs>? BrushAdded;
	event EventHandler<BrushEventArgs>? BrushRemoved;
}

public sealed class PaintBrushManager : IPaintBrushService
{
	private readonly List<BasePaintBrush> paint_brushes = [];

	public event EventHandler<BrushEventArgs>? BrushAdded;
	public event EventHandler<BrushEventArgs>? BrushRemoved;

	/// <summary>
	/// Register a new brush.
	/// </summary>
	public void AddPaintBrush (BasePaintBrush paintBrush)
	{
		paint_brushes.Add (paintBrush);
		paint_brushes.Sort (new BrushSorter ());
		OnBrushAdded (paintBrush);
	}

	/// <summary>
	/// Remove a brush type.
	/// </summary>
	public void RemoveInstanceOfPaintBrush (System.Type paintBrush)
	{
		foreach (BasePaintBrush brush in paint_brushes) {
			if (brush.GetType () == paintBrush) {
				paint_brushes.Remove (brush);
				paint_brushes.Sort (new BrushSorter ());
				OnBrushRemoved (brush);
				return;
			}
		}
	}

	#region IEnumerable<BasePaintBrush> implementation
	public IEnumerator<BasePaintBrush> GetEnumerator ()
	{
		return paint_brushes.GetEnumerator ();
	}
	#endregion

	#region IEnumerable implementation
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
	{
		return paint_brushes.GetEnumerator ();
	}
	#endregion

	private void OnBrushAdded (BasePaintBrush brush)
	{
		BrushAdded?.Invoke (this, new BrushEventArgs (brush));
	}

	private void OnBrushRemoved (BasePaintBrush brush)
	{
		BrushRemoved?.Invoke (this, new BrushEventArgs (brush));
	}

	private sealed class BrushSorter : Comparer<BasePaintBrush>
	{
		public override int Compare (BasePaintBrush? x, BasePaintBrush? y)
		{
			var xstr = x is BasePaintBrush x2 && x2.Priority != 0 ? x2.Priority.ToString () : x?.Name;
			var ystr = y is BasePaintBrush y2 && y2.Priority != 0 ? y2.Priority.ToString () : y?.Name;

			return string.Compare (xstr, ystr);
		}
	}
}

