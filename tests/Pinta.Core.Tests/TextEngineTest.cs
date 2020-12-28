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
		private string LinesToString (string[] lines)
		{
			var str = new StringBuilder ();
			foreach (string s in lines)
				str.AppendLine (s);
			return str.ToString ();
		}

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
		public void PerformLeftRight ()
		{
			// The string below contains combining characters, so there are fewer text elements than chars.
			var engine = new TextEngine (new List<string> () { "a\u0304\u0308bc\u0327", "c\u0327ba\u0304\u0308" });

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

			engine.SetCursorPosition (new TextPosition (1, 6), true);
			engine.PerformRight (false, false);
			Assert.AreEqual (new TextPosition (1, 6), engine.CurrentPosition);
		}
	}
}
