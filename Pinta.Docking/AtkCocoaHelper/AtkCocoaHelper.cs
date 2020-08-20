//
// AtkCocoaHelper.cs
//
// Author:
//       Iain Holmes <iain@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc
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
using System.Runtime.InteropServices;

namespace Pinta.Docking.AtkCocoaHelper
{
	public static class AtkCocoaExtensions
	{
		public static void SetCommonAttributes (this Atk.Object o, string name, string label, string help)
		{
			if (!string.IsNullOrEmpty (name)) {
				o.Name = name;
			}
			if (!string.IsNullOrEmpty (help)) {
				o.Description = help;
			}
			if (!string.IsNullOrEmpty (label)) {
				o.SetLabel (label);
			}
		}

#if false
		public static void SetCommonAttributes (this Xwt.Accessibility.Accessible o, string? name, string? label, string? help)
		{
			if (!string.IsNullOrEmpty (name)) {
				o.Identifier = name;
			}

			if (!string.IsNullOrEmpty (label)) {
				o.Label = label;
			}

			if (!string.IsNullOrEmpty (help)) {
				o.Description = help;
			}
		}

		public static void SetCommonAccessibilityAttributes (this Xwt.Widget w, string? name, Xwt.Widget? label, string? help)
		{
			w.Accessible.SetCommonAttributes (name, null, help);
			if (label != null) {
				// FIXME Add relationship to Xwt
			}
		}

		public static void SetCommonAccessibilityAttributes (this Xwt.Widget w, string? name, string? label, string? help)
		{
			w.Accessible.SetCommonAttributes (name, label, help);
		}
#endif

		public static void SetCommonAccessibilityAttributes (this Gtk.Widget w, string name, string label, string help)
		{
			var accessible = w.Accessible;
			accessible.SetCommonAttributes (name, label, help);
		}

		public static void SetCommonAccessibilityAttributes (this Gtk.Widget w, string name, Gtk.Widget label, string help)
		{
			var accessible = w.Accessible;
			accessible.SetCommonAttributes (name, null, help);

			if (label != null) {
				w.SetAccessibilityLabelRelationship (label);
			}
		}

		public static void SetAccessibilityLabelRelationship (this Gtk.Widget w, Gtk.Widget label)
		{
			w.Accessible.SetTitleUIElement (label.Accessible);
			label.Accessible.SetTitleFor (w.Accessible);
		}

		public static void SetCurrentFocus (AccessibilityElementProxy focusElement)
		{
#if MAC
			AtkCocoaMacExtensions.SetCurrentFocus (focusElement);
#endif
		}
	}

	// AtkCocoaHelper wraps NSAccessibilityElement to set NSAccessibility properties that aren't supported by Atk
	public static class AtkCocoa
	{
		public enum Actions
		{
			AXCancel,
			AXConfirm,
			AXDecrement,
			AXDelete,
			AXIncrement,
			AXPick,
			AXPress,
			AXRaise,
			AXShowAlternateUI,
			AXShowDefaultUI,
			AXShowMenu
		};

		public enum Roles
		{
			AXButton,
			AXCell,
			AXColumn,
			AXGroup,
			AXGrowArea,
			AXImage,
			AXLink,
			AXList,
			AXMenuButton,
			AXPopUpButton,
			AXRadioButton,
			AXRow,
			AXRuler,
			AXSplitGroup,
			AXSplitter,
			AXStaticText,
			AXTabGroup,
			AXTextArea
		};

		public enum SubRoles
		{
			AXCloseButton,
			AXTabButton,
		};

		public struct Range
		{
			public int Location { get; set; }
			public int Length { get; set; }
		}
	}

	public class ActionDelegate
	{
		HashSet<AtkCocoa.Actions> actions = new HashSet<AtkCocoa.Actions> ();

		Atk.Object owner;

		void HandleSignalAttachment (Action<GLib.Signal, EventHandler<GLib.SignalArgs>> action)
		{
			// TODO-GTK3
#if false
			var signal = GLib.Signal.Lookup (owner, "request-actions", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (RequestActionsHandler));

			signal = GLib.Signal.Lookup (owner, "perform-cancel", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformCancelHandler));
			signal = GLib.Signal.Lookup (owner, "perform-confirm", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformConfirmHandler));
			signal = GLib.Signal.Lookup (owner, "perform-decrement", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformDecrementHandler));
			signal = GLib.Signal.Lookup (owner, "perform-delete", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformDeleteHandler));
			signal = GLib.Signal.Lookup (owner, "perform-increment", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformIncrementHandler));
			signal = GLib.Signal.Lookup (owner, "perform-pick", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformPickHandler));
			signal = GLib.Signal.Lookup (owner, "perform-press", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformPressHandler));
			signal = GLib.Signal.Lookup (owner, "perform-raise", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformRaiseHandler));
			signal = GLib.Signal.Lookup (owner, "perform-show-alternate-ui", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformShowAlternateUIHandler));
			signal = GLib.Signal.Lookup (owner, "perform-show-default-ui", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformShowDefaultUIHandler));
			signal = GLib.Signal.Lookup (owner, "perform-show-menu", typeof (GLib.SignalArgs));
			action (signal, new EventHandler<GLib.SignalArgs> (PerformShowMenuHandler));
#endif
		}

		public ActionDelegate (Gtk.Widget widget)
		{
			widget.Destroyed += WidgetDestroyed;

			owner = widget.Accessible;

			if (owner.GetType () == typeof (Atk.NoOpObject)) {
				return;
			}

			HandleSignalAttachment ((signal, handler) => signal.AddDelegate (handler));
		}

		void WidgetDestroyed (object sender, EventArgs e)
		{
			FreeActions ();

			HandleSignalAttachment ((signal, handler) => signal.RemoveDelegate (handler));
			owner = null;
		}

		// Because the allocated memory is passed to unmanaged code where it cannot be freed
		// we need to keep track of it until the object is finalized, or the actions need to be calculated again
		IntPtr allocatedActionPtr;
		IntPtr []? allocatedActionStrings;

		void FreeActions ()
		{
			if (allocatedActionStrings != null) {
				foreach (var ptr in allocatedActionStrings) {
					Marshal.FreeHGlobal (ptr);
				}
				allocatedActionStrings = null;
			}

			if (allocatedActionPtr != IntPtr.Zero) {
				Marshal.FreeHGlobal (allocatedActionPtr);
				allocatedActionPtr = IntPtr.Zero;
			}
		}

		void RegenerateActions ()
		{
			FreeActions ();

			// +1 so we can add a NULL to terminate the array
			int actionCount = actions.Count + 1;
			IntPtr intPtr = Marshal.AllocHGlobal (actionCount * Marshal.SizeOf<IntPtr> ());
			IntPtr [] actionsPtr = new IntPtr [actionCount];

			int i = 0;
			foreach (var action in actions) {
				actionsPtr [i] = Marshal.StringToHGlobalAnsi (action.ToString ());
				i++;
			}

			// Terminator
			actionsPtr [i] = IntPtr.Zero;

			Marshal.Copy (actionsPtr, 0, intPtr, actionCount);

			allocatedActionStrings = actionsPtr;
			allocatedActionPtr = intPtr;
		}

		void AddAction (AtkCocoa.Actions action)
		{
			if (owner.GetType () == typeof (Atk.NoOpObject)) {
				return;
			}

			actions.Add (action);
			RegenerateActions ();
		}

		void RemoveAction (AtkCocoa.Actions action)
		{
			if (owner.GetType () == typeof (Atk.NoOpObject)) {
				return;
			}

			actions.Remove (action);
			RegenerateActions ();
		}

		void RequestActionsHandler (object sender, GLib.SignalArgs args)
		{
			args.RetVal = allocatedActionPtr;
		}

		void PerformCancelHandler (object sender, GLib.SignalArgs args)
		{
			performCancel?.Invoke (this, args);
		}

		void PerformConfirmHandler (object sender, GLib.SignalArgs args)
		{
			performConfirm?.Invoke (this, args);
		}

		void PerformDecrementHandler (object sender, GLib.SignalArgs args)
		{
			performDecrement?.Invoke (this, args);
		}

		void PerformDeleteHandler (object sender, GLib.SignalArgs args)
		{
			performDelete?.Invoke (this, args);
		}

		void PerformIncrementHandler (object sender, GLib.SignalArgs args)
		{
			performIncrement?.Invoke (this, args);
		}

		void PerformPickHandler (object sender, GLib.SignalArgs args)
		{
			performPick?.Invoke (this, args);
		}

		void PerformPressHandler (object sender, GLib.SignalArgs args)
		{
			performPress?.Invoke (this, args);
		}

		void PerformRaiseHandler (object sender, GLib.SignalArgs args)
		{
			performRaise?.Invoke (this, args);
		}

		void PerformShowAlternateUIHandler (object sender, GLib.SignalArgs args)
		{
			performShowAlternateUI?.Invoke (this, args);
		}

		void PerformShowDefaultUIHandler (object sender, GLib.SignalArgs args)
		{
			performShowDefaultUI?.Invoke (this, args);
		}

		void PerformShowMenuHandler (object sender, GLib.SignalArgs args)
		{
			performShowMenu?.Invoke (this, args);
		}

		event EventHandler performCancel;
		public event EventHandler PerformCancel {
			add {
				if (performCancel == null) {
					AddAction (AtkCocoa.Actions.AXCancel);
				}
				performCancel += value;
			}
			remove {
				performCancel -= value;
				if (performCancel == null) {
					RemoveAction (AtkCocoa.Actions.AXCancel);
				}
			}
		}

		event EventHandler performConfirm;
		public event EventHandler PerformConfirm {
			add {
				if (performConfirm == null) {
					AddAction (AtkCocoa.Actions.AXConfirm);
				}
				performConfirm += value;
			}
			remove {
				performConfirm -= value;
				if (performConfirm == null) {
					RemoveAction (AtkCocoa.Actions.AXConfirm);
				}
			}
		}
		event EventHandler performDecrement;
		public event EventHandler PerformDecrement {
			add {
				if (performDecrement == null) {
					AddAction (AtkCocoa.Actions.AXDecrement);
				}
				performDecrement += value;
			}
			remove {
				performDecrement -= value;
				if (performDecrement == null) {
					RemoveAction (AtkCocoa.Actions.AXDecrement);
				}
			}
		}
		event EventHandler performDelete;
		public event EventHandler PerformDelete {
			add {
				if (performDelete == null) {
					AddAction (AtkCocoa.Actions.AXDelete);
				}
				performDelete += value;
			}
			remove {
				performDelete -= value;
				if (performDelete == null) {
					RemoveAction (AtkCocoa.Actions.AXDelete);
				}
			}
		}
		event EventHandler performIncrement;
		public event EventHandler PerformIncrement {
			add {
				if (performIncrement == null) {
					AddAction (AtkCocoa.Actions.AXIncrement);
				}
				performIncrement += value;
			}
			remove {
				performIncrement -= value;
				if (performIncrement == null) {
					RemoveAction (AtkCocoa.Actions.AXIncrement);
				}
			}
		}
		event EventHandler performPick;
		public event EventHandler PerformPick {
			add {
				if (performPick == null) {
					AddAction (AtkCocoa.Actions.AXPick);
				}
				performPick += value;
			}
			remove {
				performPick -= value;
				if (performPick == null) {
					RemoveAction (AtkCocoa.Actions.AXPick);
				}
			}
		}
		event EventHandler performPress;
		public event EventHandler PerformPress {
			add {
				if (performPress == null) {
					AddAction (AtkCocoa.Actions.AXPress);
				}
				performPress += value;
			}
			remove {
				performPress -= value;
				if (performPress == null) {
					RemoveAction (AtkCocoa.Actions.AXPress);
				}
			}
		}
		event EventHandler performRaise;
		public event EventHandler PerformRaise {
			add {
				if (performRaise == null) {
					AddAction (AtkCocoa.Actions.AXRaise);
				}
				performRaise += value;
			}
			remove {
				performRaise -= value;
				if (performRaise == null) {
					RemoveAction (AtkCocoa.Actions.AXRaise);
				}
			}
		}
		event EventHandler performShowAlternateUI;
		public event EventHandler PerformShowAlternateUI {
			add {
				if (performShowAlternateUI == null) {
					AddAction (AtkCocoa.Actions.AXShowAlternateUI);
				}
				performShowAlternateUI += value;
			}
			remove {
				performShowAlternateUI -= value;
				if (performShowAlternateUI == null) {
					RemoveAction (AtkCocoa.Actions.AXShowAlternateUI);
				}
			}
		}
		event EventHandler performShowDefaultUI;
		public event EventHandler PerformShowDefaultUI {
			add {
				if (performShowDefaultUI == null) {
					AddAction (AtkCocoa.Actions.AXShowDefaultUI);
				}
				performShowDefaultUI += value;
			}
			remove {
				performShowDefaultUI -= value;
				if (performShowDefaultUI == null) {
					RemoveAction (AtkCocoa.Actions.AXShowDefaultUI);
				}
			}
		}
		event EventHandler performShowMenu;
		public event EventHandler PerformShowMenu {
			add {
				if (performShowMenu == null) {
					AddAction (AtkCocoa.Actions.AXShowMenu);
				}
				performShowMenu += value;
			}
			remove {
				performShowMenu -= value;
				if (performShowMenu == null) {
					RemoveAction (AtkCocoa.Actions.AXShowMenu);
				}
			}
		}
	}

	// On anything other than Mac this is just a dummy class to prevent needing to have #ifdefs all over the main code
	public interface IAccessibilityElementProxy
	{
		event EventHandler PerformCancel;
		event EventHandler PerformConfirm;
		event EventHandler PerformDecrement;
		event EventHandler PerformDelete;
		event EventHandler PerformIncrement;
		event EventHandler PerformPick;
		event EventHandler PerformPress;
		event EventHandler PerformRaise;
		event EventHandler PerformShowAlternateUI;
		event EventHandler PerformShowDefaultUI;
		event EventHandler PerformShowMenu;

		Gtk.Widget GtkParent { get; set; }
		Gdk.Rectangle FrameInGtkParent { get; set; }
		Gdk.Rectangle FrameInParent { get; set; }

		void AddAccessibleChild (IAccessibilityElementProxy child);
		void RemoveAccessibleChild (IAccessibilityElementProxy child);

		void SetRole (string role, string? description = null);
		void SetRole (AtkCocoa.Roles role, string description = null);

		string Value { get; set; }
		string Title { get; set; }
		string Label { get; set; }
		string Identifier { get; set; }
		string Help { get; set; }
		bool Hidden { get; set; }
		int Index { get; set; }
		bool Focused { get; set; }

		// For Navigable Static Text
		Func<string> Contents { set; }
		Func<int> NumberOfCharacters { set; }
		Func<int> InsertionPointLineNumber { set; }
		Func<AtkCocoa.Range, Gdk.Rectangle> FrameForRange { set; }
		Func<int, int> LineForIndex { set; }
		Func<int, AtkCocoa.Range> RangeForLine { set; }
		Func<AtkCocoa.Range, string> StringForRange { set; }
		Func<int, AtkCocoa.Range> RangeForIndex { set; }
		Func<int, AtkCocoa.Range> StyleRangeForIndex { set; }
		Func<Gdk.Point, AtkCocoa.Range> RangeForPosition { set; }
	}
}
