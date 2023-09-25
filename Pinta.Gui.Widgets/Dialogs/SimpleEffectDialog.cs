//
// SimpleEffectDialog.cs
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
//
// Inspiration and reflection code is from Miguel de Icaza's MIT Licensed MonoTouch.Dialog:
// http://github.com/migueldeicaza/MonoTouch.Dialog

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Gtk;
using Mono.Addins.Localization;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class SimpleEffectDialog : Gtk.Dialog
{
	const uint Event_delay_millis = 100;
	uint event_delay_timeout_id;

	private delegate bool TimeoutHandler ();
	TimeoutHandler? timeout_func;

	/// Since this dialog is used by add-ins, the IAddinLocalizer allows for translations to be
	/// fetched from the appropriate place.
	/// </param>
	public SimpleEffectDialog (string title, string icon_name, object effectData, IAddinLocalizer localizer)
	{
		Title = title;
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;
		this.AddCancelOkButtons ();
		this.SetDefaultResponse (ResponseType.Ok);

		IconName = icon_name;

		var contentAreaBox = this.GetContentAreaBox ();

		contentAreaBox.Spacing = 12;
		contentAreaBox.SetAllMargins (6);
		WidthRequest = 400;
		Resizable = false;

		// Build dialog
		foreach (var widget in GenerateDialogWidgets (effectData, localizer)) {
			contentAreaBox.Append (widget);
		}

		OnClose += (_, _) => HandleClose ();
	}

	/// <summary>
	/// Helper function for launching the dialog and connecting its signals.
	/// The IAddinLocalizer provides a generic way to get translated strings both for
	/// Pinta's effects and for effect add-ins.
	/// </summary>
	public static void Launch (BaseEffect effect, IAddinLocalizer localizer)
	{
		ArgumentNullException.ThrowIfNull (effect.EffectData);

		var dialog = new SimpleEffectDialog (
			effect.Name, effect.Icon, effect.EffectData, localizer);

		// Hookup event handling for live preview.
		dialog.EffectDataChanged += (o, e) => {
			if (effect.EffectData != null)
				effect.EffectData.FirePropertyChanged (e.PropertyName);
		};

		dialog.OnResponse += (_, args) => {
			effect.OnConfigDialogResponse (args.ResponseId == (int) Gtk.ResponseType.Ok);
			dialog.Destroy ();
		};

		dialog.Present ();
	}

	public event PropertyChangedEventHandler? EffectDataChanged;

	private void HandleClose ()
	{
		// If there is a timeout that hasn't been invoked yet, run it before closing the dialog.
		if (event_delay_timeout_id != 0) {
			GLib.Source.Remove (event_delay_timeout_id);
			timeout_func?.Invoke ();
		}
	}

	#region EffectData Parser
	private IEnumerable<Widget> GenerateDialogWidgets (object effectData, IAddinLocalizer localizer)
	{
		Random random = new ();
		var members = effectData.GetType ().GetMembers ().Where (m => m is FieldInfo || m is PropertyInfo);

		foreach (var mi in members) {

			var mType = GetTypeForMember (mi);

			string? caption = null;
			string? hint = null;
			var skip = false;
			var combo = false;

			var attrs = mi.GetCustomAttributes (false);

			foreach (var attr in attrs) {
				switch (attr) {
					case SkipAttribute:
						skip = true;
						break;
					case CaptionAttribute captionAttr:
						caption = captionAttr.Caption;
						break;
					case HintAttribute hintAttr:
						hint = hintAttr.Hint;
						break;
					case StaticListAttribute:
						combo = true;
						break;
				}
			}

			if (skip || string.Compare (mi.Name, "IsDefault", true) == 0)
				continue;

			caption ??= MakeCaption (mi.Name);

			if (mType == typeof (int) && (caption == "Seed"))
				yield return CreateSeed (localizer.GetString (caption), effectData, mi, attrs);
			else if (mType == typeof (int))
				yield return CreateSlider (localizer.GetString (caption), effectData, mi, attrs);
			else if (mType == typeof (double) && (caption == "Angle" || caption == "Rotation"))
				yield return CreateAnglePicker (localizer.GetString (caption), effectData, mi, attrs);
			else if (mType == typeof (double))
				yield return CreateDoubleSlider (localizer.GetString (caption), effectData, mi, attrs);
			else if (combo && mType == typeof (string))
				yield return CreateComboBox (localizer.GetString (caption), effectData, mi, attrs);
			else if (mType == typeof (bool))
				yield return CreateCheckBox (localizer.GetString (caption), effectData, mi, attrs);
			else if (mType == typeof (PointI))
				yield return CreatePointPicker (localizer.GetString (caption), effectData, mi, attrs);
			else if (mType == typeof (PointD))
				yield return CreateOffsetPicker (localizer.GetString (caption), effectData, mi, attrs);
			else if (mType.IsEnum)
				yield return CreateEnumComboBox (localizer.GetString (caption), effectData, mi, attrs);

			if (hint != null)
				yield return CreateHintLabel (localizer.GetString (hint));
		}
	}

	#endregion

	#region Control Builders
	private ComboBoxWidget CreateEnumComboBox (string caption, object o, MemberInfo member, object[] attributes)
	{
		var myType = GetTypeForMember (member)!; // NRT - We're looping through members we got from reflection

		var member_names = Enum.GetNames (myType);
		var labels = new List<string> ();
		var label_to_member = new Dictionary<string, string> ();

		foreach (var member_name in member_names) {
			var members = myType.GetMember (member_name);

			// Look for a Caption attribute that provides a (translated) description.
			string label;
			var attrs = members[0].GetCustomAttributes (typeof (CaptionAttribute), false);

			if (attrs.Length > 0)
				label = Core.Translations.GetString (((CaptionAttribute) attrs[0]).Caption);
			else
				label = Core.Translations.GetString (member_name);

			label_to_member[label] = member_name;
			labels.Add (label);
		}

		var widget = new ComboBoxWidget (labels) { Label = caption };

		if (GetValue (member, o) is object obj)
			widget.Active = ((IList) member_names).IndexOf (obj.ToString ());

		widget.Changed += (_, _) => SetValue (member, o, Enum.Parse (myType, label_to_member[widget.ActiveText]));

		return widget;
	}

	private ComboBoxWidget CreateComboBox (string caption, object o, MemberInfo member, object[] attributes)
	{
		Dictionary<string, object>? dict = null;

		foreach (var attr in attributes) {
			if (attr is StaticListAttribute attribute && GetValue (attribute.DictionaryName, o) is Dictionary<string, object> d)
				dict = d;
		}

		var entries = new List<string> ();

		if (dict != null)
			entries.AddRange (dict.Keys);

		var widget = new ComboBoxWidget (entries) {
			Label = caption
		};

		if (GetValue (member, o) is string s)
			widget.Active = entries.IndexOf (s);

		widget.Changed += (_, _) => SetValue (member, o, widget.ActiveText);

		return widget;
	}

	private HScaleSpinButtonWidget CreateDoubleSlider (string caption, object o, MemberInfo member, object[] attributes)
	{
		var min_value = -100;
		var max_value = 100;
		var inc_value = 0.01;
		var digits_value = 2;

		foreach (var attr in attributes) {
			switch (attr) {
				case MinimumValueAttribute min:
					min_value = min.Value;
					break;
				case MaximumValueAttribute max:
					max_value = max.Value;
					break;
				case IncrementValueAttribute inc:
					inc_value = inc.Value;
					break;
				case DigitsValueAttribute digits:
					digits_value = digits.Value;
					break;
			}
		}

		var widget = new HScaleSpinButtonWidget {
			Label = caption,
			MinimumValue = min_value,
			MaximumValue = max_value,
			IncrementValue = inc_value,
			DigitsValue = digits_value
		};

		if (GetValue (member, o) is double d)
			widget.DefaultValue = d;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetValue (member, o, widget.Value);
				return false;
			});
		};

		return widget;
	}

	private HScaleSpinButtonWidget CreateSlider (string caption, object o, MemberInfo member, object[] attributes)
	{
		var min_value = -100;
		var max_value = 100;
		var inc_value = 1.0;
		var digits_value = 0;

		foreach (var attr in attributes) {
			switch (attr) {
				case MinimumValueAttribute min:
					min_value = min.Value;
					break;
				case MaximumValueAttribute max:
					max_value = max.Value;
					break;
				case IncrementValueAttribute inc:
					inc_value = inc.Value;
					break;
				case DigitsValueAttribute digits:
					digits_value = digits.Value;
					break;
			}
		}

		var widget = new HScaleSpinButtonWidget {
			Label = caption,
			MinimumValue = min_value,
			MaximumValue = max_value,
			IncrementValue = inc_value,
			DigitsValue = digits_value
		};

		if (GetValue (member, o) is int i)
			widget.DefaultValue = i;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetValue (member, o, widget.ValueAsInt);
				return false;
			});
		};

		return widget;
	}

	private Gtk.CheckButton CreateCheckBox (string caption, object o, MemberInfo member, object[] attributes)
	{
		var widget = new Gtk.CheckButton { Label = caption };

		if (GetValue (member, o) is bool b)
			widget.Active = b;

		widget.OnToggled += (_, _) => SetValue (member, o, widget.Active);

		return widget;
	}

	private PointPickerWidget CreateOffsetPicker (string caption, object o, MemberInfo member, object[] attributes)
	{
		var widget = new PointPickerWidget { Label = caption };

		if (GetValue (member, o) is PointD p)
			widget.DefaultOffset = p;

		widget.PointPicked += (_, _) => SetValue (member, o, widget.Offset);

		return widget;
	}

	private PointPickerWidget CreatePointPicker (string caption, object o, MemberInfo member, object[] attributes)
	{
		var widget = new PointPickerWidget { Label = caption };

		if (GetValue (member, o) is PointI p)
			widget.DefaultPoint = p;

		widget.PointPicked += (_, _) => SetValue (member, o, widget.Point);

		return widget;
	}

	private AnglePickerWidget CreateAnglePicker (string caption, object o, MemberInfo member, object[] attributes)
	{
		var widget = new AnglePickerWidget {
			Label = caption
		};

		if (GetValue (member, o) is double d)
			widget.DefaultValue = d;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetValue (member, o, widget.Value);
				return false;
			});
		};

		return widget;
	}

	private static Gtk.Label CreateHintLabel (string hint)
	{
		var label = Gtk.Label.New (hint);
		label.Wrap = true;
		label.Halign = Align.Start;

		return label;
	}

	private static readonly Random random = new ();
	private ReseedButtonWidget CreateSeed (string caption, object o, MemberInfo member, object[] attributes)
	{
		var widget = new ReseedButtonWidget ();
		widget.Clicked += (_, _) => SetValue (member, o, random.Next ());
		return widget;
	}

	#endregion

	#region Static Reflection Methods
	private static object? GetValue (MemberInfo mi, object o)
	{
		if (mi is FieldInfo fi)
			return fi.GetValue (o);

		var pi = mi as PropertyInfo;

		return pi?.GetGetMethod ()?.Invoke (o, Array.Empty<object> ());
	}

	private void SetValue (MemberInfo mi, object o, object val)
	{
		string fieldName;

		switch (mi) {
			case FieldInfo fi:
				fi.SetValue (o, val);
				fieldName = fi.Name;
				break;
			case PropertyInfo pi:
				pi.GetSetMethod ()?.Invoke (o, new object[] { val });
				fieldName = pi.Name;
				break;
			default:
				throw new ArgumentException ("Invalid member type", nameof (mi));
		}

		EffectDataChanged?.Invoke (this, new PropertyChangedEventArgs (fieldName));
	}

	private static Type GetTypeForMember (MemberInfo mi)
	{
		return mi switch {
			FieldInfo fi => fi.FieldType,
			PropertyInfo pi => pi.PropertyType,
			_ => throw new ArgumentException ("Invalid member type", nameof (mi)),
		};
	}

	private static string MakeCaption (string name)
	{
		var sb = new StringBuilder (name.Length);
		var nextUp = true;

		foreach (var c in name) {
			if (nextUp) {
				sb.Append (char.ToUpper (c));
				nextUp = false;
			} else {
				if (c == '_') {
					sb.Append (' ');
					nextUp = true;
					continue;
				}
				if (char.IsUpper (c))
					sb.Append (' ');
				sb.Append (c);
			}
		}

		return sb.ToString ();
	}

	private static object? GetValue (string name, object o)
	{
		if (o.GetType ().GetField (name) is FieldInfo fi)
			return fi.GetValue (o);

		if (o.GetType ().GetProperty (name) is PropertyInfo pi)
			return pi.GetGetMethod ()?.Invoke (o, Array.Empty<object> ());

		return null;
	}

	private void DelayedUpdate (TimeoutHandler handler)
	{
		if (event_delay_timeout_id != 0) {
			GLib.Source.Remove (event_delay_timeout_id);
			if (handler != timeout_func)
				timeout_func?.Invoke ();
		}

		timeout_func = handler;
		event_delay_timeout_id = GLib.Functions.TimeoutAdd (0, Event_delay_millis, () => {
			event_delay_timeout_id = 0;
			timeout_func.Invoke ();
			timeout_func = null;
			return false;
		});
	}
	#endregion
}

/// <summary>
/// Wrapper around Pinta's translation template.
/// </summary>
public sealed class PintaLocalizer : IAddinLocalizer
{
	public string GetString (string msgid)
	{
		return Pinta.Core.Translations.GetString (msgid);
	}
};
