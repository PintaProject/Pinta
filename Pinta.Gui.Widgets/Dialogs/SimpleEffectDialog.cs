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
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using Mono.Addins.Localization;
using Mono.Unix;

namespace Pinta.Gui.Widgets
{
	public class SimpleEffectDialog : Gtk.Dialog
	{
		[ThreadStatic]
		Random random = new Random ();

		const uint event_delay_millis = 100;
		uint event_delay_timeout_id;

		/// Since this dialog is used by add-ins, the IAddinLocalizer allows for translations to be
		/// fetched from the appropriate place.
		/// </param>
		public SimpleEffectDialog (string title, Gdk.Pixbuf icon, object effectData,
		                           IAddinLocalizer localizer)
			: base (title, Pinta.Core.PintaCore.Chrome.MainWindow, Gtk.DialogFlags.Modal,
				Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, Gtk.Stock.Ok, Gtk.ResponseType.Ok)
		{
			Icon = icon;
			EffectData = effectData;

			BorderWidth = 6;
			VBox.Spacing = 12;
			WidthRequest = 400;
			Resizable = false;
			DefaultResponse = Gtk.ResponseType.Ok;
			AlternativeButtonOrder = new int[] { (int)Gtk.ResponseType.Ok, (int)Gtk.ResponseType.Cancel };

			BuildDialog (localizer);
		}

		public object EffectData { get; private set; }

		public event PropertyChangedEventHandler EffectDataChanged;

		#region EffectData Parser
		private void BuildDialog (IAddinLocalizer localizer)
		{
			var members = EffectData.GetType ().GetMembers ();

			foreach (var mi in members) {
				Type mType = GetTypeForMember (mi);

				if (mType == null)
					continue;

				string caption = null;
				string hint = null;
				bool skip = false;
				bool combo = false;

				object[] attrs = mi.GetCustomAttributes (false);

				foreach (var attr in attrs) {
					if (attr is SkipAttribute)
						skip = true;
					else if (attr is CaptionAttribute)
						caption = ((CaptionAttribute)attr).Caption;
					else if (attr is HintAttribute)
						hint = ((HintAttribute)attr).Hint;
					else if (attr is StaticListAttribute)
						combo = true;

				}

				if (skip || string.Compare (mi.Name, "IsDefault", true) == 0)
					continue;

				if (caption == null)
					caption = MakeCaption (mi.Name);

				if (mType == typeof (int) && (caption == "Seed"))
					AddWidget (CreateSeed (localizer.GetString (caption), EffectData, mi, attrs));
				else if (mType == typeof (int))
					AddWidget (CreateSlider (localizer.GetString (caption), EffectData, mi, attrs));
				else if (mType == typeof (double) && (caption == "Angle" || caption == "Rotation"))
					AddWidget (CreateAnglePicker (localizer.GetString (caption), EffectData, mi, attrs));
				else if (mType == typeof (double))
					AddWidget (CreateDoubleSlider (localizer.GetString (caption), EffectData, mi, attrs));
				else if (combo && mType == typeof (string))
					AddWidget (CreateComboBox (localizer.GetString (caption), EffectData, mi, attrs));
				else if (mType == typeof (bool))
					AddWidget (CreateCheckBox (localizer.GetString (caption), EffectData, mi, attrs));
				else if (mType == typeof (Gdk.Point))
					AddWidget (CreatePointPicker (localizer.GetString (caption), EffectData, mi, attrs));
				else if (mType == typeof (Cairo.PointD))
					AddWidget (CreateOffsetPicker (localizer.GetString (caption), EffectData, mi, attrs));
				else if (mType.IsEnum)
					AddWidget (CreateEnumComboBox (localizer.GetString (caption), EffectData, mi, attrs));

				if (hint != null)
					AddWidget (CreateHintLabel (localizer.GetString (hint)));
			}
		}

		private void AddWidget (Gtk.Widget widget)
		{
			widget.Show ();
			this.VBox.Add (widget);
		}
		#endregion

		#region Control Builders
		private ComboBoxWidget CreateEnumComboBox (string caption, object o, System.Reflection.MemberInfo member, System.Object[] attributes)
		{
			Type myType = GetTypeForMember (member);

			string[] member_names = Enum.GetNames (myType);
			var labels = new List<string> ();
			var label_to_member = new Dictionary<string, string> ();

			foreach (var member_name in member_names)
			{
				var members = myType.GetMember (member_name);

				// Look for a Caption attribute that provides a (translated) description.
				string label;
				var attrs = members [0].GetCustomAttributes (typeof(CaptionAttribute), false);
				if (attrs.Length > 0)
					label = Catalog.GetString (((CaptionAttribute)attrs [0]).Caption);
				else
					label = Catalog.GetString (member_name);

				label_to_member [label] = member_name;
				labels.Add (label);
			}

			ComboBoxWidget widget = new ComboBoxWidget (labels.ToArray ());

			widget.Label = caption;
			widget.AddEvents ((int)Gdk.EventMask.ButtonPressMask);
			widget.Active = ((IList)member_names).IndexOf (GetValue (member, o).ToString ());

			widget.Changed += delegate (object sender, EventArgs e) {
				SetValue (member, o, Enum.Parse (myType, label_to_member[widget.ActiveText]));
			};

			return widget;
		}

		private ComboBoxWidget CreateComboBox (string caption, object o, System.Reflection.MemberInfo member, System.Object[] attributes)
		{
			Dictionary<string, object> dict = null;

			foreach (var attr in attributes) {
				if (attr is StaticListAttribute)
					dict = (Dictionary<string, object>)GetValue (((StaticListAttribute)attr).dictionaryName, o);
			}

			List<string> entries = new List<string> ();
			foreach (string str in dict.Keys)
				entries.Add (str);

			ComboBoxWidget widget = new ComboBoxWidget (entries.ToArray ());

			widget.Label = caption;
			widget.AddEvents ((int)Gdk.EventMask.ButtonPressMask);
			widget.Active = entries.IndexOf ((string)GetValue (member, o));

			widget.Changed += delegate (object sender, EventArgs e) {
				SetValue (member, o, widget.ActiveText);
			};

			return widget;
		}

		private HScaleSpinButtonWidget CreateDoubleSlider (string caption, object o, MemberInfo member, object[] attributes)
		{
			HScaleSpinButtonWidget widget = new HScaleSpinButtonWidget ();

			int min_value = -100;
			int max_value = 100;
			double inc_value = 0.01;
			int digits_value = 2;

			foreach (var attr in attributes) {
				if (attr is MinimumValueAttribute)
					min_value = ((MinimumValueAttribute)attr).Value;
				else if (attr is MaximumValueAttribute)
					max_value = ((MaximumValueAttribute)attr).Value;
				else if (attr is IncrementValueAttribute)
					inc_value = ((IncrementValueAttribute)attr).Value;
				else if (attr is DigitsValueAttribute)
					digits_value = ((DigitsValueAttribute)attr).Value;
			}

			widget.Label = caption;
			widget.MinimumValue = min_value;
			widget.MaximumValue = max_value;
			widget.IncrementValue = inc_value;
			widget.DigitsValue = digits_value;
			widget.DefaultValue = (double)GetValue (member, o);

			widget.ValueChanged += delegate (object sender, EventArgs e) {

				if (event_delay_timeout_id != 0)
					GLib.Source.Remove (event_delay_timeout_id);

				event_delay_timeout_id = GLib.Timeout.Add (event_delay_millis, () => {
					event_delay_timeout_id = 0;
					SetValue (member, o, widget.Value);
					return false;
				});
			};

			return widget;
		}

		private HScaleSpinButtonWidget CreateSlider (string caption, object o, MemberInfo member, object[] attributes)
		{
			HScaleSpinButtonWidget widget = new HScaleSpinButtonWidget ();

			int min_value = -100;
			int max_value = 100;
			double inc_value = 1.0;
			int digits_value = 0;

			foreach (var attr in attributes) {
				if (attr is MinimumValueAttribute)
					min_value = ((MinimumValueAttribute)attr).Value;
				else if (attr is MaximumValueAttribute)
					max_value = ((MaximumValueAttribute)attr).Value;
				else if (attr is IncrementValueAttribute)
					inc_value = ((IncrementValueAttribute)attr).Value;
				else if (attr is DigitsValueAttribute)
					digits_value = ((DigitsValueAttribute)attr).Value;
			}

			widget.Label = caption;
			widget.MinimumValue = min_value;
			widget.MaximumValue = max_value;
			widget.IncrementValue = inc_value;
			widget.DigitsValue = digits_value;
			widget.DefaultValue = (int)GetValue (member, o);

			widget.ValueChanged += delegate (object sender, EventArgs e) {

				if (event_delay_timeout_id != 0)
					GLib.Source.Remove (event_delay_timeout_id);

				event_delay_timeout_id = GLib.Timeout.Add (event_delay_millis, () => {
					event_delay_timeout_id = 0;
					SetValue (member, o, widget.ValueAsInt);
					return false;
				});
			};

			return widget;
		}

		private Gtk.CheckButton CreateCheckBox (string caption, object o, MemberInfo member, object[] attributes)
		{
			Gtk.CheckButton widget = new Gtk.CheckButton ();

			widget.Label = caption;
			widget.Active = (bool)GetValue (member, o);

			widget.Toggled += delegate (object sender, EventArgs e) {
				SetValue (member, o, widget.Active);
			};

			return widget;
		}

		private PointPickerWidget CreateOffsetPicker (string caption, object o, MemberInfo member, object[] attributes)
		{
			PointPickerWidget widget = new PointPickerWidget ();

			widget.Label = caption;
			widget.DefaultOffset = (Cairo.PointD)GetValue (member, o);

			widget.PointPicked += delegate (object sender, EventArgs e) {
				SetValue (member, o, widget.Offset);
			};

			return widget;
		}

		private PointPickerWidget CreatePointPicker (string caption, object o, MemberInfo member, object[] attributes)
		{
			PointPickerWidget widget = new PointPickerWidget ();

			widget.Label = caption;
			widget.DefaultPoint = (Gdk.Point)GetValue (member, o);

			widget.PointPicked += delegate (object sender, EventArgs e) {
				SetValue (member, o, widget.Point);
			};

			return widget;
		}

		private AnglePickerWidget CreateAnglePicker (string caption, object o, MemberInfo member, object[] attributes)
		{
			AnglePickerWidget widget = new AnglePickerWidget ();

			widget.Label = caption;
			widget.DefaultValue = (double)GetValue (member, o);

			widget.ValueChanged += delegate (object sender, EventArgs e) {
				if (event_delay_timeout_id != 0)
					GLib.Source.Remove (event_delay_timeout_id);

				event_delay_timeout_id = GLib.Timeout.Add (event_delay_millis, () => {
					event_delay_timeout_id = 0;
					SetValue (member, o, widget.Value);
					return false;
				});
			};

			return widget;
		}

		private Gtk.Label CreateHintLabel (string hint)
		{
			Gtk.Label label = new Gtk.Label (hint);
			label.LineWrap = true;

			return label;
		}

		private ReseedButtonWidget CreateSeed (string caption, object o, MemberInfo member, object[] attributes)
		{
			ReseedButtonWidget widget = new ReseedButtonWidget ();

			widget.Clicked += delegate (object sender, EventArgs e) {
				SetValue (member, o, random.Next ());
			};

			return widget;
		}
		#endregion

		#region Static Reflection Methods
		private static object GetValue (MemberInfo mi, object o)
		{
			var fi = mi as FieldInfo;
			if (fi != null)
				return fi.GetValue (o);
			var pi = mi as PropertyInfo;

			var getMethod = pi.GetGetMethod ();
			return getMethod.Invoke (o, new object[0]);
		}

		private void SetValue (MemberInfo mi, object o, object val)
		{
			var fi = mi as FieldInfo;
			var pi = mi as PropertyInfo;
			string fieldName = null;

			if (fi != null) {
				fi.SetValue (o, val);
				fieldName = fi.Name;
			} else if (pi != null) {
				var setMethod = pi.GetSetMethod ();
				setMethod.Invoke (o, new object[] { val });
				fieldName = pi.Name;
			}

			if (EffectDataChanged != null)
				EffectDataChanged (this, new PropertyChangedEventArgs (fieldName));
		}

		// Returns the type for fields and properties and null for everything else
		private static Type GetTypeForMember (MemberInfo mi)
		{
			if (mi is FieldInfo)
				return ((FieldInfo)mi).FieldType;
			else if (mi is PropertyInfo)
				return ((PropertyInfo)mi).PropertyType;

			return null;
		}

		private static string MakeCaption (string name)
		{
			var sb = new StringBuilder (name.Length);
			bool nextUp = true;

			foreach (char c in name) {
				if (nextUp) {
					sb.Append (Char.ToUpper (c));
					nextUp = false;
				} else {
					if (c == '_') {
						sb.Append (' ');
						nextUp = true;
						continue;
					}
					if (Char.IsUpper (c))
						sb.Append (' ');
					sb.Append (c);
				}
			}

			return sb.ToString ();
		}

		private object GetValue (string name, object o)
		{
			var fi = o.GetType ().GetField (name);
			if (fi != null)
				return fi.GetValue (o);
			var pi = o.GetType ().GetProperty (name);
			if (pi == null)
				return null;
			var getMethod = pi.GetGetMethod ();
			return getMethod.Invoke (o, new object[0]);
		}
		#endregion
	}

	/// <summary>
	/// Wrapper around Pinta's translation template.
	/// </summary>
	public class PintaLocalizer : IAddinLocalizer
	{
		public string GetString (string msgid)
		{
			return Mono.Unix.Catalog.GetString (msgid);
		}
	};
}
