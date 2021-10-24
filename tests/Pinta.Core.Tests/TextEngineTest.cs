using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pinta.Core;

namespace Pinta.Core.Tests
{
	[TestFixture]
	class TextEngineTest
	{
		// The string below contains combining characters, so there are fewer text elements than chars.
		private readonly List<string> testSnippet = new (){
			"a\u0304\u0308bc\u0327",
			"c\u0327ba\u0304\u0308",
			"bc\u0327a\u0304\u0308"
		};

		private string LinesToString (string[] lines) => string.Join (Environment.NewLine, lines);

		[Test]
		public void PerformEnter ()
		{
			var engine = new TextEngine (new List<string> () { "foo", "bar" });
			engine.SetCursorPosition (new TextPosition (1, 1), true);
			engine.PerformEnter ();

			Assert.AreEqual (3, engine.LineCount);
			Assert.AreEqual (LinesToString (new string[] { "foo", "b", "ar" }),
					 engine.ToString ());
			Assert.AreEqual (new TextPosition (2, 0), engine.CurrentPosition);
		}

		[Test]
		public void DeleteMultiLineSelection ()
		{
			var engine = new TextEngine (new List<string> () { "line 1", "line 2", "line 3" });
			engine.SetCursorPosition (new TextPosition (0, 2), true);
			engine.PerformDown (true);
			engine.PerformDown (true);
			engine.PerformDelete ();

			Assert.AreEqual (1, engine.LineCount);
			Assert.AreEqual (LinesToString (new string[] { "line 3" }),
					 engine.ToString ());
		}

		[Test]
		public void DeleteSelection ()
		{
			var engine = new TextEngine (new List<string> () { "это тест", "это еще один тест" });
			engine.SetCursorPosition (new TextPosition (0, 2), true);
			engine.PerformDown (true);
			engine.PerformDelete ();

			Assert.AreEqual (1, engine.LineCount);
			Assert.AreEqual (LinesToString (new string[] { "это еще один тест" }),
					 engine.ToString ());
			Assert.AreEqual (new TextPosition (0, 2), engine.CurrentPosition);
		}

		[Test]
		public void BackspaceJoinLines ()
		{
			var engine = new TextEngine (new () { "foo", "bar" });
			engine.SetCursorPosition (new TextPosition (1, 0), true);
			engine.PerformBackspace ();

			Assert.AreEqual (1, engine.LineCount);
			Assert.AreEqual ("foobar", engine.ToString ());
			Assert.AreEqual (new TextPosition (0, 3), engine.CurrentPosition);
		}

		[Test]
		public void Backspace ()
		{
			var engine = new TextEngine (testSnippet);

			// End of a line.
			engine.SetCursorPosition (new TextPosition (0, 6), true);
			engine.PerformBackspace ();

			Assert.AreEqual ("a\u0304\u0308b", engine.Lines[0]);
			Assert.AreEqual (new TextPosition (0, 4), engine.CurrentPosition);

			// First character of a line.
			engine.SetCursorPosition (new TextPosition (1, 2), true);
			engine.PerformBackspace ();

			Assert.AreEqual ("ba\u0304\u0308", engine.Lines[1]);
			Assert.AreEqual (new TextPosition (1, 0), engine.CurrentPosition);

			// Middle of a line.
			engine.SetCursorPosition (new TextPosition (2, 3), true);
			engine.PerformBackspace ();

			Assert.AreEqual ("ba\u0304\u0308", engine.Lines[2]);
			Assert.AreEqual (new TextPosition (2, 1), engine.CurrentPosition);
		}

		[Test]
		public void DeleteJoinLines ()
		{
			var engine = new TextEngine (new () { "foo", "bar" });
			engine.SetCursorPosition (new TextPosition (0, 3), true);
			engine.PerformDelete ();

			Assert.AreEqual (1, engine.LineCount);
			Assert.AreEqual ("foobar", engine.ToString ());
			Assert.AreEqual (new TextPosition (0, 3), engine.CurrentPosition);

			// Nothing happens when deleting at the end of the last line.
			engine.SetCursorPosition (new TextPosition (0, 6), true);
			engine.PerformDelete ();

			Assert.AreEqual (1, engine.LineCount);
			Assert.AreEqual ("foobar", engine.ToString ());
			Assert.AreEqual (new TextPosition (0, 6), engine.CurrentPosition);
		}

		[Test]
		public void Delete ()
		{
			var engine = new TextEngine (testSnippet);

			// Beginning of a line.
			engine.SetCursorPosition (new TextPosition (0, 0), true);
			engine.PerformDelete ();

			Assert.AreEqual ("bc\u0327", engine.Lines[0]);
			Assert.AreEqual (new TextPosition (0, 0), engine.CurrentPosition);

			// Middle of a line.
			engine.SetCursorPosition (new TextPosition (2, 1), true);
			engine.PerformDelete ();

			Assert.AreEqual ("ba\u0304\u0308", engine.Lines[2]);
			Assert.AreEqual (new TextPosition (2, 1), engine.CurrentPosition);

			// End of a line.
			engine.SetCursorPosition (new TextPosition (1, 3), true);
			engine.PerformDelete ();

			Assert.AreEqual ("c\u0327b", engine.Lines[1]);
			Assert.AreEqual (new TextPosition (1, 3), engine.CurrentPosition);
		}

		[Test]
		public void PerformLeftRight ()
		{
			var engine = new TextEngine (testSnippet);

			engine.SetCursorPosition (new TextPosition (0, 3), true);
			engine.PerformRight (false, false);
			Assert.AreEqual (new TextPosition (0, 4), engine.CurrentPosition);
			engine.PerformRight (false, false);
			Assert.AreEqual (new TextPosition (0, 6), engine.CurrentPosition);
			engine.PerformRight (false, false);
			engine.PerformRight (false, false);
			Assert.AreEqual (new TextPosition (1, 2), engine.CurrentPosition);

			engine.PerformLeft (false, false);
			Assert.AreEqual (new TextPosition (1, 0), engine.CurrentPosition);
			engine.PerformLeft (false, false);
			Assert.AreEqual (new TextPosition (0, 6), engine.CurrentPosition);

			// Should stay at the beginning / end when attempting to advance further.
			engine.SetCursorPosition (new TextPosition (0, 0), true);
			engine.PerformLeft (false, false);
			Assert.AreEqual (new TextPosition (0, 0), engine.CurrentPosition);

			var endPosition = new TextPosition (testSnippet.Count - 1, testSnippet.Last ().Length);
			engine.SetCursorPosition (endPosition, true);
			engine.PerformRight (false, false);
			Assert.AreEqual (endPosition, engine.CurrentPosition);
		}

		[Test]
		public void PerformControlLeftRight ()
		{
			var engine = new TextEngine (new () { string.Join ("  ", testSnippet) });

			engine.SetCursorPosition (new TextPosition (0, 0), true);
			engine.PerformRight (true, false);
			Assert.AreEqual (new TextPosition (0, 8), engine.CurrentPosition);
			engine.SetCursorPosition (new TextPosition (0, 7), true);
			engine.PerformRight (true, false);
			Assert.AreEqual (new TextPosition (0, 8), engine.CurrentPosition);
			engine.PerformRight (true, false);
			engine.PerformRight (true, false);
			Assert.AreEqual (new TextPosition (0, 22), engine.CurrentPosition);

			engine.PerformLeft (true, false);
			engine.PerformLeft (true, false);
			Assert.AreEqual (new TextPosition (0, 8), engine.CurrentPosition);
			engine.PerformLeft (true, false);
			Assert.AreEqual (new TextPosition (0, 0), engine.CurrentPosition);
		}

		[Test]
		public void PerformUpDown ()
		{
			var engine = new TextEngine (testSnippet);

			engine.SetCursorPosition (new TextPosition (1, 2), true);
			engine.PerformUp (false);
			Assert.AreEqual (new TextPosition (0, 3), engine.CurrentPosition);

			engine.PerformDown (false);
			Assert.AreEqual (new TextPosition (1, 2), engine.CurrentPosition);
		}
	}
}
