//
// AtkCocoaHelperNoOp.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2017 
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
using Gdk;
using Gtk;

namespace Pinta.Docking.AtkCocoaHelper
{
	public static class AtkCocoaNoopExtensions
	{
		public static void SetLabel (this Atk.Object o, string label)
		{
		}

		public static void SetLabel (this Gtk.CellRenderer r, string label)
		{
		}

		public static void SetDescription (this Gtk.CellRenderer r, string description)
		{
		}

		public static void SetShouldIgnore (this Atk.Object o, bool ignore)
		{
		}

		public static void SetTitle (this Atk.Object o, string title)
		{
		}

		public static void SetDocument (this Atk.Object o, string documentUrl)
		{
		}

		public static void SetFilename (this Atk.Object o, string filename)
		{
		}

		public static void SetIsMainWindow (this Atk.Object o, bool isMainWindow)
		{
		}

		public static void SetMainWindow (this Atk.Object o, Atk.Object mainWindow)
		{
		}

		public static void SetValue (this Atk.Object o, string stringValue)
		{
		}

		public static void SetUrl (this Atk.Object o, string url)
		{
		}

		public static void SetRole (this Atk.Object o, string role, string description = null)
		{
		}

		public static void SetRole (this Atk.Object o, AtkCocoa.Roles role, string description = null)
		{
		}

		public static void SetSubRole (this Atk.Object o, string subrole)
		{
		}

		public static void SetTitleUIElement (this Atk.Object o, Atk.Object title)
		{
		}

		public static void SetAlternateUIVisible (this Atk.Object o, bool visible)
		{
		}

		public static void SetOrientation (this Atk.Object o, Gtk.Orientation orientation)
		{
		}

		public static void SetTitleFor (this Atk.Object o, params Atk.Object [] objects)
		{
		}

		public static void SetTabs (this Atk.Object o, AccessibilityElementProxy [] tabs)
		{
		}

		public static void SetTabs (this Atk.Object o, Atk.Object [] tabs)
		{
		}

		public static void AddElementToTitle (this Atk.Object title, Atk.Object o)
		{
		}

		public static void RemoveElementFromTitle (this Atk.Object title, Atk.Object o)
		{
		}

		public static void ReplaceAccessibilityElements (this Atk.Object parent, AccessibilityElementProxy [] children)
		{
		}

		public static void SetColumns (this Atk.Object parent, AccessibilityElementProxy [] columns)
		{
		}

		public static void SetRows (this Atk.Object parent, AccessibilityElementProxy [] rows)
		{
		}

		public static void AddAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
		}

		public static void RemoveAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
		}

		public static void SetAccessibleChildren (this Atk.Object o, AccessibilityElementProxy [] children)
		{
		}

		public static void AddLinkedUIElement (this Atk.Object o, Atk.Object linked)
		{
		}

		public static void AddLinkedUIElement (this Atk.Object o, params Atk.Object [] linked)
		{
		}
	
		public static void MakeAccessibilityAnnouncement (this Atk.Object o,  string message)
		{
		}

		public static void TransferAccessibleChild (this Atk.Object from, Atk.Object to, Atk.Object child)
		{
		}
	}

	public class AccessibilityElementProxy : IAccessibilityElementProxy
	{
		public AccessibilityElementProxy ()
		{
		}

		public static AccessibilityElementProxy ButtonElementProxy ()
		{
			return new AccessibilityElementProxy ();
		}

		public static AccessibilityElementProxy TextElementProxy ()
		{
			return new AccessibilityElementProxy ();
		}

		public static bool Enabled {
			get {
				return false;
			}
		}
		
		public string Identifier {
			get {
				return null;
			}
			set {
			}
		}

		public string Help {
			get {
				return null;
			}
			set {
			}
		}

		public string Label {
			get {
				return null;
			}
			set {
			}
		}

		public string Title {
			get {
				return null;
			}
			set {
			}
		}

		public string Value {
			get {
				return null;
			}
			set {
			}
		}

		public bool Hidden {
			get {
				return false;
			}
			set {
			}
		}

		public int Index {
			get {
				return 0;
			}
			set {
			}
		}

		public bool Focused {
			get {
				return false;
			}
			set {
			}
		}

		public Gtk.Widget GtkParent { get; set; }
		public Gdk.Rectangle FrameInGtkParent { get; set; }
		public Gdk.Rectangle FrameInParent { get; set; }

		public void AddAccessibleChild (IAccessibilityElementProxy child)
		{
		}

		public void RemoveAccessibleChild (IAccessibilityElementProxy child)
		{
		}

		public void SetRole (string role, string description = null)
		{
		}

		public void SetRole (AtkCocoa.Roles role, string description = null)
		{
		}

#pragma warning disable 67 // never used
		public event EventHandler PerformCancel;
		public event EventHandler PerformConfirm;
		public event EventHandler PerformDecrement;
		public event EventHandler PerformDelete;
		public event EventHandler PerformIncrement;
		public event EventHandler PerformPick;
		public event EventHandler PerformPress;
		public event EventHandler PerformRaise;
		public event EventHandler PerformShowAlternateUI;
		public event EventHandler PerformShowDefaultUI;
		public event EventHandler PerformShowMenu;
#pragma warning restore 67

		// For Navigable Text elements
		public Func<string> Contents {
			set {
			}
		}
		public Func<int> NumberOfCharacters {
			set {
			}
		}
		public Func<int> InsertionPointLineNumber {
			set {
			}
		}
		public Func<AtkCocoa.Range, Rectangle> FrameForRange {
			set {
			}
		}
		public Func<int, int> LineForIndex {
			set {
			}
		}
		public Func<int, AtkCocoa.Range> RangeForLine {
			set {
			}
		}
		public Func<AtkCocoa.Range, string> StringForRange {
			set {
			}
		}
		public Func<int, AtkCocoa.Range> RangeForIndex {
			set {
			}
		}
		public Func<int, AtkCocoa.Range> StyleRangeForIndex {
			set {
			}
		}
		public Func<Point, AtkCocoa.Range> RangeForPosition {
			set {
			}
		}
		
		public Func<AtkCocoa.Range> GetVisibleCharacterRange {
			set {
			}
		}
	}
}
