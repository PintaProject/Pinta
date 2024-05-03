using System;
using System.Xml;

namespace Pinta.Core;

internal static class XmlExtensions
{
	public static IDisposable DisposableWriteStartElement (
		this XmlWriter writer,
		string localName)
	{
		writer.WriteStartElement (localName);
		return DisposableHelper.FromDelegate (writer.WriteEndElement);
	}
}
