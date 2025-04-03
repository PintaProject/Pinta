namespace Pinta.Core;

partial class GtkExtensions
{
	/// <summary>
	/// Find the index of a string in a Gtk.StringList.
	/// </summary>
	public static bool FindString (
		this Gtk.StringList list,
		string s,
		out uint index)
	{
		for (uint i = 0, n = list.GetNItems (); i < n; ++i) {

			if (list.GetString (i) != s)
				continue;

			index = i;
			return true;
		}

		index = 0;
		return false;
	}
}
