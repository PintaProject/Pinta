using System;
using System.IO;

namespace Pinta.Core.Tests;

internal static class Utilities
{
	/// <returns>
	/// <see langword="true"/> if the files are byte-for-byte the same,
	/// <see langword="false"/> if not
	/// </returns>
	internal static bool AreFilesEqual (string fileName1, string fileName2)
	{
		const string ASSETS_FOLDER = "Assets";
		string assemblyPath = Path.GetDirectoryName (typeof (Utilities).Assembly.Location)!;
		Gio.File file1 = Gio.FileHelper.NewForPath (Path.Combine (assemblyPath, ASSETS_FOLDER, fileName1));
		Gio.File file2 = Gio.FileHelper.NewForPath (Path.Combine (assemblyPath, ASSETS_FOLDER, fileName2));
		using Gio.FileInputStream fs1 = file1.Read (null);
		using Gio.FileInputStream fs2 = file2.Read (null);
		using Gio.DataInputStream ds1 = Gio.DataInputStream.New (fs1);
		using Gio.DataInputStream ds2 = Gio.DataInputStream.New (fs2);
		const int BUFFER_SIZE = 4096;
		Span<byte> buffer1 = stackalloc byte[BUFFER_SIZE];
		Span<byte> buffer2 = stackalloc byte[BUFFER_SIZE];
		while (true) {
			long bytesRead1 = ds1.Read (buffer1, null);
			long bytesRead2 = ds1.Read (buffer2, null);
			if (bytesRead1 != bytesRead2) // Different file sizes
				return false;
			if (bytesRead1 == 0) // End of file
				return true;
			for (int i = 0; i < bytesRead1; i++) {
				if (buffer1[i] != buffer2[i]) // Differing byte
					return false;
			}
		}
	}
}
