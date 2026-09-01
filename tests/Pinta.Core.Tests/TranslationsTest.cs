using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
[NonParallelizable] // Run in isolation since this test modifies the environment.
internal sealed class TranslationsTest
{
	[OneTimeSetUp]
	public void Setup ()
	{
		// Set to some language other than English to test translations.
		GLib.Functions.Setenv ("LANGUAGE", "fr_FR", true);

		// The test runs from a path like Pinta/tests/Pinta.Core.Tests/bin/Debug/net10.0,
		// so we need the relative path to the translation folder (Pinta/build/bin/locale).
		string localeDir = "../../../../../build/bin/locale";
		Translations.Init (localeDir);
	}

	[OneTimeTearDown]
	public void TearDown ()
	{
		GLib.Functions.Unsetenv ("LANGUAGE");
	}

	[Test]
	[Description ("Test that a string can be translated. If this test fails, make sure you built with -p:BuildTranslations=true.")]
	public void SimpleTranslation ()
	{
		Assert.That (Translations.GetString ("Color"), Is.EqualTo ("Couleur"));
	}
}
