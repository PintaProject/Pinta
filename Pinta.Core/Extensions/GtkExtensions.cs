// 
// GtkExtensions.cs
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

using System;
using Gtk;

namespace Pinta.Core
{
	public static class GtkExtensions
	{
		public const int MouseLeftButton = 1;
		public const int MouseMiddleButton = 2;
		public const int MouseRightButton = 3;

		public static void AddWidgetItem (this Toolbar tb, Widget w)
		{
			w.Show ();
			ToolItem ti = new ToolItem ();
			ti.Add (w);
			ti.Show ();
			tb.Insert (ti, tb.NItems);
		}
		
		public static void AppendItem (this Toolbar tb, ToolItem item)
		{
			item.Show ();
			tb.Insert (item, tb.NItems);
		}

        public static void AppendItem (this Statusbar tb, ToolItem item, uint padding = 0)
        {
            item.Show ();
            tb.PackEnd (item, false, false, padding);
        }

        public static Gtk.ToolButton CreateToolBarItem (this Command action)
        {
			var item = new ToolButton(null, action.ShortLabel ?? action.Label)
			{
				ActionName = action.FullName,
				TooltipText = action.Tooltip ?? action.Label,
				IsImportant = action.IsImportant,
				IconName = action.IconName
			};
            return item;
        }

		public static void AddAction(this Gtk.Application app, Command action)
		{
			app.AddAction(action.Action);
		}

		public static void AddAccelAction(this Gtk.Application app, Command action, string accel)
        {
			app.AddAction(action);
			app.SetAccelsForAction(action.FullName, new string[] { accel });
		}

		public static void AddAccelAction(this Gtk.Application app, Command action, string[] accels)
		{
			app.AddAction(action);
			app.SetAccelsForAction(action.FullName, accels);
		}

		public static void Remove(this GLib.Menu menu, Command action)
        {
			for (int i = 0; i < menu.NItems; ++i)
			{
				var name_attr = (string)menu.GetItemAttributeValue(i, "action", GLib.VariantType.String);
				if (name_attr == action.FullName)
				{
					menu.Remove(i);
					return;
				}
			}
		}

		public static void AppendMenuItemSorted(this GLib.Menu menu, GLib.MenuItem item)
		{
			var new_label = (string)item.GetAttributeValue("label", GLib.VariantType.String);

			for (int i = 0; i < menu.NItems; i++)
			{
				var label = (string)menu.GetItemAttributeValue(i, "label", GLib.VariantType.String);
				if (string.Compare(label, new_label) > 0)
				{
					menu.InsertItem(i, item);
					return;
				}
			}

			menu.AppendItem(item);
		}

		public static void Toggle (this Gtk.ToggleToolButton button)
        {
            button.Active = !button.Active;
        }

		/// <summary>
		/// Initialize an image preview widget for the dialog
		/// </summary>
		public static void AddImagePreview (this FileChooserDialog dialog)
		{
			dialog.PreviewWidget = new Image ();
			dialog.UpdatePreview += new EventHandler (OnUpdateImagePreview);
		}

		private const int MaxPreviewWidth = 256;
		private const int MaxPreviewHeight = 512;

		/// <summary>
		/// Update the image preview widget of a FileChooserDialog
		/// </summary>
		private static void OnUpdateImagePreview (object sender, EventArgs e)
		{
			FileChooserDialog dialog = (FileChooserDialog)sender;
			Image preview = (Image)dialog.PreviewWidget;

			if (preview.Pixbuf != null) {
				preview.Pixbuf.Dispose ();
			}

			try {
				Gdk.Pixbuf pixbuf = null;
				string filename = dialog.PreviewFilename;

				IImageImporter importer = PintaCore.System.ImageFormats.GetImporterByFile (filename);

				if (importer != null) {
					pixbuf = importer.LoadThumbnail (filename, MaxPreviewWidth, MaxPreviewHeight, dialog);
				}

				if (pixbuf == null) {
					dialog.PreviewWidgetActive = false;
					return;
				}

				// Resize the thumbnail in case the importer didn't.
				if (pixbuf.Width > MaxPreviewWidth || pixbuf.Width > MaxPreviewHeight) {
					double ratio = Math.Min ((double)MaxPreviewWidth / pixbuf.Width,
					                         (double)MaxPreviewHeight / pixbuf.Height);
					var old_pixbuf = pixbuf;
					pixbuf = pixbuf.ScaleSimple ((int)(ratio * pixbuf.Width),
					                             (int)(ratio * pixbuf.Height),
					                             Gdk.InterpType.Bilinear);
					old_pixbuf.Dispose ();
				}

				// add padding so that small images don't cause the dialog to shrink
				preview.Xpad = (MaxPreviewWidth - pixbuf.Width) / 2;
				preview.Pixbuf = pixbuf;
				dialog.PreviewWidgetActive = true;
			} catch (GLib.GException) {
				// if the image preview failed, don't show the preview widget
				dialog.PreviewWidgetActive = false;
			}
		}

        public static int GetItemCount (this ComboBox combo)
        {
            return (combo.Model as ListStore).IterNChildren ();
        }

        public static int FindValue<T> (this ComboBox combo, T value)
        {
            for (var i = 0; i < combo.GetItemCount (); i++)
                if (combo.GetValueAt<T> (i).Equals (value))
                    return i;

            return -1;
        }

        public static T GetValueAt<T> (this ComboBox combo, int index)
        {
            TreeIter iter;
            
            // Set the tree iter to the correct row
            (combo.Model as ListStore).IterNthChild (out iter, index);

            // Retrieve the value of the first column at that row
            return (T)combo.Model.GetValue (iter, 0);
        }

        public static void SetValueAt (this ComboBox combo, int index, object value)
        {
            TreeIter iter;

            // Set the tree iter to the correct row
            (combo.Model as ListStore).IterNthChild (out iter, index);

            // Set the value of the first column at that row
            combo.Model.SetValue (iter, 0, value);
        }

		public static Gdk.Pixbuf LoadIcon(this Gtk.IconTheme theme, string icon_name, int size)
        {
			// Simple wrapper to avoid the verbose IconLookupFlags parameter.
			return theme.LoadIcon(icon_name, size, Gtk.IconLookupFlags.ForceSize);
		}

		/// <summary>
		/// Returns the Cancel / Open button pair in the correct order for the current platform.
		/// This can be used with the Gtk.Dialog constructor.
		/// </summary>
		public static object[] DialogButtonsCancelOpen()
		{
			if (PintaCore.System.OperatingSystem == OS.Windows)
            {
				return new object[] {
                    Gtk.Stock.Open,
                    Gtk.ResponseType.Ok,
                    Gtk.Stock.Cancel,
                    Gtk.ResponseType.Cancel
				};
            }
			else
            {
				return new object[] {
                    Gtk.Stock.Cancel,
                    Gtk.ResponseType.Cancel,
                    Gtk.Stock.Open,
                    Gtk.ResponseType.Ok
				};
            }
        }

		/// <summary>
		/// Returns the Cancel / Save button pair in the correct order for the current platform.
		/// This can be used with the Gtk.Dialog constructor.
		/// </summary>
		public static object[] DialogButtonsCancelSave()
		{
			if (PintaCore.System.OperatingSystem == OS.Windows)
            {
				return new object[] {
                    Gtk.Stock.Save,
                    Gtk.ResponseType.Ok,
                    Gtk.Stock.Cancel,
                    Gtk.ResponseType.Cancel
				};
            }
			else
            {
				return new object[] {
                    Gtk.Stock.Cancel,
                    Gtk.ResponseType.Cancel,
                    Gtk.Stock.Save,
                    Gtk.ResponseType.Ok
				};
            }
        }

		/// <summary>
		/// Returns the Cancel / Ok button pair in the correct order for the current platform.
		/// This can be used with the Gtk.Dialog constructor.
		/// </summary>
		public static object[] DialogButtonsCancelOk()
		{
			if (PintaCore.System.OperatingSystem == OS.Windows)
            {
				return new object[] {
                    Gtk.Stock.Ok,
                    Gtk.ResponseType.Ok,
                    Gtk.Stock.Cancel,
                    Gtk.ResponseType.Cancel
				};
            }
			else
            {
				return new object[] {
                    Gtk.Stock.Cancel,
                    Gtk.ResponseType.Cancel,
                    Gtk.Stock.Ok,
                    Gtk.ResponseType.Ok
				};
            }
        }
	}
}
