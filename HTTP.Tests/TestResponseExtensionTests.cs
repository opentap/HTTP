using System.Collections.Generic;
using OpenTap;
using OpenTap.Diagnostic;
using OpenTap.UnitTest;

namespace Tests;

public class TestResponseExtensionTests : ITestFixture
{
    [Test]
    public void MultiLineConsoleLog()
    {
        MyListener lis = new MyListener();
        Log.AddListener(lis);
        HTTP.Console console = new HTTP.Console();
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