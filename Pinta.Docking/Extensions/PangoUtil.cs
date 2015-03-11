// 
// PangoUtils.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Runtime.InteropServices;

namespace Pinta.Docking
{
	static class PangoUtil
	{
		internal const string LIBGTK          = "libgtk-win32-2.0-0.dll";
		internal const string LIBATK          = "libatk-1.0-0.dll";
		internal const string LIBGLIB         = "libglib-2.0-0.dll";
		internal const string LIBGDK          = "libgdk-win32-2.0-0.dll";
		internal const string LIBGOBJECT      = "libgobject-2.0-0.dll";
		internal const string LIBPANGO        = "libpango-1.0-0.dll";
		internal const string LIBPANGOCAIRO   = "libpangocairo-1.0-0.dll";
		internal const string LIBQUARTZ       = "libgtk-quartz-2.0.dylib";
		internal const string LIBGTKGLUE      = "gtksharpglue-2";
		
		/// <summary>
		/// This doesn't leak Pango layouts, unlike some other ways to create them in GTK# &lt;= 2.12.11
		/// </summary>
		public static Pango.Layout CreateLayout (Widget widget)
		{
			var ptr = gtk_widget_create_pango_layout (widget.Handle, IntPtr.Zero);
			return ptr == IntPtr.Zero? null : new Pango.Layout (ptr);
		}

		public static Pango.Layout CreateLayout (Widget widget, string text)
		{
			IntPtr textPtr = text == null? IntPtr.Zero : GLib.Marshaller.StringToPtrGStrdup (text);
			
			var ptr = gtk_widget_create_pango_layout (widget.Handle, textPtr);
			
			if (textPtr != IntPtr.Zero)
				GLib.Marshaller.Free (textPtr);
			
			return ptr == IntPtr.Zero? null : new Pango.Layout (ptr);
		}
		
		public static Pango.Layout CreateLayout (PrintContext context)
		{
			var ptr = gtk_print_context_create_pango_layout (context.Handle);
			return ptr == IntPtr.Zero? null : new Pango.Layout (ptr);
		}
		
		[DllImport (LIBGTK, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr gtk_widget_create_pango_layout (IntPtr widget, IntPtr text);
		
		[DllImport (LIBGTK, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr gtk_print_context_create_pango_layout (IntPtr context);
	}
	
	/// <summary>
	/// This creates a Pango list and applies attributes to it with *much* less overhead than the GTK# version.
	/// </summary>
	class FastPangoAttrList : IDisposable
	{
		IntPtr list;
		
		public FastPangoAttrList ()
		{
			list = pango_attr_list_new ();
		}
		
		public void AddStyleAttribute (Pango.Style style, uint start, uint end)
		{
			Add (pango_attr_style_new (style), start, end);
		}
		
		public void AddWeightAttribute (Pango.Weight weight, uint start, uint end)
		{
			Add (pango_attr_weight_new (weight), start, end);
		}
		
		public void AddForegroundAttribute (Gdk.Color color, uint start, uint end)
		{
			Add (pango_attr_foreground_new (color.Red, color.Green, color.Blue), start, end);
		}
		
		public void AddBackgroundAttribute (Gdk.Color color, uint start, uint end)
		{
			Add (pango_attr_background_new (color.Red, color.Green, color.Blue), start, end);
		}
		
		public void AddUnderlineAttribute (Pango.Underline underline, uint start, uint end)
		{
			Add (pango_attr_underline_new (underline), start, end);
		}
		
		void Add (IntPtr attribute, uint start, uint end)
		{
			unsafe {
				PangoAttribute *attPtr = (PangoAttribute *) attribute;
				attPtr->start_index = start;
				attPtr->end_index = end;
			}
			pango_attr_list_insert (list, attribute);
		}

		/// <summary>
		/// Like Splice, except it only offsets/clamps the inserted items, doesn't affect items already in the list.
		/// </summary>
		public void InsertOffsetList (Pango.AttrList atts, uint startOffset, uint endOffset)
		{
			//HACK: atts.Iterator.Attrs broken (throws NRE), so manually P/Invoke
			var iter = pango_attr_list_get_iterator (atts.Handle);
			try {
				do {
					IntPtr list = pango_attr_iterator_get_attrs (iter);
					try {
						int len = g_slist_length (list);
						for (uint i = 0; i < len; i++) {
							IntPtr val = g_slist_nth_data (list, i);
							AddOffsetCopy (val, startOffset, endOffset);
						}
					} finally {
						g_slist_free (list);
					}
				} while (pango_attr_iterator_next (iter));
			} finally {
				pango_attr_iterator_destroy (iter);
			}
		}

		void AddOffsetCopy (IntPtr attr, uint startOffset, uint endOffset)
		{
			var copy = pango_attribute_copy (attr);
			unsafe {
				PangoAttribute *attPtr = (PangoAttribute *) copy;
				attPtr->start_index = startOffset + attPtr->start_index;
				attPtr->end_index = System.Math.Min (endOffset, startOffset + attPtr->end_index);
			}
			pango_attr_list_insert (list, copy);
		}
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attr_style_new (Pango.Style style);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attr_stretch_new (Pango.Stretch stretch);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attr_weight_new (Pango.Weight weight);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attr_foreground_new (ushort red, ushort green, ushort blue);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attr_background_new (ushort red, ushort green, ushort blue);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attr_underline_new (Pango.Underline underline);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attr_list_new ();

		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_attr_list_unref (IntPtr list);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_attr_list_insert (IntPtr list, IntPtr attr);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_layout_set_attributes (IntPtr layout, IntPtr attrList);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_attr_list_splice (IntPtr attr_list, IntPtr other, Int32 pos, Int32 len);

		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attribute_copy (IntPtr attr);

		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attr_list_get_iterator (IntPtr list);
		
		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern bool pango_attr_iterator_next (IntPtr iterator);

		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_attr_iterator_destroy (IntPtr iterator);

		[DllImport (PangoUtil.LIBPANGO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_attr_iterator_get_attrs (IntPtr iterator);

		[DllImport (PangoUtil.LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		private static extern int g_slist_length (IntPtr l);

		[DllImport (PangoUtil.LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr g_slist_nth_data (IntPtr l, uint n);

		[DllImport (PangoUtil.LIBGLIB, CallingConvention = CallingConvention.Cdecl)]
		private static extern void g_slist_free (IntPtr l);

		public void Splice (Pango.AttrList attrs, int pos, int len)
		{
			pango_attr_list_splice (list, attrs.Handle, pos, len);
		}
		
		public void AssignTo (Pango.Layout layout)
		{
			pango_layout_set_attributes (layout.Handle, list);
		}

		[StructLayout (LayoutKind.Sequential)]
		struct PangoAttribute
		{
			public IntPtr klass;
			public uint start_index;
			public uint end_index;
		}
		
		public void Dispose ()
		{
			if (list != IntPtr.Zero) {
				GC.SuppressFinalize (this);
				Destroy ();
			}
		}
		
		//NOTE: the list destroys all its attributes when the ref count reaches zero
		void Destroy ()
		{
			pango_attr_list_unref (list);
			list = IntPtr.Zero;
		}
		
		~FastPangoAttrList ()
		{
			GLib.Idle.Add (delegate {
				Destroy ();
				return false;
			});
		}
	}
}