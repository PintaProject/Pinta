//
// DockToolbarPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using Gdk;
using System.Collections;

namespace Pinta.Docking.DockToolbars
{
	internal class DockToolbarPanel: FixedPanel
	{
		DockToolbarFrame parentFrame;
		ArrayList bars = new ArrayList ();
		Orientation orientation;
		
		ArrowWindow placeholderArrow1;
		ArrowWindow placeholderArrow2;
		PlaceholderWindow placeholder;
		bool currentPlaceholderHorz;
		
		int dropOffset;
		int dropRow = -1;
		bool dropNewRow;
		bool enableAnimations = true;
		
		public DockToolbarPanel (DockToolbarFrame parentFrame, Placement placement)
		{
	//		ResizeMode = ResizeMode.Immediate;
			Placement = placement;
			switch (placement) {
				case Placement.Top:
					this.orientation = Orientation.Horizontal;
					break;
				case Placement.Bottom:
					this.orientation = Orientation.Horizontal;
					break;
				case Placement.Left:
					this.orientation = Orientation.Vertical;
					break;
				case Placement.Right:
					this.orientation = Orientation.Vertical;
					break;
			}
			
			this.parentFrame = parentFrame;
		}
		
		public Orientation Orientation {
			get { return orientation; }
		}
		
		public void AddDockToolbar (DockToolbar bar)
		{
			bool ea = EnableAnimation (false);

			Put (bar, 0, 0);
			bar.Orientation = orientation;
			
			if (bars.Count > 0 && IsRealized) {
				DockToolbar last = (DockToolbar) bars [bars.Count - 1];
				int width = bar.DefaultSize;
				int lastx = last.DockOffset + last.DefaultSize;
				
				if (lastx + width <= PanelWidth)
					MoveBar (bar, lastx, last.DockRow, false);
				else
					MoveBar (bar, 0, last.DockRow + 1, false);
				bar.AnchorOffset = bar.DockOffset;
				InternalAdd (bar);
				SortBars ();
			} else {
				MoveBar (bar, 0, 0);
				bar.AnchorOffset = bar.DockOffset;
				InternalAdd (bar);
			}

			EnableAnimation (ea);
		}
		
		public void AddDockToolbar (DockToolbar bar, int offset, int row)
		{
			bool ea = EnableAnimation (false);
			InternalAdd (bar);
			Put (bar, 0, 0);
			bar.Orientation = orientation;
			MoveBar (bar, offset, row, false);
			bar.AnchorOffset = offset;
			SortBars ();
			UpdateRowSizes (bar.DockRow);
			EnableAnimation (ea);
		}
		
		void InternalAdd (DockToolbar bar)
		{
			bars.Add (bar);
			bar.DefaultSizeChanged += new EventHandler (OnBarSizeChanged);
		}
		
		public void RemoveBar (DockToolbar bar)
		{
			if (IsSingleBarRow (bar))
				RemoveRow (bar.DockRow);
		
			Remove (bar);
			bars.Remove (bar);
			bar.DefaultSizeChanged -= new EventHandler (OnBarSizeChanged);
			
			UpdateRowHeight (bar.DockRow);
			PackBars ();
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			ResetBarPositions (false);
		}

		public void ResetBarPositions (bool animate)
		{
			int x=0, row=0;
			int width = PanelWidth;
			
			bool ea = EnableAnimation (animate);
			
			foreach (DockToolbar b in bars) {
				int barw = GetChildWidth (b);
				if (x + barw < width)
					MoveBar (b, x, row);
				else {
					row++;
					x = 0;
					MoveBar (b, 0, row);
				}
				x += barw;
			}
			SortBars ();
			EnableAnimation (ea);
		}
		
		void SetPlaceholder (DockToolbar bar, int offset, int row)
		{
			if (dropRow != row && dropRow != -1)
				RestoreShiftedBars (dropRow);

			ShowPlaceholder (bar, false, offset, GetRowTop (row), GetChildWidth (bar), GetRowSize (row));

			dropOffset = offset;
			dropRow = row;
			dropNewRow = false;
		}
		
		void SetNewRowPlaceholder (DockToolbar bar, int offset, int toprow)
		{
			if (dropRow != -1)
				RestoreShiftedBars (dropRow);
			
			int y = GetRowTop (toprow) - parentFrame.DockMargin;
			int h = parentFrame.DockMargin * 2;
			ShowPlaceholder (bar, true, offset, y, GetChildWidth (bar), h);
			
			dropOffset = offset;
			dropRow = toprow;
			dropNewRow = true;
		}
		
		void ShowPlaceholder (DockToolbar bar, bool horz, int x, int y, int w, int h)
		{
			if (orientation != Orientation.Horizontal)
				horz = !horz;
			
			PanelToWindow (x, y, w, h, out x, out y, out w, out h);
			
			bool created = false;
			
			if (placeholder == null || horz != currentPlaceholderHorz) {
				HidePlaceholder ();
				placeholder = new PlaceholderWindow (parentFrame);
				placeholderArrow1 = new ArrowWindow (parentFrame, horz ? ArrowWindow.Direction.Right : ArrowWindow.Direction.Down);
				placeholderArrow2 = new ArrowWindow (parentFrame, horz ? ArrowWindow.Direction.Left : ArrowWindow.Direction.Up);
				currentPlaceholderHorz = horz;
				created = true;
			}
			
			int sx, sy;
			this.GdkWindow.GetOrigin (out sx, out sy);
			sx += x;
			sy += y;
			
			int mg = -4;
			placeholder.Move (sx - mg, sy - mg);
			placeholder.Resize (w + mg*2, h + mg * 2);
			
			if (horz) {
				placeholderArrow1.Move (sx - placeholderArrow1.Width, sy + (h/2) - placeholderArrow1.Height/2);
				placeholderArrow2.Move (sx + w, sy + (h/2) - placeholderArrow1.Height/2);
			} else {
				int px = sx + w/2 - placeholderArrow1.Width/2;
				if (px < 0) px = 0;
				placeholderArrow1.Move (px, sy - placeholderArrow1.Height);
				placeholderArrow2.Move (px, sy + h);
			}
			
			if (created) {
				placeholder.Show ();
				placeholder.Present ();
				if (bar.FloatingDock != null)
					bar.FloatingDock.Present ();
				placeholderArrow1.Present ();
				placeholderArrow2.Present ();
			}
		}
		
		void HidePlaceholder ()
		{
			if (placeholder == null) return;
			placeholder.Destroy ();
			placeholder = null;
			placeholderArrow1.Destroy ();
			placeholderArrow1 = null;
			placeholderArrow2.Destroy ();
			placeholderArrow2 = null;

			if (dropRow != -1 && !dropNewRow) {
				RestoreShiftedBars (dropRow);
				dropRow = -1;
			}
		}
		
		bool IsPlaceHolderVisible {
			get { return placeholder != null; }
		}
		
		public void StartDragBar (DockToolbar bar)
		{
		}
		
		public void DropDragBar (DockToolbar bar)
		{
			if (!IsPlaceHolderVisible) return;
			
			foreach (DockToolbar b in bars) {
				if (b.DockRow == dropRow && b.DockShiftOffset != -1) {
					b.DockShiftOffset = -1;
					b.AnchorOffset = b.DockRow;
				}
			}
			
			if (dropRow != -1) {
				if (dropNewRow)
					InsertRow (bar, dropOffset, dropRow);
				else {
					MoveBar (bar, dropOffset, dropRow);
					UpdateRowHeight (dropRow);
				}
				SortBars ();
				dropRow = -1;
			}
		}
		
		public void EndDragBar (DockToolbar bar)
		{
			if (IsPlaceHolderVisible) {
				HidePlaceholder ();
			}
		}
		
		void RestoreShiftedBars (int row)
		{
			foreach (DockToolbar b in bars) {
				if (b.DockRow == row && b.DockShiftOffset != -1) {
					MoveBar (b, b.DockShiftOffset, b.DockRow, false);
					b.DockShiftOffset = -1;
				}
			}
		}
		
		public void Reposition (DockToolbar bar, int xcursor, int ycursor, int difx, int dify)
		{
			if (!bar.CanDockTo (this))
				return;

			bar.Orientation = orientation;
			
			int x, y;
			WindowToPanel (xcursor + difx, ycursor + dify, bar.Allocation.Width, bar.Allocation.Height, out x, out y);
			WindowToPanel (xcursor, ycursor, 0, 0, out xcursor, out ycursor);
			
			RepositionInternal (bar, x, y, xcursor, ycursor);
		}
		
		void RepositionInternal (DockToolbar bar, int x, int y, int xcursor, int ycursor)
		{
			int width = GetChildWidth (bar);
			
			ycursor = y + bar.DefaultHeight / 2;
			
			if (bars.Count == 0 && bar.Floating) {
				SetNewRowPlaceholder (bar, x, 0);
				return;
			}
			
			int dx = (x + width) - PanelWidth;
			if (dx > parentFrame.DockMargin && !bar.Floating) {
				HidePlaceholder ();
				FloatBar (bar, x, y);
				return;
			}
			else if (dx > 0)
				x -= dx;
			else if (x < -parentFrame.DockMargin && !bar.Floating) {
				HidePlaceholder ();
				FloatBar (bar, x, y);
				return;
			}
			else if (x < 0)
				x = 0;

			int nx = x;
			int row = -1;
			
			// Get the old bar y position
			 
			int panelBottom = GetPanelBottom ();
				
			if (ycursor < - parentFrame.DockMargin || ycursor > panelBottom + parentFrame.DockMargin) {
				HidePlaceholder ();
				FloatBar (bar, x, y);
				return;
			}
			
			int rtop = 0;
			int prevtop = 0;
			row = 0;
			while (ycursor >= rtop) {
				prevtop = rtop;
				row++;
				if (rtop >= panelBottom) break;
				rtop += GetRowSize (row - 1);
			}
			
			row--;
			int ry = ycursor - prevtop;
			
			if (ry <= parentFrame.DockMargin && ry >= 0) {
				SetNewRowPlaceholder (bar, x, row);
				FloatBar (bar, x, y);
				return;
			} else if (ry >= (GetRowSize(row) - parentFrame.DockMargin) || (ry < 0 && -ry < parentFrame.DockMargin)) {
				SetNewRowPlaceholder (bar, x, row + 1);
				FloatBar (bar, x, y);
				return;
			}
			
			// Can't create a new row. Try to fit the bar in the current row
			// Find the first bar in the row:
			
			int ns = -1;
			for (int n=0; n<bars.Count; n++) {
				DockToolbar b = (DockToolbar)bars[n];
				
				// Ignore the bar being moved
				if (b == bar) continue;
				
				if (b.DockRow == row) {
					ns = n;
					break;
				}
			}
			
			if (ns == -1) {
				// There are no other bars, no problem then
				if (bar.Floating) {
					SetPlaceholder (bar, nx, row);
					return;
				}

				if ((nx == bar.DockOffset && row == bar.DockRow) || (row != bar.DockRow)) {
					SetPlaceholder (bar, nx, row);
					FloatBar (bar, x, y);
					return;
				}
				
				HidePlaceholder ();
				MoveBar (bar, nx, row);
				return;
			}
			
			// Compute the available space, and find the bars at the
			// left and the right of the bar being moved
			
			int gapsTotal = 0;
			int lastx = 0;
			int leftIndex=-1, rightIndex = -1;
			int gapsLeft = 0, gapsRight = 0;
			
			for (int n=ns; n<bars.Count; n++) {
				DockToolbar b = (DockToolbar)bars[n];
				
				// Ignore the bar being moved
				if (b == bar) continue;

				if (b.DockRow != row) break;
				int bx = b.DockOffset;
				
				if (bx > x && (rightIndex == -1))
					rightIndex = n;
				else if (bx <= x)
					leftIndex = n;
				
				if (bx < x)
					gapsLeft += bx - lastx;
				else {
					if (lastx < x) {
						gapsLeft += x - lastx;
						gapsRight += bx - x;
					} else
						gapsRight += bx - lastx;
				}
				
				gapsTotal += bx - lastx;
				lastx = GetChildRightOffset (b); 
			}

			if (lastx < x) {
				gapsLeft += x - lastx;
				gapsRight += PanelWidth - x;
			} else {
				gapsRight += PanelWidth - lastx;
			}
			
			gapsTotal += PanelWidth - lastx;
			
			// Is there room for the bar? 
			if (gapsTotal < width) {
				HidePlaceholder ();
				FloatBar (bar, x, y);
				return;
			}
			
			// Shift the bars at the left and the right
			
			int oversizeLeft = 0;
			int oversizeRight = 0;
			
			if (leftIndex != -1) {
				int r = GetChildRightOffset ((DockToolbar) bars [leftIndex]);
				oversizeLeft = r - nx;
			}
			
			if (rightIndex != -1) {
				int r = ((DockToolbar) bars [rightIndex]).DockOffset;
				oversizeRight = (nx + width) - r;
			}
			
			if (oversizeLeft > gapsLeft)
				oversizeRight += (oversizeLeft - gapsLeft);
			else if (oversizeRight > gapsRight)
				oversizeLeft += (oversizeRight - gapsRight);
			
			if (leftIndex != -1 && oversizeLeft > 0) {
				ShiftBar (leftIndex, -oversizeLeft);
				nx = GetChildRightOffset ((DockToolbar) bars [leftIndex]);
			}
			
			if (rightIndex != -1 && oversizeRight > 0) {
				ShiftBar (rightIndex, oversizeRight);
				nx = ((DockToolbar) bars [rightIndex]).DockOffset - width;
			}
			
			
			if (bar.Floating) {
				SetPlaceholder (bar, nx, row);
				return;
			}

			if ((nx == bar.DockOffset && row == bar.DockRow) || (row != bar.DockRow)) {
				if (bar.Floating) {
					SetPlaceholder (bar, nx, row);
					FloatBar (bar, x, y);
				}
				return;
			}
			
			HidePlaceholder ();
			MoveBar (bar, nx, row);
		}
		
		void FloatBar (DockToolbar bar, int x, int y)
		{
			if (bar.Floating) return;
			
			int wx,wy,w,h;
			PanelToWindow (x, y, GetChildWidth (bar), bar.DefaultHeight, out x, out y, out w, out h);
				
			this.GdkWindow.GetOrigin (out wx, out wy);
			RemoveBar (bar);
			parentFrame.FloatBar (bar, orientation, wx + x, wy + y);
		}
		
		void ShiftBar (int index, int size)
		{
			DockToolbar bar = (DockToolbar) bars [index];
			if (bar.DockShiftOffset == -1)
				bar.DockShiftOffset = bar.DockOffset;
			
			if (size > 0) {
				int rp = GetChildRightOffset (bar);
				int gap = PanelWidth - rp;
				if (index + 1 < bars.Count) {
					DockToolbar obar = (DockToolbar) bars [index + 1];
					if (bar.DockRow == obar.DockRow) {
						gap = obar.DockOffset - rp;
						if (gap < size) {
							ShiftBar (index + 1, size - gap);
							gap = obar.DockOffset - rp;
						}
					}
				}
				if (gap > size)
					gap = size;
				if (gap > 0)
					MoveBar (bar, bar.DockOffset + gap, bar.DockRow, false);
			} else {
				size = -size;
				int lp = bar.DockOffset;
				int gap = lp;
				if (index > 0) {
					DockToolbar obar = (DockToolbar) bars [index - 1];
					if (bar.DockRow == obar.DockRow) {
						gap = lp - GetChildRightOffset (obar);
						if (gap < size) {
							ShiftBar (index - 1, gap - size);
							gap = lp - GetChildRightOffset (obar);
						}
					}
				}
				
				if (gap > size)
					gap = size;
				if (gap > 0)
					MoveBar (bar, bar.DockOffset - gap, bar.DockRow, false);
			}
		}
		
		void MoveBar (DockToolbar bar, int x, int row)
		{
			MoveBar (bar, x, row, true);
		}
		
		void MoveBar (DockToolbar bar, int x, int row, bool setAnchorOffset)
		{
			int rt = GetRowTop (row);

			bar.DockRow = row;
			bar.DockOffset = x;
			
			if (bar.Floating) {
				FloatingDock win = bar.FloatingDock;
				win.Detach ();
				win.Destroy ();
				
				InternalAdd (bar);
				Put (bar, x, rt);
				SortBars ();
				ResetAnchorOffsets (row);
				
			} else {
				if (setAnchorOffset)
					ResetAnchorOffsets (row);

				InternalMove (bar, x, rt, true);
			}
		}
		
		void ResetAnchorOffsets (int row)
		{
			for (int n=0; n<bars.Count; n++) {
				DockToolbar b = (DockToolbar) bars [n];
				if (b.DockRow < row) continue;
				if (b.DockRow > row) return;
				b.AnchorOffset = b.DockOffset;
			}
		}
		
		void UpdateRowHeight (int row)
		{
			int nr = row + 1;
			bool ea = EnableAnimation (false);
			for (int n=0; n<bars.Count; n++) {
				DockToolbar b = (DockToolbar) bars [n];
				if (b.DockRow < nr) continue;
				MoveBar (b, b.DockOffset, b.DockRow);
			}
			EnableAnimation (ea);
		}
		
		void OnBarSizeChanged (object s, EventArgs e)
		{
			if (IsRealized)
				UpdateRowSizes (((DockToolbar)s).DockRow);
		}
		
		void UpdateRowSizes (int row)
		{
			int lastx = 0;
			for (int n=0; n<bars.Count; n++) {
				DockToolbar b = (DockToolbar) bars [n];
				if (b.DockRow < row) continue;
				if (b.DockRow > row) break;
				if (b.AnchorOffset < lastx)
					b.AnchorOffset = lastx;
				lastx = b.AnchorOffset + b.DefaultSize;
			}
			PackBars ();
		}
		
		protected override void OnSizeAllocated (Rectangle rect)
		{
			Rectangle oldRect = Allocation;
			base.OnSizeAllocated (rect);
			
			if (!rect.Equals (oldRect))
				PackBars ();
		}
		
		void PackBars ()
		{
			bool ea = EnableAnimation (false);
			int n=0;
			while (n < bars.Count)
				n = PackRow (n);
			EnableAnimation (ea);
		}
		
		int PackRow (int sn)
		{
			// The 'sn' parameter is the index if the first toolbar of the row.
			// It returns the index of the first toolbar of the next row
			
			int n = sn;
			int row = ((DockToolbar)bars[n]).DockRow;
			int lastx = 0;
			int gaps = 0;
			
			// Calculates the free space in the row
			
			while (n < bars.Count) {
				DockToolbar bar = (DockToolbar) bars [n];
				if (bar.DockRow != row) break;
				
				if (bar.AnchorOffset > lastx)
					gaps += bar.AnchorOffset - lastx;
				
				lastx = bar.AnchorOffset + bar.DefaultSize;
				n++;
			}

			if (lastx <= PanelWidth) {
				for (int i=sn; i<n; i++) {
					DockToolbar b = (DockToolbar) bars[i];
					if (b.AnchorOffset != b.DockOffset)
						MoveBar (b, b.AnchorOffset, b.DockRow, false);
					if (b.Size != b.DefaultSize) {
						b.ShowArrow = false;
						b.Size = b.DefaultSize;
					}
				}
				return n;
			}
			
			int barsSize = lastx - gaps;
			double gapShrink = 0;
			
			int[] sizeReduction = new int [n - sn];
			
			if (barsSize > PanelWidth) {
				int remSize = barsSize - PanelWidth;
				while (remSize > 0) {
					int oldRemSize = remSize;
					for (int k=n-1; k >= sn; k--) {
						int pos = k - sn;
						DockToolbar bar = (DockToolbar) bars [k];
						int newReduction = GetBarReduction (bar, remSize, sizeReduction [pos]);
						sizeReduction [pos] += newReduction;
						remSize -= newReduction;
					}
					if (oldRemSize == remSize)
						break;
				}
			}
			else
				gapShrink = ((double)(PanelWidth - barsSize)) / (double)gaps;
			
			lastx = 0;
			int newlastx = 0;
			for (int i=sn; i < n; i++) {
				DockToolbar bar = (DockToolbar) bars [i];
				int gap = bar.AnchorOffset - lastx;
				lastx = bar.AnchorOffset + bar.DefaultSize;
				
				int nx = (int)(newlastx + ((double)gap * gapShrink));
				if (nx != bar.DockOffset)
					MoveBar (bar, nx, bar.DockRow, false);
				
				int nw = bar.DefaultSize - sizeReduction [i - sn];
				if (nw != bar.Size) {
					bar.ShowArrow = nw != bar.DefaultSize;
					bar.Size = nw;
				}
				newlastx = bar.DockOffset + nw;
			}
			
			return n;
		}
		
		int GetBarReduction (DockToolbar bar, int sizeToReduce, int currentReduction)
		{
			Gtk.Widget[] children = bar.Children;
			int w = bar.DefaultSize;
			int arrowSize = 0;
			bar.Forall (delegate (Gtk.Widget wa) {
				if (wa is ToggleButton) {
					arrowSize = bar.Orientation == Orientation.Horizontal ? wa.SizeRequest ().Width : wa.SizeRequest ().Height;
				}
			});
			for (int n=children.Length - 1; n >= 1; n--) {
				if (!children [n].Visible)
					continue;
				int x;
				if (bar.Orientation == Orientation.Horizontal)
					x = children [n].Allocation.X - bar.Allocation.X;
				else
					x = children [n].Allocation.Y - bar.Allocation.Y;
				int rightSize = w - x - 1;
				if (rightSize > currentReduction) {
					int prevRequest = Orientation == Orientation.Horizontal ? children [n-1].SizeRequest ().Width : children [n-1].SizeRequest ().Height;
					if (prevRequest > arrowSize*2)
						continue;
					int newReduction = rightSize - currentReduction;
					if (newReduction > sizeToReduce)
						newReduction = sizeToReduce;
					
					int minSize = Orientation == Orientation.Horizontal ? children[0].SizeRequest ().Width : children[0].SizeRequest ().Height;
					minSize += arrowSize + 6;
					if (bar.DefaultSize - (currentReduction + newReduction) < minSize)
						newReduction = (bar.DefaultSize - minSize) - currentReduction;
					
					return newReduction;
				}
			}
			return 0;
		}
		
		int GetPanelBottom ()
		{
			if (bars.Count > 0) {
				DockToolbar bar = (DockToolbar) bars [bars.Count - 1];
				return GetRowTop (bar.DockRow + 1);
			}
			else
				return 0;
		} 
		
		bool IsSingleBarRow (DockToolbar bar)
		{
			int row = bar.DockRow;
			foreach (DockToolbar b in bars) {
				if (bar != b && b.DockRow == row)
					return false;
			}
			return true;
		}
		
		void InsertRow (DockToolbar ibar, int offset, int row)
		{
			MoveBar (ibar, offset, row);
			foreach (DockToolbar bar in bars) {
				if (ibar != bar && bar.DockRow >= row)
					bar.DockRow++;
			}
			SortBars ();
			UpdateRowHeight (row);
		}
		
		void RemoveRow (int row)
		{
			foreach (DockToolbar bar in bars) {
				if (bar.DockRow >= row)
					MoveBar (bar, bar.DockOffset, bar.DockRow - 1, false);
			}
		}
		
		int GetChildRightOffset (DockToolbar bar)
		{
			return bar.DockOffset + bar.Size;
		}
		
		int GetRowSize (int row)
		{
			int max = 0;
			for (int n=0; n<bars.Count; n++) {
				DockToolbar b = (DockToolbar) bars [n];
				if (b.DockRow < row) continue;
				if (b.DockRow > row) return max;
				max = Math.Max (b.DefaultHeight, max);
			}
			return max;
		}
		
		int GetRowTop (int row)
		{
			int t = 0;
			for (int n=0; n < row; n++)
				t += GetRowSize (n);
			return t;
		}
		
		int MaxRow {
			get {
				var lastBar = (DockToolbar)bars[bars.Count -1];
				return lastBar.DockRow;
			}
		}
		
		void SortBars ()
		{
			bars.Sort (DocBarComparer.Instance);
		}
		
		void InternalMove (DockToolbar bar, int x, int y, bool animate)
		{
			if (bar.Animation != null) {
				bar.Animation.Cancel ();
				bar.Animation = null;
			}
			
			if (animate && enableAnimations) {
				bar.Animation = new MoveAnimation (this, bar, x, y);
				bar.Animation.Start ();
			}
			else
				Move (bar, x, y);
		}
		
		bool EnableAnimation (bool enable)
		{
			bool r = enableAnimations;
			enableAnimations = enable;
			return r;
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			//leave this plain unless there are horizontal children, so that the panel matches the toolbars
			//see DockToolbar.OnExposeEvent
			if (bars.Count == 0 || this.Orientation != Orientation.Horizontal)
				return base.OnExposeEvent (evnt);
			
			// get a proper GTK+ toolbar style by using one of the children, i.e. a DockToolbar, a subclass of GtkToolbar
			var styleProvider = (Widget)bars[0];
			var shadowType = (ShadowType) styleProvider.StyleGetProperty ("shadow-type");
			
			//render each row separately, so the theme treats each as a row, and they match the individual toolbars' themed painting
			int row = MaxRow;
			for (int i = bars.Count - 1; i >= 0; i--) {
				var bar = (DockToolbar) bars[i];
				if (bar.DockRow == row) {
					row--;
					//HACK: On Mac, PaintBox seems to fill the whole area instead of using it for clipping
					//so we create a new area for the row, to stop it overpainting other rows
					//otoh this could improve performance when only single rows are repainted, because we can intersect with invalidated area
					var rowArea = new Rectangle (Allocation.X, bar.Allocation.Y, Allocation.Width, bar.Allocation.Height);
					var invalidated = Gdk.Rectangle.Intersect (evnt.Area, rowArea);
					Style.PaintBox (bar.Style, evnt.Window, State, shadowType, invalidated, bar, "toolbar", 
					                rowArea.X, rowArea.Y, rowArea.Width, rowArea.Height);
				}
			}
			
			foreach (DockToolbar bar in bars)
				this.PropagateExpose (bar, evnt);
			return true;
		}
		
		//ensures that all widgets in each row have consistent allocation
		public override Requisition GetChildRequisition (Widget w)
		{
			var r = w.ChildRequisition;
			var t = (DockToolbar) w;
			var size = GetRowSize (t.DockRow);
			if (orientation == Orientation.Horizontal) {
				r.Height = size;
			} else {
				r.Width = size;
			}
			return r;
		}
	}


	internal class DocBarComparer: IComparer
	{
		internal static DocBarComparer Instance = new DocBarComparer (); 
		
		public int Compare (object a, object b)
		{
			DockToolbar b1 = (DockToolbar) a;
			DockToolbar b2 = (DockToolbar) b;

			if (b1.DockRow < b2.DockRow) return -1;
			else if (b1.DockRow > b2.DockRow) return 1;
			else if (b1.DockOffset < b2.DockOffset) return -1;
			else if (b1.DockOffset > b2.DockOffset) return 1;
			else return 0;
		} 
	}

	internal abstract class AnimationManager
	{
		static ArrayList anims = new ArrayList ();
		static int s = 0;
		
		public static void Animate (Animation a)
		{
			if (anims.Count == 0)
				GLib.Timeout.Add (10, new GLib.TimeoutHandler (Animate));
			anims.Add (a);
		}
		
		public static void CancelAnimation (Animation a)
		{
			anims.Remove (a);
		}
		
		public static bool Animate ()
		{
			s++;
			ArrayList toDelete = new ArrayList ();
			foreach (Animation a in anims)
				if (!a.Run ())
					toDelete.Add (a);
			
			foreach (object ob in toDelete)
				anims.Remove (ob);

			return anims.Count != 0;
		}
	}

	internal abstract class Animation
	{
		protected Widget widget;
		
		public Animation (Widget w)
		{
			widget = w;
		}
		
		public void Start ()
		{
			AnimationManager.Animate (this);
		}
		
		public void Cancel ()
		{
			AnimationManager.CancelAnimation (this);
		}
		
		internal protected abstract bool Run ();
	}

	internal class MoveAnimation: Animation
	{
		FixedPanel panel;
		float destx, desty;
		float curx, cury;
		
		public MoveAnimation (FixedPanel f, Widget w, int destx, int desty): base (w)
		{
			panel = f;
			int x, y;
			f.GetPosition (w, out x, out y);
			curx = (float)x;
			cury = (float)y;
			this.destx = (float) destx;
			this.desty = (float) desty;
		}
		
		internal protected override bool Run ()
		{
			float dx = destx - curx;
			float dy = desty - cury;
			
			dx = dx / 4;
			dy = dy / 4;
			
			curx += dx;
			cury += dy;
			
			panel.Move (widget, (int)curx, (int)cury);
			
			if(Math.Abs (dx) < 0.1 && Math.Abs (dy) < 0.1) {
				panel.Move (widget, (int)destx, (int)desty);
				return false;
			} else
				return true;
		}
	}
}
