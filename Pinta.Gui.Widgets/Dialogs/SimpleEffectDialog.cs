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
using System.Threading.Tasks;
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
	public SimpleEffectDialog (
		string title,
		string iconName,
		EffectData effectData,
		IAddinLocalizer localizer)
	{
		// --- Initialization (Gtk.Window)

		Title = title;
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;
		IconName = iconName;
		WidthRequest = 400;
		Resizable = false;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- Initialization

		Gtk.Box contentAreaBox = this.GetContentAreaBox ();
		contentAreaBox.Spacing = 12;
		contentAreaBox.SetAllMargins (6);
		foreach (var widget in GenerateDialogWidgets (effectData, localizer))
			contentAreaBox.Append (widget);

		OnClose += (_, _) => HandleClose ();
	}

	/// <summary>
	/// Helper function for launching the dialog and connecting its signals.
	/// The IAddinLocalizer provides a generic way to get translated strings both for
	/// Pinta's effects and for effect add-ins.
	/// </summary>
	public static async Task<bool> Launch (BaseEffect effect, IAddinLocalizer localizer)
	{
		if (effect.EffectData == null)
			throw new ArgumentException ($"{effect.EffectData} should not be null", nameof (effect));

		SimpleEffectDialog dialog = new (
			effect.Name,
			effect.Icon,
			effect.EffectData,
			localizer);

		// Hookup event handling for live preview.
		dialog.EffectDataChanged += (o, e) => effect.EffectData.FirePropertyChanged (e.PropertyName);

		Gtk.ResponseType response = await dialog.RunAsync ();

		dialog.Destroy ();

		return Gtk.ResponseType.Ok == response;
	}

	public event PropertyChangedEventHandler? EffectDataChanged;

	private void HandleClose ()
	{
		// If there is a timeout that hasn't been invoked yet, run it before closing the dialog.
		if (event_delay_timeout_id == 0) return;
		GLib.Source.Remove (event_delay_timeout_id);
		timeout_func?.Invoke ();
	}

	private IEnumerable<Gtk.Widget> GenerateDialogWidgets (EffectData effectData, IAddinLocalizer localizer) =>
			effectData
			.GetType ()
			.GetMembers ()
			.Where (IsInstanceFieldOrProperty)
			.Where (IsCustomProperty)
			.Select (CreateSettings)
			.Where (settings => !settings.skip)
			.Select (settings => GenerateWidgetsForMember (settings, effectData, localizer))
			.SelectMany (widgets => widgets);

	private bool IsCustomProperty (MemberInfo memberInfo)
	{
		return string.Compare (memberInfo.Name, nameof (EffectData.IsDefault), true) != 0;
	}

	private bool IsInstanceFieldOrProperty (MemberInfo memberInfo)
	{
		switch (memberInfo) {
			case FieldInfo fieldInfo:
				return !fieldInfo.IsStatic;
			case PropertyInfo propertyInfo:
				MethodInfo? getter = propertyInfo.GetGetMethod ();
				if (getter is null) return false;
				return !getter.IsStatic;
			default:
				return false;
		}
	}

	private sealed record MemberSettings (
		MemberReflector reflector,
		string caption,
		string? hint,
		bool skip);

	private static MemberSettings CreateSettings (MemberInfo memberInfo)
	{
		MemberReflector reflector = new (memberInfo);

		string? caption =
			reflector.Attributes
			.OfType<CaptionAttribute> ()
			.Select (h => h.Caption)
			.FirstOrDefault ();

		return new (
			reflector: reflector,
			caption: caption ?? MakeCaption (memberInfo.Name),
			hint: reflector.Attributes.OfType<HintAttribute> ().Select (h => h.Hint).FirstOrDefault (),
			skip: reflector.Attributes.OfType<SkipAttribute> ().Any ());
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

	private IEnumerable<Gtk.Widget> GenerateWidgetsForMember (
		MemberSettings settings,
		EffectData effectData,
		IAddinLocalizer localizer)
	{
		WidgetFactory? widgetFactory = GetWidgetFactory (settings);

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
		else if (memberType == typeof (string) && settings.reflector.Attributes.OfType<StaticListAttribute> ().Any ())
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

	private ComboBoxWidget CreateEnumComboBox (
		string caption,
		EffectData effectData,
		MemberSettings settings)
	{
		var memberNames = Enum.GetNames (settings.reflector.MemberType);

		var mapping_items =
			from memberName in memberNames
			let members = settings.reflector.MemberType.GetMember (memberName)
			let attrs = members[0].GetCustomAttributes<CaptionAttribute> (false).Take (1).ToArray ()
			let label = attrs.Length > 0 ? attrs[0].Caption : memberName
			let translatedLabel = Translations.GetString (label)
			select KeyValuePair.Create (translatedLabel, memberName);

		Dictionary<string, string> label_to_member = new (mapping_items);
		List<string> labels = new ();
		foreach (var kvp in mapping_items) {
			label_to_member[kvp.Key] = kvp.Value;
			labels.Add (kvp.Key);
		}

		ComboBoxWidget widget = new (labels) { Label = caption };

		if (settings.reflector.GetValue (effectData) is object obj)
			widget.Active = Array.IndexOf (memberNames, obj.ToString ());

		widget.Changed += (_, _) => SetAndNotify (settings.reflector, effectData, Enum.Parse (settings.reflector.MemberType, label_to_member[widget.ActiveText]));

		return widget;
	}

	private ComboBoxWidget CreateComboBox (
		string caption,
		EffectData effectData,
		MemberSettings settings)
	{
		IDictionary<string, object>? dict = null;

		foreach (var attr in settings.reflector.Attributes)
			if (attr is StaticListAttribute attribute && ReflectionHelper.GetValue (effectData, attribute.DictionaryName) is IDictionary<string, object> d)
				dict = d;

		var entries =
			dict == null
			? ImmutableArray<string>.Empty
			: dict.Keys.ToImmutableArray ();

		ComboBoxWidget widget = new (entries) { Label = caption };

		if (settings.reflector.GetValue (effectData) is string s)
			widget.Active = entries.IndexOf (s);

		widget.Changed += (_, _) => SetAndNotify (settings.reflector, effectData, widget.ActiveText);

		return widget;
	}

	private HScaleSpinButtonWidget CreateDoubleSlider (
		string caption,
		EffectData effectData,
		MemberSettings settings)
	{
		double initialValue =
			(settings.reflector.GetValue (effectData) is double i)
			? i
			: default;

		var attributes = settings.reflector.Attributes;

		HScaleSpinButtonWidget widget = new (initialValue) {
			Label = caption,
			MinimumValue = attributes.OfType<MinimumValueAttribute> ().Select (m => m.Value).FirstOrDefault (-100),
			MaximumValue = attributes.OfType<MaximumValueAttribute> ().Select (m => m.Value).FirstOrDefault (100),
			IncrementValue = attributes.OfType<IncrementValueAttribute> ().Select (i => i.Value).FirstOrDefault (0.01),
			DigitsValue = attributes.OfType<DigitsValueAttribute> ().Select (d => d.Value).FirstOrDefault (2),
		};

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetAndNotify (settings.reflector, effectData, widget.Value);
				return false;
			});
		};

		return widget;
	}

	private HScaleSpinButtonWidget CreateSlider (
		string caption,
		EffectData effectData,
		MemberSettings settings)
	{
		int initialValue =
			(settings.reflector.GetValue (effectData) is int i)
			? i
			: default;

		var attributes = settings.reflector.Attributes;

		HScaleSpinButtonWidget widget = new (initialValue) {
			Label = caption,
			MinimumValue = attributes.OfType<MinimumValueAttribute> ().Select (m => m.Value).FirstOrDefault (-100),
			MaximumValue = attributes.OfType<MaximumValueAttribute> ().Select (m => m.Value).FirstOrDefault (100),
			IncrementValue = attributes.OfType<IncrementValueAttribute> ().Select (i => i.Value).FirstOrDefault (1.0),
			DigitsValue = attributes.OfType<DigitsValueAttribute> ().Select (d => d.Value).FirstOrDefault (0),
		};

		widget.ValueChanged += (_, _) => {
			DelayedUpdate (() => {
				SetAndNotify (settings.reflector, effectData, widget.ValueAsInt);
				return false;
			});
		};

		return widget;
	}

	private Gtk.CheckButton CreateCheckBox (
		string caption,
		EffectData effectData,
		MemberSettings settings)
	{
		Gtk.CheckButton widget = new () { Label = caption };

		if (settings.reflector.GetValue (effectData) is bool b)
			widget.Active = b;

		widget.OnToggled += (_, _) => SetAndNotify (settings.reflector, effectData, widget.Active);

		return widget;
	}

	private PointPickerWidget CreateOffsetPicker (
		string caption,
		EffectData effectData,
		MemberSettings settings)
	{
		PointI initialPoint =
			(settings.reflector.GetValue (effectData) is PointI p)
			? p
			: default;

		PointPickerWidget widget = new (PintaCore.Workspace.ImageSize, initialPoint) { Label = caption };

		widget.PointPicked += (_, _) => SetAndNotify (settings.reflector, effectData, widget.Offset);

		return widget;
	}

	private PointPickerWidget CreatePointPicker (
		string caption,
		EffectData effectData,
		MemberSettings settings)
	{
		PointI initialPoint =
			(settings.reflector.GetValue (effectData) is PointI p)
			? p
			: default;

		PointPickerWidget widget = new (PintaCore.Workspace.ImageSize, initialPoint) { Label = caption };

		widget.PointPicked += (_, _) => SetAndNotify (settings.reflector, effectData, widget.Point);

		return widget;
	}

	private AnglePickerWidget CreateAnglePicker (
		string caption,
		EffectData effectData,
		MemberSettings settings)
	{
		DegreesAngle initialAngle =
			(settings.reflector.GetValue (effectData) is DegreesAngle d)
			? d
			: default;

		AnglePickerWidget widget = new (initialAngle) { Label = caption };

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
		Gtk.Label label = Gtk.Label.New (hint);
		label.Wrap = true;
		label.Halign = Gtk.Align.Start;
		label.MaxWidthChars = 40;
		return label;
	}

	private void SetAndNotify (MemberReflector reflector, object o, object val)
	{
		reflector.SetValue (o, val);
		EffectDataChanged?.Invoke (this, new PropertyChangedEventArgs (reflector.OriginalMemberInfo.Name));
	}

	private ReseedButtonWidget CreateSeed (
		string caption,
		EffectData effectData,
		MemberSettings settings)
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
