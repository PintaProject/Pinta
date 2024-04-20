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
			effect.Name,
			effect.Icon,
			effect.EffectData,
			localizer);

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
		if (event_delay_timeout_id == 0) return;
		GLib.Source.Remove (event_delay_timeout_id);
		timeout_func?.Invoke ();
	}

	#region EffectData Parser

	private sealed record MemberSettings (
		MemberInfo mi,
		Type mType,
		string caption,
		string? hint,
		bool skip,
		bool combo,
		ImmutableArray<Attribute> attrs);

	private static MemberSettings CreateMemberSettings (MemberInfo mi)
	{
		ImmutableArray<Attribute> attrs =
			mi
			.GetCustomAttributes<Attribute> (false)
			.ToImmutableArray ();

		string? caption = attrs.OfType<CaptionAttribute> ().Select (h => h.Caption).FirstOrDefault ();

		return new (
			mi: mi,
			mType: GetTypeForMember (mi),
			caption: caption ?? MakeCaption (mi.Name),
			hint: attrs.OfType<HintAttribute> ().Select (h => h.Hint).FirstOrDefault (),
			skip: attrs.OfType<SkipAttribute> ().Any (),
			combo: attrs.OfType<StaticListAttribute> ().Any (),
			attrs: attrs);
	}

	private IEnumerable<Gtk.Widget> GenerateDialogWidgets (EffectData effectData, IAddinLocalizer localizer) =>
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
		var widgetFactory = GetWidgetFactory (settings);

		if (widgetFactory is not null)
			yield return widgetFactory (localizer.GetString (settings.caption), effectData, settings);

		if (settings.hint != null)
			yield return CreateHintLabel (localizer.GetString (settings.hint));
	}

	private WidgetFactory? GetWidgetFactory (MemberSettings settings)
	{
		if (settings.mType == typeof (RandomSeed))
			return CreateSeed;
		else if (settings.mType == typeof (int))
			return CreateSlider;
		else if (settings.mType == typeof (DegreesAngle))
			return CreateAnglePicker;
		else if (settings.mType == typeof (double))
			return CreateDoubleSlider;
		else if (settings.combo && settings.mType == typeof (string))
			return CreateComboBox;
		else if (settings.mType == typeof (bool))
			return CreateCheckBox;
		else if (settings.mType == typeof (PointI))
			return CreatePointPicker;
		else if (settings.mType == typeof (PointD))
			return CreateOffsetPicker;
		else if (settings.mType.IsEnum)
			return CreateEnumComboBox;
		else
			return null;
	}

	private delegate Gtk.Widget WidgetFactory (string caption, EffectData effectData, MemberSettings settings);

	#endregion

	#region Control Builders
	private ComboBoxWidget CreateEnumComboBox (string caption, EffectData effectData, MemberSettings settings)
	{
		var myType = GetTypeForMember (settings.mi)!; // NRT - We're looping through members we got from reflection

		var member_names = Enum.GetNames (myType);

		var mapping_items =
			from member_name in member_names
			let members = myType.GetMember (member_name)
			let attrs = members[0].GetCustomAttributes<CaptionAttribute> (false).Take (1).ToArray ()
			let label = attrs.Length > 0 ? attrs[0].Caption : member_name
			let translatedLabel = Translations.GetString (label)
			select KeyValuePair.Create (translatedLabel, member_name);

		Dictionary<string, string> label_to_member = new (mapping_items);
		List<string> labels = new ();
		foreach (var kvp in mapping_items) {
			label_to_member[kvp.Key] = kvp.Value;
			labels.Add (kvp.Key);
		}

		ComboBoxWidget widget = new (labels) { Label = caption };

		if (GetValue (settings.mi, effectData) is object obj)
			widget.Active = Array.IndexOf (member_names, obj.ToString ());

		widget.Changed += (_, _) => SetValue (settings.mi, effectData, Enum.Parse (myType, label_to_member[widget.ActiveText]));

		return widget;
	}

	private ComboBoxWidget CreateComboBox (string caption, EffectData effectData, MemberSettings settings)
	{
		Dictionary<string, object>? dict = null;

		foreach (var attr in settings.attrs)
			if (attr is StaticListAttribute attribute && GetValue (attribute.DictionaryName, effectData) is Dictionary<string, object> d)
				dict = d;

		var entries = dict == null ? ImmutableArray<string>.Empty : dict.Keys.ToImmutableArray ();

		var widget = new ComboBoxWidget (entries) { Label = caption };

		if (GetValue (settings.mi, effectData) is string s)
			widget.Active = entries.IndexOf (s);

		widget.Changed += (_, _) => SetValue (settings.mi, effectData, widget.ActiveText);

		return widget;
	}

	private HScaleSpinButtonWidget CreateDoubleSlider (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new HScaleSpinButtonWidget {
			Label = caption,
			MinimumValue = settings.attrs.OfType<MinimumValueAttribute> ().Select (m => m.Value).FirstOrDefault (-100),
			MaximumValue = settings.attrs.OfType<MaximumValueAttribute> ().Select (m => m.Value).FirstOrDefault (100),
			IncrementValue = settings.attrs.OfType<IncrementValueAttribute> ().Select (i => i.Value).FirstOrDefault (0.01),
			DigitsValue = settings.attrs.OfType<DigitsValueAttribute> ().Select (d => d.Value).FirstOrDefault (2),
		};

		if (GetValue (settings.mi, effectData) is double d)
			widget.DefaultValue = d;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetValue (settings.mi, effectData, widget.Value);
				return false;
			});
		};

		return widget;
	}

	private HScaleSpinButtonWidget CreateSlider (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new HScaleSpinButtonWidget {
			Label = caption,
			MinimumValue = settings.attrs.OfType<MinimumValueAttribute> ().Select (m => m.Value).FirstOrDefault (-100),
			MaximumValue = settings.attrs.OfType<MaximumValueAttribute> ().Select (m => m.Value).FirstOrDefault (100),
			IncrementValue = settings.attrs.OfType<IncrementValueAttribute> ().Select (i => i.Value).FirstOrDefault (1.0),
			DigitsValue = settings.attrs.OfType<DigitsValueAttribute> ().Select (d => d.Value).FirstOrDefault (0),
		};

		if (GetValue (settings.mi, effectData) is int i)
			widget.DefaultValue = i;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetValue (settings.mi, effectData, widget.ValueAsInt);
				return false;
			});
		};

		return widget;
	}

	private Gtk.CheckButton CreateCheckBox (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new Gtk.CheckButton { Label = caption };

		if (GetValue (settings.mi, effectData) is bool b)
			widget.Active = b;

		widget.OnToggled += (_, _) => SetValue (settings.mi, effectData, widget.Active);

		return widget;
	}

	private PointPickerWidget CreateOffsetPicker (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new PointPickerWidget { Label = caption };

		if (GetValue (settings.mi, effectData) is PointD p)
			widget.DefaultOffset = p;

		widget.PointPicked += (_, _) => SetValue (settings.mi, effectData, widget.Offset);

		return widget;
	}

	private PointPickerWidget CreatePointPicker (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new PointPickerWidget { Label = caption };

		if (GetValue (settings.mi, effectData) is PointI p)
			widget.DefaultPoint = p;

		widget.PointPicked += (_, _) => SetValue (settings.mi, effectData, widget.Point);

		return widget;
	}

	private AnglePickerWidget CreateAnglePicker (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new AnglePickerWidget { Label = caption };

		if (GetValue (settings.mi, effectData) is DegreesAngle d)
			widget.DefaultValue = d;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetValue (settings.mi, effectData, widget.Value);
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
		label.MaxWidthChars = 40;
		return label;
	}

	private ReseedButtonWidget CreateSeed (string caption, EffectData effectData, MemberSettings settings)
	{
		int min_value = settings.attrs.OfType<MinimumValueAttribute> ().Select (m => m.Value).FirstOrDefault (0);
		int max_value = settings.attrs.OfType<MaximumValueAttribute> ().Select (m => m.Value).FirstOrDefault (int.MaxValue - 1);

		ReseedButtonWidget widget = new () { Label = caption };
		Random random = new ();
		widget.Clicked += (_, _) => SetValue (
			settings.mi,
			effectData,
			new RandomSeed (
				random.Next (
					min_value,
					max_value
				)
			)
		);
		return widget;
	}

	#endregion

	#region Static Reflection Methods

	private static object? GetValue (MemberInfo mi, object o)
		=> mi switch {
			FieldInfo fi => fi.GetValue (o),
			PropertyInfo pi => pi.GetGetMethod ()?.Invoke (o, Array.Empty<object> ()),
			_ => null,
		};

	private void SetValue (MemberInfo mi, object o, object val)
	{
		string fieldName;

		switch (mi) {
			case FieldInfo fi:
				fi.SetValue (o, val);
				fieldName = fi.Name;
				break;
			case PropertyInfo pi:
				pi.GetSetMethod ()?.Invoke (o, new[] { val });
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
		=> Translations.GetString (msgid);
};
