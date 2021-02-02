using NUnit.Framework;
using OpenTap;
using OpenTap.Diagnostic;
using System;
using System.Collections.Generic;
using System.Text;
using Assert = NUnit.Framework.Assert;

namespace Tests
{
    public class TestResponseExtensionTests
    {
        [Test]
        public void MultiLineConsoleLog()
        {
            MyListener lis = new MyListener();
            Log.AddListener(lis);
            WebApiTester.Console console = new WebApiTester.Console();
            string testString = @"Testing this
awesome
multiline
string";
            console.log(testString);
            Log.Flush();
            Assert.AreEqual(4, lis.Messages.Count);
        }
    }

    public class MyListener : TraceListener
    {
        public List<string> Messages = new List<string>();
        public override void TraceEvents(IEnumerable<Event> events)
        {
            foreach(var ev in events)
            {
                Messages.Add(ev.Message);
            }
        }
    }
}
