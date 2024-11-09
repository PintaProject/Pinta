using System;
using Cairo;

namespace Pinta.Core.Tests;

internal static class Utilities
{
	static Utilities ()
	{
		Gio.Module.Initialize ();
		GdkPixbuf.Module.Initialize ();
		Cairo.Module.Initialize ();
		Gdk.Module.Initialize ();
	}

	/// <returns>
	/// <see langword="true"/> if the files with these file names
	/// are byte-for-byte the same, <see langword="false"/> if not
	/// </returns>
	internal static bool AreFilesEqual (string fileName1, string fileName2)
	{
		var context1 = OpenFile (fileName1);
		var context2 = OpenFile (fileName2);
		return AreFilesEqual (context1.DataStream, context2.DataStream); ;
	}

	internal static DataInputStreamContext OpenFile (string filePath)
	{
		return new (filePath);
	}

	internal static string GetAssetPath (string fileName)
	{
		const string ASSETS_FOLDER = "Assets";
		string assemblyPath = System.IO.Path.GetDirectoryName (typeof (Utilities).Assembly.Location)!;
		return System.IO.Path.Combine (assemblyPath, ASSETS_FOLDER, fileName);
	}

	internal sealed class DataInputStreamContext : IDisposable
	{
		private Gio.FileInputStream FileStream { get; }
		public Gio.DataInputStream DataStream { get; }

		internal DataInputStreamContext (string filePath)
		{
			Gio.File file = Gio.FileHelper.NewForPath (filePath);
			Gio.FileInputStream fs = file.Read (null);
			FileStream = fs;
			DataStream = Gio.DataInputStream.New (fs);
		}

		public void Dispose ()
		{
			DataStream.Dispose ();
			FileStream.Dispose ();
		}
	}

	internal static bool AreFilesEqual (Gio.DataInputStream dataStream1, Gio.DataInputStream dataStream2)
	{
		dataStream1.Seek (0, GLib.SeekType.Set, null);
		dataStream2.Seek (0, GLib.SeekType.Set, null);

		const int BUFFER_SIZE = 4096;

		Span<byte> buffer1 = stackalloc byte[BUFFER_SIZE];
		Span<byte> buffer2 = stackalloc byte[BUFFER_SIZE];

		while (true) {

			long bytesRead1 = dataStream1.Read (buffer1, null);
			long bytesRead2 = dataStream2.Read (buffer2, null);

			//Console.WriteLine ($"1: {bytesRead1} bytes read");
			//Console.WriteLine ($"2: {bytesRead2} bytes read");

			if (bytesRead1 != bytesRead2) // Different file sizes
			{
				//Console.WriteLine ("Different file sizes");
				return false;
			}

			if (bytesRead1 == 0) // End of file
			{
				//Console.WriteLine ("End of file");
				return true;
			}

			for (int i = 0; i < bytesRead1; i++) {
				if (buffer1[i] != buffer2[i]) // Differing byte
				{
					//Console.WriteLine ($"Differing byte at position {i} of buffer");
					return false;
				}
			}
		}
	}

	public static ImageSurface LoadImage (string imageFilePath)
	{
		var file = Gio.FileHelper.NewForPath (imageFilePath);
		using var fs = file.Read (null);
		try {
			var bg = GdkPixbuf.Pixbuf.NewFromStream (fs, cancellable: null)!; // NRT: only nullable when error is thrown.
			var surf = CairoExtensions.CreateImageSurface (Format.Argb32, bg.Width, bg.Height);
			using var context = new Cairo.Context (surf);
			context.DrawPixbuf (bg, PointD.Zero);
			return surf;
		} finally {
			fs.Close (null);
		}
	}
}
