/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pinta.Core;

public static class OtherExtensions
{
	public static IEnumerable<RectangleI> ToRows (this RectangleI original)
	{
		if (original.Height < 0) throw new ArgumentException ("Height cannot be negative", nameof (original));
		if (original.Height == 0) yield break;
		for (int i = 0; i < original.Height; i++)
			yield return new (
				original.X,
				original.Y + i,
				original.Width,
				1);
	}

	public static ColorBgra RandomColorBgra (this Random random, bool includeAlpha = false)
	{
		Span<byte> colorBytes = stackalloc byte[4];
		random.NextBytes (colorBytes);
		ColorBgra baseColor = ColorBgra.FromBgr (colorBytes[0], colorBytes[1], colorBytes[2]);
		return
			includeAlpha
			? baseColor.NewAlpha (colorBytes[3])
			: baseColor;
	}

	public static IReadOnlyList<IReadOnlyList<PointI>> CreatePolygonSet (
		this BitMask stencil,
		RectangleD bounds,
		PointI translateOffset)
	{
		if (stencil.IsEmpty)
			return [];

		List<IReadOnlyList<PointI>> polygons = [];
		List<PointI> pts = [];

		PointI start = bounds.Location ().ToInt ();

		int count = 0;

		// find all islands
		while (true) {

			bool startFound = false;

			while (true) {

				if (stencil[start]) {
					startFound = true;
					break;
				}

				start = start with { X = start.X + 1 };

				if (start.X < bounds.Right)
					continue;

				start = start with { X = (int) bounds.X, Y = start.Y + 1 };

				if (start.Y >= bounds.Bottom)
					break;
			}

			if (!startFound)
				break;

			pts.Clear ();

			PointI last = new (start.X, start.Y + 1);
			PointI curr = new (start.X, start.Y);
			PointI next = curr;

			// trace island outline
			while (true) {

				PointI currLastDelta = curr - last;

				PointI left = new (
					X: (currLastDelta.X + currLastDelta.Y + 2) / 2 + curr.X - 1,
					Y: (currLastDelta.Y - currLastDelta.X + 2) / 2 + curr.Y - 1);

				PointI right = new (
					X: (currLastDelta.X - currLastDelta.Y + 2) / 2 + curr.X - 1,
					Y: (currLastDelta.Y + currLastDelta.X + 2) / 2 + curr.Y - 1);

				if (bounds.ContainsPoint ((PointD) left) && stencil[left]) {
					// go left
					next -= currLastDelta.Rotated90CCW ();
				} else if (bounds.ContainsPoint ((PointD) right) && stencil[right]) {
					// go straight
					next += currLastDelta;
				} else {
					// turn right
					next += currLastDelta.Rotated90CCW ();
				}

				if (
					Math.Sign (next.X - curr.X) != Math.Sign (currLastDelta.X) ||
					Math.Sign (next.Y - curr.Y) != Math.Sign (currLastDelta.Y)) {
					pts.Add (curr);
					++count;
				}

				last = curr;
				curr = next;

				if (next == start)
					break;
			}

			PointI[] points = [.. pts];

			var scans = CairoExtensions.GetScans (points);

			foreach (var scan in scans)
				stencil.Invert (scan);

			CairoExtensions.TranslatePointsInPlace (points, translateOffset);

			polygons.Add (points);
		}

		return polygons;
	}

	public static async Task LaunchUri (this SystemManager system, string uri)
	{
		// Workaround for macOS, which produces an "unsupported on current backend" error (https://gitlab.gnome.org/GNOME/gtk/-/issues/6788)
		if (system.OperatingSystem == OS.Mac) {
			Process process = Process.Start ("open", uri);
			process.WaitForExit ();
		} else {
			Gtk.UriLauncher launcher = Gtk.UriLauncher.New (uri);
			await launcher.LaunchAsync (PintaCore.Chrome.MainWindow);
		}
	}

	internal static Task ShowUnsupportedFormatDialog (
		this ChromeManager chrome,
		Gtk.Window parent,
		IEnumerable<PaletteDescriptor> supportedPalettes,
		string filename,
		string message,
		string errors)
	{
		StringBuilder details = new ();

		details.AppendLine (Translations.GetString ("Could not open file: {0}", filename));
		details.AppendLine (Translations.GetString ("Pinta supports the following palette formats:"));

		var extensions =
			from format in supportedPalettes
			where format.Loader != null
			from extension in format.Extensions
			where char.IsLower (extension.FirstOrDefault ())
			orderby extension
			select extension;

		details.AppendJoin (", ", extensions);

		return chrome.ShowErrorDialog (
			parent,
			message,
			details.ToString (),
			errors);
	}
}
