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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Mono.Addins.Localization;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class SimpleEffectDialog : Gtk.Dialog
{
	const uint Event_delay_millis = 100;
	uint event_delay_timeout_id;

	private delegate bool TimeoutHandler ();
	TimeoutHandler? timeout_func;

	private readonly Random random = new ();

	/// Since this dialog is used by add-ins, the IAddinLocalizer allows for translations to be
	/// fetched from the appropriate place.
	/// </param>
	public SimpleEffectDialog (string title, string icon_name, EffectData effectData, IAddinLocalizer localizer)
	{
		Title = title;
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;
		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		IconName = icon_name;

		var contentAreaBox = this.GetContentAreaBox ();

		contentAreaBox.Spacing = 12;
		contentAreaBox.SetAllMargins (6);
		WidthRequest = 400;
		Resizable = false;

		// Build dialog
		foreach (var widget in GenerateDialogWidgets (effectData, localizer))
			contentAreaBox.Append (widget);

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
		dialog.EffectDataChanged += (o, e) => effect.EffectData?.FirePropertyChanged (e.PropertyName);

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

	private readonly record struct MemberSettings (
		MemberInfo mi,
		Type mType,
		string caption,
		string? hint,
		bool skip,
		bool combo,
		object[] attrs);

	private static MemberSettings CreateMemberSettings (MemberInfo mi)
	{
		Type mType = GetTypeForMember (mi);

		string? caption = null;
		string? hint = null;
		bool skip = false;
		bool combo = false;

		object[] attrs = mi.GetCustomAttributes (false);

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

		caption ??= MakeCaption (mi.Name);

		return new (
			mi: mi,
			mType: mType,
			caption: caption,
			hint: hint,
			skip: skip,
			combo: combo,
			attrs: attrs
		);
	}

	private IEnumerable<Gtk.Widget> GenerateDialogWidgets (EffectData effectData, IAddinLocalizer localizer)
		=>
			effectData
			.GetType ()
			.GetMembers ()
			.Where (m => m is FieldInfo || m is PropertyInfo)
			.Select (CreateMemberSettings)
			.Where (settings => !settings.skip && string.Compare (settings.mi.Name, nameof (EffectData.IsDefault), true) != 0)
			.Select (settings => GetMemberWidgets (settings, effectData, localizer))
			.SelectMany (widgets => widgets);

	private IEnumerable<Gtk.Widget> GetMemberWidgets (MemberSettings settings, EffectData effectData, IAddinLocalizer localizer)
	{
		if (settings.mType == typeof (RandomSeed))
			yield return CreateSeed (localizer.GetString (settings.caption), effectData, settings.mi, settings.attrs);
		else if (settings.mType == typeof (int))
			yield return CreateSlider (localizer.GetString (settings.caption), effectData, settings.mi, settings.attrs);
		else if (settings.mType == typeof (DegreesAngle))
			yield return CreateAnglePicker (localizer.GetString (settings.caption), effectData, settings.mi, settings.attrs);
		else if (settings.mType == typeof (double))
			yield return CreateDoubleSlider (localizer.GetString (settings.caption), effectData, settings.mi, settings.attrs);
		else if (settings.combo && settings.mType == typeof (string))
			yield return CreateComboBox (localizer.GetString (settings.caption), effectData, settings.mi, settings.attrs);
		else if (settings.mType == typeof (bool))
			yield return CreateCheckBox (localizer.GetString (settings.caption), effectData, settings.mi, settings.attrs);
		else if (settings.mType == typeof (PointI))
			yield return CreatePointPicker (localizer.GetString (settings.caption), effectData, settings.mi, settings.attrs);
		else if (settings.mType == typeof (PointD))
			yield return CreateOffsetPicker (localizer.GetString (settings.caption), effectData, settings.mi, settings.attrs);
		else if (settings.mType.IsEnum)
			yield return CreateEnumComboBox (localizer.GetString (settings.caption), effectData, settings.mi, settings.attrs);

		if (settings.hint != null)
			yield return CreateHintLabel (localizer.GetString (settings.hint));
	}

	#endregion

	#region Control Builders
	private ComboBoxWidget CreateEnumComboBox (string caption, EffectData effectData, MemberInfo member, object[] attributes)
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

		if (GetValue (member, effectData) is object obj)
			widget.Active = Array.IndexOf (member_names, obj.ToString ());

		widget.Changed += (_, _) => SetValue (member, effectData, Enum.Parse (myType, label_to_member[widget.ActiveText]));

		return widget;
	}

	private ComboBoxWidget CreateComboBox (string caption, EffectData effectData, MemberInfo member, object[] attributes)
	{
		Dictionary<string, object>? dict = null;

		foreach (var attr in attributes) {
			if (attr is StaticListAttribute attribute && GetValue (attribute.DictionaryName, effectData) is Dictionary<string, object> d)
				dict = d;
		}

		var entries = dict == null ? ImmutableArray<string>.Empty : dict.Keys.ToImmutableArray ();

		var widget = new ComboBoxWidget (entries) { Label = caption };

		if (GetValue (member, effectData) is string s)
			widget.Active = entries.IndexOf (s);

		widget.Changed += (_, _) => SetValue (member, effectData, widget.ActiveText);

		return widget;
	}

	private HScaleSpinButtonWidget CreateDoubleSlider (string caption, EffectData effectData, MemberInfo member, object[] attributes)
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
			DigitsValue = digits_value,
		};

		if (GetValue (member, effectData) is double d)
			widget.DefaultValue = d;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetValue (member, effectData, widget.Value);
				return false;
			});
		};

		return widget;
	}

	private HScaleSpinButtonWidget CreateSlider (string caption, EffectData effectData, MemberInfo member, object[] attributes)
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

		if (GetValue (member, effectData) is int i)
			widget.DefaultValue = i;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetValue (member, effectData, widget.ValueAsInt);
				return false;
			});
		};

		return widget;
	}

	private Gtk.CheckButton CreateCheckBox (string caption, EffectData effectData, MemberInfo member, object[] attributes)
	{
		var widget = new Gtk.CheckButton { Label = caption };

		if (GetValue (member, effectData) is bool b)
			widget.Active = b;

		widget.OnToggled += (_, _) => SetValue (member, effectData, widget.Active);

		return widget;
	}

	private PointPickerWidget CreateOffsetPicker (string caption, EffectData effectData, MemberInfo member, object[] attributes)
	{
		var widget = new PointPickerWidget { Label = caption };

		if (GetValue (member, effectData) is PointD p)
			widget.DefaultOffset = p;

		widget.PointPicked += (_, _) => SetValue (member, effectData, widget.Offset);

		return widget;
	}

	private PointPickerWidget CreatePointPicker (string caption, EffectData effectData, MemberInfo member, object[] attributes)
	{
		var widget = new PointPickerWidget { Label = caption };

		if (GetValue (member, effectData) is PointI p)
			widget.DefaultPoint = p;

		widget.PointPicked += (_, _) => SetValue (member, effectData, widget.Point);

		return widget;
	}

	private AnglePickerWidget CreateAnglePicker (string caption, EffectData effectData, MemberInfo member, object[] attributes)
	{
		var widget = new AnglePickerWidget { Label = caption };

		if (GetValue (member, effectData) is DegreesAngle d)
			widget.DefaultValue = d;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetValue (member, effectData, widget.Value);
				return false;
			});
		};

		return widget;
	}

	private static Gtk.Label CreateHintLabel (string hint)
	{
		var label = Gtk.Label.New (hint);
		label.Wrap = true;
		label.Halign = Gtk.Align.Start;

		return label;
	}

	private ReseedButtonWidget CreateSeed (string caption, EffectData effectData, MemberInfo member, object[] attributes)
	{
		var widget = new ReseedButtonWidget () { Label = caption };

		int min_value = 0;
		int max_value = int.MaxValue - 1;
		foreach (var attr in attributes) {
			switch (attr) {
				case MinimumValueAttribute min:
					min_value = min.Value;
					break;
				case MaximumValueAttribute max:
					max_value = max.Value;
					break;
			}
		}

		widget.Clicked += (_, _) => SetValue (member, effectData, new RandomSeed (random.Next (min_value, max_value)));
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
		return string.Join (string.Empty, GenerateCharacters (name));

		static IEnumerable<char> GenerateCharacters (string name)
		{
			bool nextIsUpperCase = true;

			foreach (char c in name) {

				if (nextIsUpperCase) {
					yield return char.ToUpper (c);
					nextIsUpperCase = false;
					continue;
				}

				if (c == '_') {
					yield return ' ';
					nextIsUpperCase = true;
					continue;
				}

				if (char.IsUpper (c))
					yield return ' ';

				yield return c;
			}
		}
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
		event_delay_timeout_id = GLib.Functions.TimeoutAdd (
			0,
			Event_delay_millis, () => {
				event_delay_timeout_id = 0;
				timeout_func.Invoke ();
				timeout_func = null;
				return false;
			}
		);
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
