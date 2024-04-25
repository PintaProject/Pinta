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

	private IEnumerable<Gtk.Widget> GenerateDialogWidgets (EffectData effectData, IAddinLocalizer localizer) =>
			effectData
			.GetType ()
			.GetMembers ()
			.Where (m => m is FieldInfo || m is PropertyInfo)
			.Select (CreateSettings)
			.Where (settings => !settings.skip && string.Compare (settings.reflector.OriginalMemberInfo.Name, nameof (EffectData.IsDefault), true) != 0)
			.Select (settings => GetMemberWidgets (settings, effectData, localizer))
			.SelectMany (widgets => widgets);

	private sealed record MemberSettings (
		MemberReflector reflector,
		string caption,
		string? hint,
		bool skip,
		bool isComboBox);

	private static MemberSettings CreateSettings (MemberInfo memberInfo)
	{
		MemberReflector reflector = new (memberInfo);

		string? caption =
			reflector.Attributes
			.OfType<CaptionAttribute> ()
			.Select (h => h.Caption)
			.FirstOrDefault ();

		return new MemberSettings (
			reflector: reflector,
			caption: caption ?? MakeCaption (memberInfo.Name),
			hint: reflector.Attributes.OfType<HintAttribute> ().Select (h => h.Hint).FirstOrDefault (),
			skip: reflector.Attributes.OfType<SkipAttribute> ().Any (),
			isComboBox: reflector.Attributes.OfType<StaticListAttribute> ().Any ()
		);
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
		Type memberType = settings.reflector.MemberType;
		if (memberType == typeof (RandomSeed))
			return CreateSeed;
		else if (memberType == typeof (int))
			return CreateSlider;
		else if (memberType == typeof (DegreesAngle))
			return CreateAnglePicker;
		else if (memberType == typeof (double))
			return CreateDoubleSlider;
		else if (settings.isComboBox && memberType == typeof (string))
			return CreateComboBox;
		else if (memberType == typeof (bool))
			return CreateCheckBox;
		else if (memberType == typeof (PointI))
			return CreatePointPicker;
		else if (memberType == typeof (PointD))
			return CreateOffsetPicker;
		else if (memberType.IsEnum)
			return CreateEnumComboBox;
		else
			return null;
	}

	private delegate Gtk.Widget WidgetFactory (string caption, EffectData effectData, MemberSettings settings);

	#endregion

	#region Control Builders
	private ComboBoxWidget CreateEnumComboBox (string caption, EffectData effectData, MemberSettings settings)
	{
		var member_names = Enum.GetNames (settings.reflector.MemberType);

		var mapping_items =
			from member_name in member_names
			let members = settings.reflector.MemberType.GetMember (member_name)
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

		if (settings.reflector.GetValue (effectData) is object obj)
			widget.Active = Array.IndexOf (member_names, obj.ToString ());

		widget.Changed += (_, _) => SetAndNotify (settings.reflector, effectData, Enum.Parse (settings.reflector.MemberType, label_to_member[widget.ActiveText]));

		return widget;
	}

	private ComboBoxWidget CreateComboBox (string caption, EffectData effectData, MemberSettings settings)
	{
		Dictionary<string, object>? dict = null;

		foreach (var attr in settings.reflector.Attributes)
			if (attr is StaticListAttribute attribute && GetValue (attribute.DictionaryName, effectData) is Dictionary<string, object> d)
				dict = d;

		var entries = dict == null ? ImmutableArray<string>.Empty : dict.Keys.ToImmutableArray ();

		var widget = new ComboBoxWidget (entries) { Label = caption };

		if (settings.reflector.GetValue (effectData) is string s)
			widget.Active = entries.IndexOf (s);

		widget.Changed += (_, _) => SetAndNotify (settings.reflector, effectData, widget.ActiveText);

		return widget;
	}

	private HScaleSpinButtonWidget CreateDoubleSlider (string caption, EffectData effectData, MemberSettings settings)
	{
		var attributes = settings.reflector.Attributes;

		var widget = new HScaleSpinButtonWidget {
			Label = caption,
			MinimumValue = attributes.OfType<MinimumValueAttribute> ().Select (m => m.Value).FirstOrDefault (-100),
			MaximumValue = attributes.OfType<MaximumValueAttribute> ().Select (m => m.Value).FirstOrDefault (100),
			IncrementValue = attributes.OfType<IncrementValueAttribute> ().Select (i => i.Value).FirstOrDefault (0.01),
			DigitsValue = attributes.OfType<DigitsValueAttribute> ().Select (d => d.Value).FirstOrDefault (2),
		};

		if (settings.reflector.GetValue (effectData) is double d)
			widget.DefaultValue = d;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetAndNotify (settings.reflector, effectData, widget.Value);
				return false;
			});
		};

		return widget;
	}

	private HScaleSpinButtonWidget CreateSlider (string caption, EffectData effectData, MemberSettings settings)
	{
		var attributes = settings.reflector.Attributes;

		var widget = new HScaleSpinButtonWidget {
			Label = caption,
			MinimumValue = attributes.OfType<MinimumValueAttribute> ().Select (m => m.Value).FirstOrDefault (-100),
			MaximumValue = attributes.OfType<MaximumValueAttribute> ().Select (m => m.Value).FirstOrDefault (100),
			IncrementValue = attributes.OfType<IncrementValueAttribute> ().Select (i => i.Value).FirstOrDefault (1.0),
			DigitsValue = attributes.OfType<DigitsValueAttribute> ().Select (d => d.Value).FirstOrDefault (0),
		};

		if (settings.reflector.GetValue (effectData) is int i)
			widget.DefaultValue = i;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetAndNotify (settings.reflector, effectData, widget.ValueAsInt);
				return false;
			});
		};

		return widget;
	}

	private Gtk.CheckButton CreateCheckBox (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new Gtk.CheckButton { Label = caption };

		if (settings.reflector.GetValue (effectData) is bool b)
			widget.Active = b;

		widget.OnToggled += (_, _) => SetAndNotify (settings.reflector, effectData, widget.Active);

		return widget;
	}

	private PointPickerWidget CreateOffsetPicker (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new PointPickerWidget { Label = caption };

		if (settings.reflector.GetValue (effectData) is PointD p)
			widget.DefaultOffset = p;

		widget.PointPicked += (_, _) => SetAndNotify (settings.reflector, effectData, widget.Offset);

		return widget;
	}

	private PointPickerWidget CreatePointPicker (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new PointPickerWidget { Label = caption };

		if (settings.reflector.GetValue (effectData) is PointI p)
			widget.DefaultPoint = p;

		widget.PointPicked += (_, _) => SetAndNotify (settings.reflector, effectData, widget.Point);

		return widget;
	}

	private AnglePickerWidget CreateAnglePicker (string caption, EffectData effectData, MemberSettings settings)
	{
		var widget = new AnglePickerWidget { Label = caption };

		if (settings.reflector.GetValue (effectData) is DegreesAngle d)
			widget.DefaultValue = d;

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetAndNotify (settings.reflector, effectData, widget.Value);
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

	private void SetAndNotify (MemberReflector settings, object o, object val)
	{
		settings.SetValue (o, val);
		EffectDataChanged?.Invoke (this, new PropertyChangedEventArgs (settings.OriginalMemberInfo.Name));
	}

	private ReseedButtonWidget CreateSeed (string caption, EffectData effectData, MemberSettings settings)
	{
		var attributes = settings.reflector.Attributes;

		int min_value = attributes.OfType<MinimumValueAttribute> ().Select (m => m.Value).FirstOrDefault (0);
		int max_value = attributes.OfType<MaximumValueAttribute> ().Select (m => m.Value).FirstOrDefault (int.MaxValue - 1);

		ReseedButtonWidget widget = new () { Label = caption };
		Random random = new ();
		widget.Clicked += (_, _) => {
			int seedValue = random.Next (min_value, max_value);
			RandomSeed seed = new (seedValue);
			SetAndNotify (settings.reflector, effectData, seed);
		};
		return widget;
	}

	#endregion

	#region Static Reflection Methods

	private static object? GetValue (string name, object o)
	{
		if (o.GetType ().GetField (name) is FieldInfo fi)
			return fi.GetValue (o);

		if (o.GetType ().GetProperty (name) is PropertyInfo pi)
			return pi.GetGetMethod ()?.Invoke (o, Array.Empty<object> ());

		return null;
	}

	#endregion

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

}

/// <summary>
/// Wrapper around Pinta's translation template.
/// </summary>
public sealed class PintaLocalizer : IAddinLocalizer
{
	public string GetString (string msgid)
		=> Translations.GetString (msgid);
};
