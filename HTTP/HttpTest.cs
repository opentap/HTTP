using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Xml;
using HTTP.TestResponseExtension;
using Jint;
using Newtonsoft.Json;
using OpenTap;

namespace HTTP;

internal class HttpTest
{
    private VariableHandler EnvironmentVariables { get; set; }
    private VariableHandler GlobalVariables { get; set; }
    static TraceSource log = Log.CreateSource("Web API Tester");
    TraceSource javascriptLog = Log.CreateSource("Javascript");
    private const string JavascriptModulesDirectory = "Packages/HTTP/Packages";
    public Assert Tests { get; set; } = null;

    private static List<ITestResponseExtension> TestResponseExtensions { get; set; } = new List<ITestResponseExtension>();

    static HttpTest()
    {
        foreach (var p in PluginManager.GetPlugins<ITestResponseExtension>())
        {
            try
            {
                TestResponseExtensions.Add(Activator.CreateInstance(p) as ITestResponseExtension);
            }
            catch (Exception ex)
            {
                log.Error("Could not load Test Response plugins.");
                log.Error(ex);
            }
        }
    }

    public HttpTest(Request request, VariableHandler environmentVariables, VariableHandler globalVariables)
    {
        Request = request;
        EnvironmentVariables = environmentVariables;
        GlobalVariables = globalVariables;
    }

    public Request Request { get; set; }
    public Response response { get; set; }


    internal static HttpTest Generate(HttpRequestMessage request, VariableHandler environmentVariables, VariableHandler globalVariables)
    {
        return new HttpTest(new Request(request), environmentVariables, globalVariables);
    }

    public class Alert
    {
        public Alert(object message)
        {
            this.Message = message;
        }
        [Browsable(true)]
        public object Message { get; private set; }
    }

    public string parseXmlToJson(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return JsonConvert.SerializeXmlNode(doc);
    }

    internal void RunTests(string responseTests)
    {
        Assert assert = new Assert();
        if (string.IsNullOrWhiteSpace(responseTests))
            Tests = assert;
        // https://github.com/sebastienros/jint

        Action<object> alert = (message) =>
        {
            UserInput.Request(new Alert(message), false);
        };

        var engine = new Engine()
            .SetValue("tap", this)
            .SetValue("assert", assert)
            .SetValue("alert", alert);

        // Load Test Response plugins
        foreach (var p in TestResponseExtensions)
        {
            var display = p.GetType().GetCustomAttribute<DisplayAttribute>();
            engine.SetValue(display != null ? display.Name : p.GetType().Name, p);
        }

        if (Directory.Exists(JavascriptModulesDirectory))
        {
            var files = Directory.GetFiles(JavascriptModulesDirectory, "*.js", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var js = File.ReadAllText(file);
                    engine.Execute(js);
                    javascriptLog.Debug($"Found and loaded .js file: '{file}'.");
                }
                catch(Exception ex)
                {
                    javascriptLog.Warning($"Failed to load .js file: '{file}'.");
                    javascriptLog.Debug(ex);
                }
            }
        }

        engine.Execute(responseTests);
        Tests =  assert;
    }


    public void setEnvironmentVariable(string key, string value)
    {
        EnvironmentVariables[key] = value;
    }
    public string getEnvironmentVariable(string key)
    {
        if (!EnvironmentVariables.ContainsKey(key))
            return "";

        return EnvironmentVariables[key];
    }
    public void removeEnvironmentVariable(string key)
    {
        if (!EnvironmentVariables.ContainsKey(key))
            return;

        EnvironmentVariables.Remove(key);
    }

    public void setGlobalVariable(string key, string value)
    {
        GlobalVariables[key] = value;
    }
    public string getGlobalVariable(string key)
    {
        if (!GlobalVariables.ContainsKey(key))
            return "";

        return GlobalVariables[key];
    }
    public void removeGlobalVariable(string key)
    {
        if (!GlobalVariables.ContainsKey(key))
            return;

        GlobalVariables.Remove(key);
    }

    internal void SetResponse(HttpResponseMessage response)
    {
        this.response = new Response(response);
    }
}

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

public class Response
{
    public byte[] arrayBuffer { get; private set; }
    public string body { get; private set; }
    public int statusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    public Response(HttpResponseMessage response)
    {
        body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        arrayBuffer = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        statusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
            Headers.Add(header.Key, header.Value.FirstOrDefault());
        
        foreach (var header in response.Content.Headers)
            Headers.Add(header.Key, header.Value.FirstOrDefault());
    }

    public Response()
    {
    }
}

public class Request
{
    public string Body { get; private set; } = "";
    public string Method { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string Path { get; set; } = "";
    public Request()
    {
    }

    public Request(HttpRequestMessage request)
    {
        if (request.Content is object)
            Body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Method = request.Method.ToString();
        foreach (var header in request.Headers)
            Headers.Add(header.Key, header.Value.FirstOrDefault());

        Path = request.RequestUri.PathAndQuery;
    }
}