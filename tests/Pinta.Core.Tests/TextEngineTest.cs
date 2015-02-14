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
        private string LinesToString(string[] lines)
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
            Assert.AreEqual (LinesToString (new string[] {"foo", "b", "ar" }),
                             engine.ToString());
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
        public void PerformLeft ()
        {
            var engine = new TextEngine (new List<string> () { "foo", "bar" });

            engine.SetCursorPosition (new TextPosition (1, 0), true);
            engine.PerformLeft (false, false);
            Assert.AreEqual (new TextPosition (0, 3), engine.CurrentPosition);

            engine.SetCursorPosition (new TextPosition (0, 1), true);
            engine.PerformLeft (false, false);
            Assert.AreEqual (new TextPosition (0, 0), engine.CurrentPosition);

            engine.PerformLeft (false, false);
            Assert.AreEqual (new TextPosition (0, 0), engine.CurrentPosition);
        }
    }
}
