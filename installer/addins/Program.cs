using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Xml;

// Create a map from the header properties to their untranslated values, e.g. "Name" => "MyEffect"
static Dictionary<string, string> ExtractHeaderProperties (XmlDocument manifestDoc)
{
	// The properties that we care about translating.
	string[] headerProperties = ["Name", "Description"];

	Dictionary<string, string> propertyMap = new ();
	foreach (string propertyName in headerProperties) {
		XmlNode? node = manifestDoc.SelectSingleNode ($"/Addin/Header/{propertyName}");
		if (node is null)
			throw new InvalidDataException ($"Add-in manifest does not specify header property '{propertyName}'");

		propertyMap[node.Name] = node.InnerText;
	}

	return propertyMap;
}

// Insert translations from the resource files into the manifest (.addin.xml)
// See https://github.com/mono/mono-addins/wiki/The-add-in-header
static void LocalizeManifest (FileInfo manifestFile, FileInfo[] resourceFiles)
{
	Console.WriteLine ($"Loading manifest from {manifestFile}");
	XmlDocument manifestDoc = new () { PreserveWhitespace = true };
	manifestDoc.Load (manifestFile.FullName);

	Dictionary<string, string> headerProperties = ExtractHeaderProperties (manifestDoc);

	XmlNode addinRootNode = manifestDoc.DocumentElement
		?? throw new InvalidDataException ("Failed to find addin root node");
	XmlNode addinHeaderNode = addinRootNode.SelectSingleNode ("Header")
		?? throw new InvalidDataException ("Failed to find addin header node");

	XmlElement localizerNode = manifestDoc.CreateElement ("Localizer");
	localizerNode.SetAttribute ("type", "StringTable");
	addinRootNode.AppendChild (localizerNode);

	foreach (FileInfo resourceFile in resourceFiles) {
		// Parse the locale name from filenames like Language.es.resx.
		// We don't need to process the template file (Language.resx).
		string[] components = resourceFile.Name.Split ('.');
		if (components.Length != 3) {
			Console.WriteLine ($"Skipping file {resourceFile}");
			continue;
		}

		string langCode = components[1];

		Console.WriteLine ($"{langCode}: Loading resource {resourceFile}");
		XmlDocument resourceDoc = new ();
		resourceDoc.Load (resourceFile.FullName);

		// Add translations for header properties.
		foreach ((string propertyName, string propertyText) in headerProperties) {
			XmlNode? translationNode = resourceDoc.SelectSingleNode ($"/root/data[@name='{propertyText}']/value");
			if (translationNode is not null) {
				Console.WriteLine ($" - Adding translation for '{propertyName}': '{translationNode.InnerText}'");

				// Add a sibling node, e.g. <Name locale="es">Translated string</Name>
				XmlElement newNode = manifestDoc.CreateElement (propertyName);
				newNode.SetAttribute ("locale", langCode);
				newNode.InnerText = translationNode.InnerText;

				addinHeaderNode.AppendChild (newNode);
			} else
				Console.WriteLine ($" - Did not find translation for {propertyName}");
		}

		XmlElement localeNode = manifestDoc.CreateElement ("Locale");
		localeNode.SetAttribute ("id", langCode);
		localizerNode.AppendChild (localeNode);

		// Add other strings into the manifest to use with the StringTable localizer.
		foreach (XmlElement dataNode in resourceDoc.SelectNodes ("/root/data")!) {
			XmlNode valueNode = dataNode.SelectSingleNode ("value")
				?? throw new InvalidDataException ("Failed to find 'value' child node");

			XmlElement msgNode = manifestDoc.CreateElement ("Msg");
			msgNode.SetAttribute ("id", dataNode.GetAttribute ("name"));
			msgNode.SetAttribute ("str", valueNode.InnerText);
			localeNode.AppendChild (msgNode);

			Console.WriteLine ($" - Adding translation for '{msgNode.GetAttribute ("id")}': '{msgNode.GetAttribute ("str")}'");
		}
	}

	Console.WriteLine ($"Updating {manifestFile}");
	manifestDoc.Save (manifestFile.FullName);
}

var manifestFileOption =
	new Option<FileInfo> (name: "--manifest-file") { Required = true }
	.AcceptExistingOnly ();

var resourceFilesOption =
	new Option<FileInfo[]> (name: "--resource-files") {
		Required = true,
		AllowMultipleArgumentsPerToken = true
	}
	.AcceptExistingOnly ();

Command localizeManifestCommand = new (
	name: "localize-manifest",
	description: "Copy translations from resource files into the add-in manifest")
{
	manifestFileOption,
	resourceFilesOption,
};
localizeManifestCommand.SetAction (result => {
	LocalizeManifest (
		result.GetRequiredValue (manifestFileOption),
		result.GetRequiredValue (resourceFilesOption));
});

RootCommand rootCommand = new ("Command-line utilities for Pinta add-ins.");
rootCommand.Subcommands.Add (localizeManifestCommand);

return rootCommand.Parse (args).Invoke ();
