using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class TextEngineTest
{
	// The string below contains combining characters, so there are fewer text elements than chars.
	private readonly List<string> testSnippet = new () {
		"a\u0304\u0308bc\u0327",
		"c\u0327ba\u0304\u0308",
		"bc\u0327a\u0304\u0308"
	};

	private static string LinesToString (string[] lines) => string.Join (Environment.NewLine, lines);

	[OneTimeSetUp]
	public void Init ()
	{
		Pango.Module.Initialize ();
	}

	[Test]
	public void PerformEnter ()
	{
		var engine = new TextEngine (new List<string> () { "foo", "bar" });
		engine.SetCursorPosition (new TextPosition (1, 1), true);
		engine.PerformEnter ();

		Assert.That (engine.LineCount, Is.EqualTo (3));
		Assert.That (engine.ToString (), Is.EqualTo (LinesToString (new string[] { "foo", "b", "ar" })));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (2, 0)));
	}

	[Test]
	public void DeleteMultiLineSelection ()
	{
		var engine = new TextEngine (new List<string> () { "line 1", "line 2", "line 3" });
		engine.SetCursorPosition (new TextPosition (0, 2), true);
		engine.PerformDown (true);
		engine.PerformDown (true);
		engine.PerformDelete ();

		Assert.That (engine.LineCount, Is.EqualTo (1));
		Assert.That (engine.ToString (), Is.EqualTo (LinesToString (new string[] { "line 3" })));
	}

	[Test]
	public void DeleteSelection ()
	{
		var engine = new TextEngine (new List<string> () { "это тест", "это еще один тест" });
		engine.SetCursorPosition (new TextPosition (0, 2), true);
		engine.PerformDown (true);
		engine.PerformDelete ();

		Assert.That (engine.LineCount, Is.EqualTo (1));
		Assert.That (engine.ToString (), Is.EqualTo (LinesToString (new string[] { "это еще один тест" })));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 2)));
	}

	[Test]
	public void BackspaceJoinLines ()
	{
		var engine = new TextEngine (new[] { "foo", "bar" });
		engine.SetCursorPosition (new TextPosition (1, 0), true);
		engine.PerformBackspace ();

		Assert.That (engine.LineCount, Is.EqualTo (1));
		Assert.That (engine.ToString (), Is.EqualTo ("foobar"));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 3)));
	}

	[Test]
	public void Backspace ()
	{
		var engine = new TextEngine (testSnippet);

		// End of a line.
		engine.SetCursorPosition (new TextPosition (0, 6), true);
		engine.PerformBackspace ();

		Assert.That (engine.Lines[0], Is.EqualTo ("a\u0304\u0308b"));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 4)));

		// First character of a line.
		engine.SetCursorPosition (new TextPosition (1, 2), true);
		engine.PerformBackspace ();

		Assert.That (engine.Lines[1], Is.EqualTo ("ba\u0304\u0308"));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (1, 0)));

		// Middle of a line.
		engine.SetCursorPosition (new TextPosition (2, 3), true);
		engine.PerformBackspace ();

		Assert.That (engine.Lines[2], Is.EqualTo ("ba\u0304\u0308"));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (2, 1)));
	}

	[Test]
	public void DeleteJoinLines ()
	{
		var engine = new TextEngine (new[] { "foo", "bar" });
		engine.SetCursorPosition (new TextPosition (0, 3), true);
		engine.PerformDelete ();

		Assert.That (engine.LineCount, Is.EqualTo (1));
		Assert.That (engine.ToString (), Is.EqualTo ("foobar"));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 3)));

		// Nothing happens when deleting at the end of the last line.
		engine.SetCursorPosition (new TextPosition (0, 6), true);
		engine.PerformDelete ();

		Assert.That (engine.LineCount, Is.EqualTo (1));
		Assert.That (engine.ToString (), Is.EqualTo ("foobar"));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 6)));
	}

	[Test]
	public void Delete ()
	{
		var engine = new TextEngine (testSnippet);

		// Beginning of a line.
		engine.SetCursorPosition (new TextPosition (0, 0), true);
		engine.PerformDelete ();

		Assert.That (engine.Lines[0], Is.EqualTo ("bc\u0327"));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 0)));

		// Middle of a line.
		engine.SetCursorPosition (new TextPosition (2, 1), true);
		engine.PerformDelete ();

		Assert.That (engine.Lines[2], Is.EqualTo ("ba\u0304\u0308"));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (2, 1)));

		// End of a line.
		engine.SetCursorPosition (new TextPosition (1, 3), true);
		engine.PerformDelete ();

		Assert.That (engine.Lines[1], Is.EqualTo ("c\u0327b"));
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (1, 3)));
	}

	[Test]
	public void PerformLeftRight ()
	{
		var engine = new TextEngine (testSnippet);

		engine.SetCursorPosition (new TextPosition (0, 3), true);
		engine.PerformRight (false, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 4)));
		engine.PerformRight (false, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 6)));
		engine.PerformRight (false, false);
		engine.PerformRight (false, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (1, 2)));

		engine.PerformLeft (false, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (1, 0)));
		engine.PerformLeft (false, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 6)));

		// Should stay at the beginning / end when attempting to advance further.
		engine.SetCursorPosition (new TextPosition (0, 0), true);
		engine.PerformLeft (false, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 0)));

		var endPosition = new TextPosition (testSnippet.Count - 1, testSnippet.Last ().Length);
		engine.SetCursorPosition (endPosition, true);
		engine.PerformRight (false, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (endPosition));
	}

	[Test]
	public void PerformControlLeftRight ()
	{
		var engine = new TextEngine (new[] { string.Join ("  ", testSnippet) });

		engine.SetCursorPosition (new TextPosition (0, 0), true);
		engine.PerformRight (true, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 8)));
		engine.SetCursorPosition (new TextPosition (0, 7), true);
		engine.PerformRight (true, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 8)));
		engine.PerformRight (true, false);
		engine.PerformRight (true, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 22)));

		engine.PerformLeft (true, false);
		engine.PerformLeft (true, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 8)));
		engine.PerformLeft (true, false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 0)));
	}

	[Test]
	public void PerformUpDown ()
	{
		var engine = new TextEngine (testSnippet);

		engine.SetCursorPosition (new TextPosition (1, 2), true);
		engine.PerformUp (false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (0, 3)));

		engine.PerformDown (false);
		Assert.That (engine.CurrentPosition, Is.EqualTo (new TextPosition (1, 2)));
	}
}
