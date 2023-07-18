using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sb1;

namespace tests
{
    [TestClass]
    public class MultiSelectorTester
    {
        [TestMethod]
        public void MultiSelectorTestSimplest()
        {
            var items = Enumerable.Range(0,9).ToList();
            var m = new MultiSelector<int>(items, 9);
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.WriteLine(m.GetSubdivisions());

            var result = m.Select(5);
            Assert.IsFalse(result);
            var i = m.GetCurrentSubdivision();
            Assert.AreEqual(9, i.Count());
            Assert.AreEqual(0, i.First());
        }

        [TestMethod]
        public void MultiSelectorTestDouble()
        {
            var items = Enumerable.Range(0, 1000).ToList();
            var m = new MultiSelector<int>(items, 9);

            m.Select(4);
            var i = m.GetCurrentSubdivision();
            Assert.AreEqual(112, i.Count());
            Assert.AreEqual(448, i.First());

            m.Select(7);
            i = m.GetCurrentSubdivision();
            Assert.AreEqual(13, i.Count());
            Assert.AreEqual(539, i.First());

            m.Select(2);
            i = m.GetCurrentSubdivision();
            Assert.AreEqual(2, i.Count());
            Assert.AreEqual(543, i.First());
        }
    }
}
