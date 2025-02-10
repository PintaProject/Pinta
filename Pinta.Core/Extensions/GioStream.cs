/*
 * GioStream.cs: provide a System.IO.Stream api to [Input|Output]Streams
 *
 * Author(s):
 *	Stephane Delcroix  (stephane@delcroix.org)
 *
 * Copyright (c) 2008 Novell, Inc.
 *
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 */
using System;
using System.Diagnostics;

namespace Pinta.Core;

public sealed class GioStream : System.IO.Stream
{
	readonly GObject.Object stream;
	readonly bool can_read;
	readonly bool can_seek;
	readonly bool can_write;
	bool is_disposed;

	public GioStream (Uri uri, System.IO.FileMode mode)
		=> throw new NotImplementedException ();

	public GioStream (string filename, System.IO.FileMode mode)
		=> throw new NotImplementedException ();

	public GioStream (Gio.InputStream stream)
	{
		this.stream = stream;
		can_read = true;
		can_seek = stream is Gio.Seekable s && s.CanSeek ();
	}

	public GioStream (Gio.OutputStream stream)
	{
		this.stream = stream;
		can_write = true;
		can_seek = stream is Gio.Seekable s && s.CanSeek ();
	}

	public GioStream (Gio.IOStream stream)
	{
		this.stream = stream;
		can_read = true;
		can_write = true;
		can_seek = stream is Gio.Seekable s && s.CanSeek ();
	}

	public override bool CanSeek => can_seek;

	public override bool CanRead => can_read;

	public override bool CanWrite => can_write;

	public override long Length {
		get {
			if (!CanSeek) throw new NotSupportedException ("This stream doesn't support seeking");
			if (is_disposed) throw new ObjectDisposedException ("The stream is closed");
			return stream switch {
				Gio.FileInputStream istream => istream.QueryInfo ("standard::size", null).GetSize (),
				Gio.FileOutputStream ostream => ostream.QueryInfo ("standard::size", null).GetSize (),
				Gio.FileIOStream iostream => iostream.QueryInfo ("standard::size", null).GetSize (),
				_ => throw new NotImplementedException ($"not implemented for {stream.GetType ()} streams"),
			};
		}
	}

	public override long Position {
		get {
			if (!CanSeek) throw new NotSupportedException ("This stream doesn't support seeking");
			if (is_disposed) throw new ObjectDisposedException ("The stream is closed");
			return ((Gio.Seekable) stream).Tell ();
		}
		set => Seek (value, System.IO.SeekOrigin.Begin);
	}

	public override void Flush ()
	{
		if (is_disposed)
			throw new ObjectDisposedException ("The stream is closed");
	}

	public override int Read (byte[] buffer, int offset, int count)
	{
		if (offset + count - 1 > buffer.Length)
			throw new ArgumentException ($"({nameof (offset)} + {nameof (count)} - {1}) is greater than the length of buffer");

		ArgumentOutOfRangeException.ThrowIfNegative (offset);
		ArgumentOutOfRangeException.ThrowIfNegative (count);

		if (!CanRead)
			throw new NotSupportedException ("The stream does not support reading");

		if (is_disposed)
			throw new ObjectDisposedException ("The stream is closed");

		Gio.InputStream? input_stream = stream switch {
			Gio.InputStream istream => istream,
			Gio.IOStream iostream => iostream.InputStream,
			_ => null,
		};

		if (input_stream == null)
			throw new UnreachableException ();

		return (int) input_stream.Read (new Span<byte> (buffer, offset, count), null);
	}

	public override void Write (byte[] buffer, int offset, int count)
	{
		if (offset + count > buffer.Length)
			throw new ArgumentException ($"({nameof (offset)} + {nameof (count)}) is greater than the length of buffer");

		ArgumentOutOfRangeException.ThrowIfNegative (offset);
		ArgumentOutOfRangeException.ThrowIfNegative (count);

		if (!CanWrite)
			throw new NotSupportedException ("The stream does not support writing");

		if (is_disposed)
			throw new ObjectDisposedException ("The stream is closed");

		Gio.OutputStream? output_stream = stream switch {
			Gio.OutputStream ostream => ostream,
			Gio.IOStream iostream => iostream.OutputStream,
			_ => null,
		};

		if (output_stream == null)
			throw new UnreachableException ();

		output_stream.Write (new Span<byte> (buffer, offset, count), null);
	}

	public override long Seek (long offset, System.IO.SeekOrigin origin)
	{
		if (!CanSeek)
			throw new NotSupportedException ("This stream doesn't support seeking");

		if (is_disposed)
			throw new ObjectDisposedException ("The stream is closed");

		Gio.Seekable seekable = (Gio.Seekable) stream;

		GLib.SeekType seek_type = origin switch {
			System.IO.SeekOrigin.Current => GLib.SeekType.Cur,
			System.IO.SeekOrigin.End => GLib.SeekType.End,
			_ => GLib.SeekType.Set,
		};

		seekable.Seek (offset, seek_type, null);
		return Position;
	}

	public override void SetLength (long value)
	{
		if (!CanSeek || !CanWrite)
			throw new NotSupportedException ("This stream doesn't support seeking");

		Gio.Seekable seekable = (Gio.Seekable) stream;

		if (!seekable.CanTruncate ())
			throw new NotSupportedException ("This stream doesn't support truncating");

		if (is_disposed)
			throw new ObjectDisposedException ("The stream is closed");

		seekable.Truncate (value, null);
	}

	public override void Close ()
	{
		switch (stream) {
			case Gio.InputStream istream:
				istream.Close (null);
				break;
			case Gio.OutputStream ostream:
				ostream.Close (null);
				break;
			case Gio.IOStream iostream:
				iostream.Close (null);
				break;
		}

		is_disposed = true;
	}
}
