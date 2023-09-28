using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Xml;
using HTTP.TestResponseExtension;
using Jint;
using Jint.Native;
using Newtonsoft.Json;
using OpenTap;

namespace HTTP;

internal class JavaScriptRunner
{
    static readonly  TraceSource log = Log.CreateSource("Web API Tester");
    static readonly TraceSource javascriptLog = Log.CreateSource("js");
    private const string JavascriptModulesDirectory = "Packages/HTTP/Packages";

    private static List<ITestResponseExtension> TestResponseExtensions { get; set; } = new List<ITestResponseExtension>();

    static JavaScriptRunner()
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

    public JavaScriptRunner(Request request)
    {
        Request = request;
    }

    public Request Request { get; set; }
    public Response response { get; set; }


    internal static JavaScriptRunner Generate(HttpRequestMessage request)
    {
        return new JavaScriptRunner(new Request(request));
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

    IEnumerable<(string, object, IMemberData)> GetDynamicSettings(object target)
    {
        foreach (var mem in TypeData.GetTypeData(target).GetMembers().OfType<MixinMemberData>())
        {
            yield return (mem.GetDisplayAttribute().Name, mem.GetValue(target), mem);
        }
    }
    class AssertionFailedException : Exception
    {
        
    }

    internal void RunTests(string responseTests, object target)
    {
        void alert (string message)
        {
            UserInput.Request(new Alert(message), false);
        };
        void assert (bool condition)
        {
            if (!condition)
                throw new AssertionFailedException();
        };

        var engine = new Engine()
            .SetValue("tap", this)
            .SetValue("assert", assert)
            .SetValue("alert", alert);
        foreach (var value in GetDynamicSettings(target))
        {
            engine.SetValue(value.Item1, value.Item2);
        }

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
        
        foreach (var setting in GetDynamicSettings(target))
        {
            JsValue value2 = engine.GetValue(setting.Item1);
            var value3 = JsObjectToNet(value2);
            IConvertible conv = null;
            var member = setting.Item3;
            var type = member.TypeDescriptor;
            if (type.DescendsTo(typeof(string)))
            {
                if (value3 is string str)
                {
                    member.SetValue(target, str);
                }
                if (value3 is double d)
                {
                    member.SetValue(target, d.ToString());
                }
            }
            if (type.DescendsTo(typeof(double)))
            {
                if (value3 is string str)
                {
                    member.SetValue(target, double.Parse(str));
                }
                if (value3 is double d)
                {
                    member.SetValue(target, d);
                }
            }
            // otherwise unsupported value.
        }
    }

    static object JsObjectToNet(JsValue value)
    {
        if (value.IsString())
        {
            return value.AsString();
        }
        if (value.IsNumber())
        {
            return value.AsNumber();
        }
        if (value.IsNull())
        {
            return null;
        }
        return value.ToString();

    }

    internal void SetResponse(HttpResponseMessage response)
    {
        this.response = new Response(response);
    }
}