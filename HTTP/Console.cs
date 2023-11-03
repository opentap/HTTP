using System;
using System.Dynamic;
using HTTP.TestResponseExtension;
using Newtonsoft.Json;
using OpenTap;
namespace HTTP;

[Display("console")]
public class Console : ITestResponseExtension
{
    TraceSource ConsoleLog = Log.CreateSource("Javascript");

    public void log(object message)
    {
        if (message is string str)
            foreach(var msg in str.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                ConsoleLog.Info(msg);
        else if(message is ExpandoObject expando)
        {
            string json = JsonConvert.SerializeObject(expando);
            ConsoleLog.Info(json);
        }
        else
            ConsoleLog.Info(message.ToString());
    }
}