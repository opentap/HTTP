using System.Collections.Generic;
using NUnit.Framework;
using OpenTap;
using OpenTap.Diagnostic;

namespace Tests;

public class TestResponseExtensionTests
{
    [Test]
    public void MultiLineConsoleLog()
    {
        MyListener lis = new MyListener();
        Log.AddListener(lis);
        HttpSteps.Console console = new HttpSteps.Console();
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