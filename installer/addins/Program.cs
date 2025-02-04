using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Xml;

// Insert translations from the resource files into the manifest (.addin.xml)
// See https://github.com/mono/mono-addins/wiki/The-add-in-header
static void LocalizeManifest (FileInfo manifestFile, FileInfo[] resourceFiles)
{
	Console.WriteLine ($"Loading manifest from {manifestFile}");
	var manifestDoc = new XmlDocument () { PreserveWhitespace = true };
	manifestDoc.Load (manifestFile.FullName);

	// The properties to translate.
	string[] headerProperties = ["Name", "Description"];
	var headerPropertyNodes = headerProperties.Select (propertyName =>
	    manifestDoc.SelectSingleNode ($"/Addin/Header/{propertyName}") ??
	    throw new InvalidDataException ($"Add-in manifest does not specify header property '{propertyName}'"));

	foreach (var resourceFile in resourceFiles) {
		// Parse the locale name from filenames like Language.es.resx.
		// We don't need to process the template file (Language.resx).
		var components = resourceFile.Name.Split ('.');
		if (components.Length != 3) {
			Console.WriteLine ($"Skipping file {resourceFile}");
			continue;
		}

		string langCode = components[1];

		Console.WriteLine ($"{langCode}: Loading resource {resourceFile}");
		var resourceDoc = new XmlDocument ();
		resourceDoc.Load (resourceFile.FullName);

		foreach (XmlNode headerPropertyNode in headerPropertyNodes) {
			string propertyName = headerPropertyNode.Name;
			var translationNode = resourceDoc.SelectSingleNode ($"/root/data[@name='{headerPropertyNode.InnerText}']/value");
			if (translationNode is not null) {
				Console.WriteLine ($" - Adding translation for {propertyName}: {translationNode.InnerText}");

				// Add a sibling node, e.g. <Name locale="es">Translated string</Name>
				var newNode = manifestDoc.CreateElement (propertyName);
				newNode.SetAttribute ("locale", langCode);
				newNode.InnerText = translationNode.InnerText;

				headerPropertyNode.ParentNode!.AppendChild (newNode);
			} else
				Console.WriteLine ($" - Did not find translation for {propertyName}");
		}
	}

	Console.WriteLine ($"Updating {manifestFile}");
	manifestDoc.Save (manifestFile.FullName);
}

var manifestFileOption =
	new Option<FileInfo> (name: "--manifest-file") { IsRequired = true }
	.ExistingOnly ();

var resourceFilesOption =
	new Option<FileInfo[]> (name: "--resource-files") {
		IsRequired = true,
		AllowMultipleArgumentsPerToken = true,
	}
	.ExistingOnly ();

Command localizeManifestCommand = new (
	name: "localize-manifest",
	description: "Copy translations from resource files into the add-in manifest")
{
	manifestFileOption,
	resourceFilesOption,
};
localizeManifestCommand.SetHandler (LocalizeManifest, manifestFileOption, resourceFilesOption);

RootCommand rootCommand = new ("Command-line utilities for Pinta add-ins.");
rootCommand.AddCommand (localizeManifestCommand);
rootCommand.Invoke (args);
