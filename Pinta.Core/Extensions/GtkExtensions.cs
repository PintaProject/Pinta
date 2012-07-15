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
		
		public static void AppendSeparator (this Menu menu)
		{
			SeparatorMenuItem smi = new SeparatorMenuItem ();
			smi.Show ();
			menu.Append (smi);
		}

		public static MenuItem AppendItem (this Menu menu, MenuItem item)
		{
			menu.Append (item);
			return item;
		}
		
		public static Gtk.Action AppendAction (this Menu menu, string actionName, string actionLabel, string actionTooltip, string actionIcon)
		{
			Gtk.Action action = new Gtk.Action (actionName, actionLabel, actionTooltip, actionIcon);
			menu.AppendItem ((MenuItem)action.CreateMenuItem ());
			return action;
		}

		public static Gtk.ToggleAction AppendToggleAction (this Menu menu, string actionName, string actionLabel, string actionTooltip, string actionIcon)
		{
			Gtk.ToggleAction action = new Gtk.ToggleAction (actionName, actionLabel, actionTooltip, actionIcon);
			menu.AppendItem ((MenuItem)action.CreateMenuItem ());
			return action;
		}

		public static MenuItem AppendMenuItemSorted (this Menu menu, MenuItem item)
		{
			var text = item.GetText ();

			for (int i = 0; i < menu.Children.Length; i++)
				if (string.Compare (((menu.Children[i]) as MenuItem).GetText (), text) > 0) {
					menu.Insert (item, i);
					return item;
				}

			menu.AppendItem (item);
			return item;
		}

		public static string GetText (this MenuItem item)
		{
			foreach (var child in item.AllChildren)
				if (child is Label)
					return (child as Label).Text;

			return string.Empty;
		}

		public static Gtk.ToolItem CreateToolBarItem (this Gtk.Action action)
		{
			Gtk.ToolItem item = (Gtk.ToolItem)action.CreateToolItem ();
			item.TooltipText = action.Label;
			return item;
		}
		
		public static Gtk.ImageMenuItem CreateAcceleratedMenuItem (this Gtk.Action action, Gdk.Key key, Gdk.ModifierType mods)
		{
			ImageMenuItem item = (ImageMenuItem)action.CreateMenuItem ();
			
			(item.Child as AccelLabel).AccelWidget = item;
			item.AddAccelerator ("activate", PintaCore.Actions.AccelGroup, new AccelKey (key, mods, AccelFlags.Visible));

			return item;
		}

		public static Gtk.CheckMenuItem CreateAcceleratedMenuItem (this Gtk.ToggleAction action, Gdk.Key key, Gdk.ModifierType mods)
		{
			CheckMenuItem item = (CheckMenuItem)action.CreateMenuItem ();

			(item.Child as AccelLabel).AccelWidget = item;
			item.AddAccelerator ("activate", PintaCore.Actions.AccelGroup, new AccelKey (key, mods, AccelFlags.Visible));

			return item;
		}

		public static Gtk.MenuItem CreateSubMenuItem (this Gtk.Action action)
		{
			MenuItem item = (MenuItem)action.CreateMenuItem ();
			
			Menu sub_menu = new Menu ();
			item.Submenu = sub_menu;

			return item;
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
		/// <param name="sender"></param>
		private static void OnUpdateImagePreview (object sender, EventArgs e)
		{
			FileChooserDialog dialog = (FileChooserDialog)sender;
			Image preview = (Image)dialog.PreviewWidget;

            if (preview.Pixbuf != null)
            {
                preview.Pixbuf.Dispose ();
            }

			try
			{
                Gdk.Pixbuf pixbuf = null;
                int imageWidth, imageHeight;
                string filename = dialog.PreviewFilename;

                var imageInfo = Gdk.Pixbuf.GetFileInfo (filename, out imageWidth, out imageHeight);

                if (imageInfo == null)
                {
                    dialog.PreviewWidgetActive = false;
                    return;
                }

                // scale down images that are too large, but don't scale up small images
                if (imageWidth > MaxPreviewWidth || imageHeight > MaxPreviewHeight)
                {
                    pixbuf = new Gdk.Pixbuf (filename, MaxPreviewWidth, MaxPreviewHeight, true);
                }
                else
                {
                    pixbuf = new Gdk.Pixbuf (filename);
                }

                // add padding so that small images don't cause the dialog to shrink
                preview.Xpad = (MaxPreviewWidth - pixbuf.Width) / 2;
                preview.Pixbuf = pixbuf;
                dialog.PreviewWidgetActive = true;
			}
			catch (GLib.GException)
			{
				// if the image preview failed, don't show the preview widget
				dialog.PreviewWidgetActive = false;
			}
		}
	}
}
