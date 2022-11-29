using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using OpenTap;
using OpenTap.Cli;

namespace HTTP;

[Display("convert", Group: "webapi")]
public class ApiTestCliAction : ICliAction
{
    [UnnamedCommandLineArgument("PostmanCollection")]
    public string PostManFile { get; set; }
    public int Errors { get; private set; }

    OpenTap.TraceSource log = Log.CreateSource("Web API Tester");

    public int Execute(CancellationToken cancellationToken)
    {
        Stopwatch watch = Stopwatch.StartNew();
        if (!File.Exists(PostManFile))
            throw new ArgumentException("Invalid path to postman test collection json file");

        var json = File.ReadAllText(PostManFile);

        JToken root = JToken.Parse(json);
        TestPlan plan = new TestPlan();

        RestApiEnvironment testSession = new RestApiEnvironment() { Name = Path.GetFileNameWithoutExtension(PostManFile) };
        plan.ChildTestSteps.Add(testSession);

        Errors = 0;

        if (root["item"] is JArray jAr)
        {
            foreach(JObject jObj in jAr)
            {
                testSession.ChildTestSteps.Add(ParseObject(jObj));
            }
        }

        plan.Save($"{PostManFile}.TestPlan");

        int stepCount = plan.ChildTestSteps.RecursivelyGetAllTestSteps(TestStepSearch.All).Count();

        log.Info(watch, $"Converted {stepCount} test steps in {watch.ElapsedMilliseconds} ms");
        log.Info(watch, $"Completed with {Errors} errors");
        log.Info($"Saved to \"{PostManFile}.TestPlan\"");

        return 0;
    }

    private TestStep ParseObject(JObject jObject)
    {
        if (jObject.ContainsKey("item") && jObject.ContainsKey("name"))
        {
            RestApiEnvironment testSession = new RestApiEnvironment();
            foreach (var item in jObject["item"].Values<JObject>())
            {
                testSession.ChildTestSteps.Add(ParseObject(item));
            }
            testSession.Name = jObject["name"].Value<string>();
            return testSession;
        }
        else
        {
            RequestStep requestStep = new RequestStep();
            if (jObject.ContainsKey("name"))
                requestStep.Name = jObject["name"].Value<string>();

            if(jObject.ContainsKey("event") && jObject["event"] is JArray jArray)
            {
                foreach(var arrayEntry in jArray)
                {
                    if(arrayEntry is JObject jObjArrayEntry)
                    {
                        if(jObjArrayEntry.ContainsKey("listen") && jObjArrayEntry["listen"].Value<string>() == "test")
                        {
                            if(jObjArrayEntry.ContainsKey("script") && jObjArrayEntry["script"] is JObject jScript)
                            {
                                if (jScript["exec"] is JArray jScriptLines)
                                {

                                    List<string> script = new List<string>();
                                    foreach(var line in jScriptLines)
                                    {
                                        script.Add(line.Value<string>());
                                    }
                                    requestStep.ResponseTests = ConvertScript(script);

                                    if(requestStep.ResponseTests.Contains("postman"))
                                        AddError($"Step '{requestStep.Name}' script may not be fully converted. Contains 'postman'");

                                    if (requestStep.ResponseTests.Contains("pm"))
                                        AddError($"Step '{requestStep.Name}' script may not be fully converted. Contains 'pm'");

                                }
                            }
                        } else if (jObjArrayEntry.ContainsKey("listen") && jObjArrayEntry["listen"].Value<string>() == "prerequest")
                        {
                            AddError($"Step '{requestStep.Name}' contains a prescript");
                        }
                    }
                }
            }

            if (jObject.ContainsKey("request") && jObject["request"] is JObject jRequest)
            {
                string method = jRequest["method"].Value<string>();
                string path = string.Join("/", jRequest["url"]["path"].Values<string>());

                requestStep.Endpoint = "/" + path;

                if(jRequest["url"] is JObject jUrl && jUrl.ContainsKey("query"))
                {
                    if(jUrl["query"] is JArray jQueryArray)
                    {
                        foreach (var query in jQueryArray)
                        {
                            requestStep.Endpoint += $"?{query["key"]}={query["value"]}";
                        }
                    }
                }

                switch (method)
                {
                    case "POST":
                        requestStep.Method = HttpMethod.POST;
                        break;
                    case "GET":
                        requestStep.Method = HttpMethod.GET;
                        break;
                    case "DELETE":
                        requestStep.Method = HttpMethod.DELETE;
                        break;
                }
                if (jRequest.ContainsKey("header") && jRequest["header"] is JArray jHeaders)
                {
                    foreach(var header in jHeaders)
                    {
                        if(header is JObject jHeader)
                        {
                            var curheader = new Header();
                            curheader.Key = header["key"].Value<string>();
                            curheader.Value = header["value"].Value<string>();
                            requestStep.Headers.Add(curheader);
                        }
                    }
                }

                if(jRequest.ContainsKey("body") && jRequest["body"] is JObject jBody)
                {
                    try { 
                        requestStep.Body = jBody["raw"].Value<string>();
                    }
                    catch (Exception ex)
                    {
                        AddError($"Step '{requestStep.Name}' script may not be fully converted. {ex.Message}");
                    }
                }

            }

            return requestStep;
        }
    }

    private void AddError(string v)
    {
        log.Error(v);
        Errors++;
    }

    Dictionary<string, string> conversionTable = new Dictionary<string, string>()
    {
        { "pm.environment.set", "tap.SetEnvironmentVariable" },
        { "pm.response.to.have.status(", "assert.Equals(tap.Response.StatusCode, "},
        { "responseBody", "tap.Response.Body" },
        { "pm.response.json()", "JSON.parse(tap.Response.Body)" },
        { "pm.expect", "assert.Equals" },
        { ").to.eql(", ", " },
        { "pm.environment.unset", "tap.RemoveEnvironmentVariable" },
        { "tests[\"Status code is 200\"] = responseCode.code === 200",  "assert.Equals(tap.Response.StatusCode, 200)"},
        { "pm.response.text()", "JSON.parse(tap.Response.Body)" }
    };
    internal string ConvertScript(List<string> script)
    {
        try
        {
            return PostmanTestCollectionConverter.Convert(script);
        }
        catch (Exception)
        {

        }

        //return script;

        for (int i = 0; i < script.Count; i++)
        {
            script[i] = convertLine(script[i].Trim());
        }

        script = script.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

        return string.Join(Environment.NewLine, script);
    }

    private string convertLine(string v)
    {
        string converted = v;
        if (converted.Contains("pm.test") || converted.Equals("});"))
            converted = string.Empty;

        foreach(var key in conversionTable.Keys)
        {
            if (converted.Contains(key))
                converted = converted.Replace(key, conversionTable[key]);
        }

        return converted;
    }
}