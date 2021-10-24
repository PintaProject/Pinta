using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Gdk;
using Gtk;
using Pango;

namespace Pinta.Core
{
	// TODO-GTK3: These are known obsolete GTK methods that we should replace, but haven't yet.
	// Moving them here so that they don't drown out other warnings.
	public static class ObsoleteExtensions
	{
#pragma warning disable CS0612
		public static void AddToIconFactory (IconFactory factory, string stock_id, IconSet icon_set)
			=> factory.Add (stock_id, icon_set);

		public static void AddDefaultToIconFactory (IconFactory factory)
			=> factory.AddDefault ();

		public static FontDescription GetStyleContextFont (StyleContext context, StateFlags flags)
			=> context.GetFont (flags);
	}
}
