using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTap;

namespace HTTP;

[Display("Request", Group: "HTTP")]
public class RequestStep : TestStep
{
    internal static HttpClient HttpClient = new HttpClient() { Timeout = Timeout.InfiniteTimeSpan };
    [Display("Endpoint", Description: "Endpoint to send request against", Group: "Request", Order: 10)]
    public string Endpoint { get; set; } = "/api/values";
    [Display("Http Method", Description: "Http request method, e.g. GET or POST", Group: "Request", Order: 15)]
    public HttpMethod Method { get; set; } = HttpMethod.GET;
    [Display("Headers", Description: "Attach headers to the request", Group: "Request", Order: 20)]
    public List<Header> Headers { get; set; } = new List<Header>();

    #region bodytypes
    [EnabledIf("Method", HttpMethod.POST, HttpMethod.DELETE, HttpMethod.PUT, HttpMethod.TRACE, HideIfDisabled = true)]
    [Display("Request Body Type", Group: "Request", Order: 25)]
    public RequestBodyType BodyType { get; set; }

    [EnabledIf("BodyType", RequestBodyType.Raw, HideIfDisabled = true)]
    [EnabledIf("Method", HttpMethod.POST, HttpMethod.DELETE, HttpMethod.PUT, HttpMethod.TRACE, HideIfDisabled = true)]
    [Display("raw", Description: "Attach body content to the request", Group: "Request", Order: 30)]
    [Layout(LayoutMode.FullRow, 10)]
    public string Body { get; set; } = string.Empty;
    [Display("Timeout", Description: "HTTP Request timeout", Group: "Request", Order: 100)]
    public Enabled<TimeSpan> UseTimeout { get; set; } = new Enabled<TimeSpan>() { Value = TimeSpan.FromMinutes(1), IsEnabled = false };

    [EnabledIf("BodyType", RequestBodyType.FormData, HideIfDisabled = true)]
    [EnabledIf("Method", HttpMethod.POST, HttpMethod.DELETE, HttpMethod.PUT, HttpMethod.TRACE, HideIfDisabled = true)]
    [Display("form-data", Description: "Attach body content to the request", Group: "Request", Order: 30)]
    public List<FileUpload> BodyFormData { get; set; } = new List<FileUpload>();

    [EnabledIf("BodyType", RequestBodyType.Binary, HideIfDisabled = true)]
    [EnabledIf("Method", HttpMethod.POST, HttpMethod.DELETE, HttpMethod.PUT, HttpMethod.TRACE, HideIfDisabled = true)]
    [Display("binary", Description: "Attach body content to the request", Group: "Request", Order: 30)]
    [FilePath(FilePathAttribute.BehaviorChoice.Open)]
    public string BodyBinary { get; set; }

    [EnabledIf("BodyType", RequestBodyType.FormUrlEncoded, HideIfDisabled = true)]
    [EnabledIf("Method", HttpMethod.POST, HttpMethod.DELETE, HttpMethod.PUT, HttpMethod.TRACE, HideIfDisabled = true)]
    [Display("x-www-form-urlencoded", Description: "Attach body content to the request", Group: "Request", Order: 30)]
    public List<ValuePair> BodyFormUrlEncoded { get; set; } = new List<ValuePair>();
    #endregion


    #region TestSnippets
    [Display("Add test snippet", Description: "Add a javascript snippet which serves as examples to test http request and response", Group: "Response", Order: 95)]
    [AvailableValues("TestSnippets")]
    [EnabledIf("ResponseActionRunTests", true, HideIfDisabled = true)]
    public string AvailableSnippets
    {
        get
        {
            return "";
        }
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
                ResponseTests += $"{Environment.NewLine}{TestSnippetDictionary[value]}";
        }
    }

    [Browsable(false)]
    public List<string> TestSnippets { get { return TestSnippetDictionary.Keys.ToList(); } }

    private Dictionary<string, string> TestSnippetDictionary = new Dictionary<string, string>()
    {
        { "Print to log", "console.log('printing value')" },
        { "Test status code", "assert.equals(tap.response.statusCode, 200);" },
        { "Body contains string", "assert.contains(tap.response.body, 'test');" },
        { "Body equals string", "assert.equals(tap.response.body, 'test');" },
        { "Save environment variable", "tap.SetEnvironmentVariable(key, value);" },
        { "Get environment variable", "tap.GetEnvironmentVariable(key);" },
        { "Remove environment variable", "tap.RemoveEnvironmentVariable(key);" },
        { "Save global variable", "tap.SetGlobalVariable(key, value);" },
        { "Get global variable", "tap.GetGlobalVariable(key);" },
        { "Remove global variable", "tap.RemoveGlobalVariable(key);" },
        { "Response body as JSON", "var json = JSON.parse(tap.Response.Body);" },
        { "Response body as byte array", "var bytes = tap.Response.arrayBuffer;" },
        { "JSON value check", @"var json = JSON.parse(tap.Response.Body);
assert.Equals(json.value, 'Hello TAP');" },
        {"Alert message", "alert('Your message');" }
    };
    #endregion


    #region Response

    [Display("Response action", Description: "Response content actions", Group: "Response", Order: 50)]
    public ResponseAction ResponseAction { get; set; } = ResponseAction.RunTests;

    public bool ResponseActionRunTests => ResponseAction.HasFlag(ResponseAction.RunTests);
    public bool ResponseActionPrint => ResponseAction.HasFlag(ResponseAction.Print);
    public bool ResponseActionSaveToFile => ResponseAction.HasFlag(ResponseAction.SaveToFile);

    [Display("Test", Description: "Test the response", Group: "Response", Order: 90)]
    [Layout(LayoutMode.FullRow, 10)]
    [EnabledIf("ResponseActionRunTests", true, HideIfDisabled = true)]
    public string ResponseTests { get; set; } = "";

    [Display("Save response file", Description: "Save the HTTP response file to disk", Group: "Response", Order: 1000)]
    [FilePath(FilePathAttribute.BehaviorChoice.Save, "")]
    [EnabledIf("ResponseActionSaveToFile", true, HideIfDisabled = true)]
    public string SaveToFile { get; set; } = "";

    #endregion


    [Display("HTTP Request fail behavior", Description: "Step verdict when HTTP request fails", Group: "Misc", Order: 9000)]
    public Verdict HttpRequestFailBehavior { get; set; } = Verdict.Error;

    public RequestStep()
    {
        Name = "{Http Method} {Endpoint}";
    }

    public override void PrePlanRun()
    {
        RestApiEnvironment environment = GetParent<RestApiEnvironment>();
        if (environment is null)
        {
            throw new InvalidOperationException($"This step must have a 'API Environment' step in upwards parent chain");
        }
        base.PrePlanRun();
    }

    public override void Run()
    {
        if (TapThread.Current.AbortToken.IsCancellationRequested)
            return;
        RestApiEnvironment environment = GetParent<RestApiEnvironment>();

        HttpRequestMessage request = SetupRequest(environment.BaseAddress, environment.EnvironmentVariables, environment.GlobalVariables);
        HttpTest httpTest = HttpTest.Generate(request, environment.EnvironmentVariables, environment.GlobalVariables);
        HttpResponseMessage response;

        if (request.Headers.Contains("Cookie"))
        {
            var handler = new HttpClientHandler() { UseCookies = false };
            HttpClient = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan, BaseAddress = new Uri(environment.BaseAddress) };
        }

        try
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(UseTimeout.IsEnabled ? UseTimeout.Value : HttpClient.Timeout);
            var tokens = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, TapThread.Current.AbortToken);
            var watch = Stopwatch.StartNew();
            response = HttpClient.SendAsync(request, tokens.Token).GetAwaiter().GetResult();
            Results.Publish("Request Duration", watch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            UpgradeVerdict(HttpRequestFailBehavior);
            return;
        }

        httpTest.SetResponse(response);

        if (ResponseActionSaveToFile)
            using (FileStream fs = new FileStream(SaveToFile, FileMode.OpenOrCreate))
                response.Content.CopyToAsync(fs).GetAwaiter().GetResult();

        if (ResponseActionRunTests)
        {
            RunTests(httpTest, environment);
        }
        else
        {
            UpgradeVerdict(Verdict.Pass);
        }

        if (ResponseActionPrint && !(ResponseActionRunTests && !httpTest.Tests.Passed))
            PrintResponse(httpTest, LogEventType.Information);

        RunChildSteps();
    }

    private void RunTests(HttpTest httpTest, RestApiEnvironment environment)
    {
        var expandedResponseTests = TryExpand(ResponseTests, environment.EnvironmentVariables, environment.GlobalVariables);

        try
        {
            httpTest.RunTests(expandedResponseTests);
            if (!httpTest.Tests.Passed)
            {
                UpgradeVerdict(Verdict.Fail);
                PrintDetails(httpTest, expandedResponseTests);
            }
            else
            {
                UpgradeVerdict(Verdict.Pass);
            }
        }
        catch (Exception ex)
        {
            UpgradeVerdict(Verdict.Fail);
            Log.Error(ex);
            PrintDetails(httpTest, expandedResponseTests);
        }
    }

    private void PrintResponse(HttpTest httpTest, LogEventType logLevel)
    {
        List<string> output = new List<string>();
        output.Add("Response:");
        output.Add($"  StatusCode: {httpTest.response.statusCode}");
        output.Add($"  Headers:");
        foreach (var header in httpTest.response.Headers)
            output.Add($"    {header.Key} = {header.Value}");

        string body = httpTest.response.body;

        if (!string.IsNullOrWhiteSpace(body) && body.Length <= 10000)
        {
            output.Add($"  Body content:");
            try
            {
                body = JToken.Parse(body).ToString(Formatting.Indented);
            }
            catch
            {
                // Ok, response was not json
            }

            foreach (var line in body.Split(new string[1] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                output.Add($"    {line}");
        }

        if (body.Length > 10000)
            Log.Info($"Request response body too large to print in log window.. ({body.Length} characters)");

        foreach (var line in output)
            Log.TraceEvent(logLevel, 1, line);
    }

    private void PrintDetails(HttpTest httpTest, string javascriptTests)
    {
        if (Name.Equals("{Http Method} {Endpoint}"))
            Log.Error($"{Method} {Endpoint} failed!");
        else
            Log.Error($"{Name} failed!");

        Log.Error("Errors:");
        foreach (var error in httpTest.Tests.Errors)
            Log.Error("  " + error);

        Log.Error("Request:");
        Log.Error($"  URI: {httpTest.Request.Method} {httpTest.Request.Path}");
        Log.Error($"  Headers:");
        foreach (var header in httpTest.Request.Headers)
            Log.Error($"    {header.Key} = {header.Value}");

        Log.Error($"  Body content:");
        foreach (var line in httpTest.Request.Body.Split(new string[1] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            Log.Error($"    {line}");

        PrintResponse(httpTest, LogEventType.Error);

        Log.Error($"Javascript tests:{Environment.NewLine}{javascriptTests}");
    }

    private HttpRequestMessage SetupRequest(string baseUri, VariableHandler environmentVariables, VariableHandler globalVariables)
    {
        System.Net.Http.HttpMethod httpMethod = SetRequestMethod();

        var endpoint = TryExpand(Endpoint, environmentVariables, globalVariables);
        Uri uri = new Uri(baseUri + endpoint, UriKind.Absolute);
        Log.Info($"{Method.ToString()} Request: {uri.ToString()}");
        HttpRequestMessage request = new HttpRequestMessage(httpMethod, uri);

        // Add content
        if (Method == HttpMethod.POST || Method == HttpMethod.DELETE || Method == HttpMethod.PUT || Method == HttpMethod.TRACE)
        {
            if (BodyType == RequestBodyType.Raw)
            {
                string expandedBody = TryExpand(Body, environmentVariables, globalVariables);
                request.Content = new StringContent(expandedBody);
            }
            if (BodyType == RequestBodyType.FormData) // form-data
            {
                var multipart = new MultipartFormDataContent();
                foreach (var file in BodyFormData)
                {
                    var content = new StreamContent(File.Open(file.Value, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                    {
                        Name = file.Name,
                        FileName = Path.GetFileName(file.Value)
                    };
                    content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(file.ContentType);

                    multipart.Add(content);
                }
                request.Content = multipart;
            }
            if (BodyType == RequestBodyType.Binary) // binary
                request.Content = new StreamContent(new FileStream(BodyBinary, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            if (BodyType == RequestBodyType.FormUrlEncoded) // form urlencoded
                request.Content = new FormUrlEncodedContent(BodyFormUrlEncoded.Select(p => new KeyValuePair<string, string>(p.Key, p.Value)));
        }

        foreach (var header in Headers)
        {
            try
            {
                request.Headers.Add(TryExpand(header.Key, environmentVariables, globalVariables), TryExpand(header.Value, environmentVariables, globalVariables));
            }
            catch (InvalidOperationException)
            {
                if (request.Content is object)
                {
                    string headerKey = TryExpand(header.Key, environmentVariables, globalVariables);
                    if (request.Content.Headers.Contains(headerKey))
                        request.Content.Headers.Remove(headerKey);
                    request.Content.Headers.Add(TryExpand(header.Key, environmentVariables, globalVariables), TryExpand(header.Value, environmentVariables, globalVariables));
                }
            }
        }
        return request;
    }

    public string TryExpand(string value, VariableHandler environmentVariables, VariableHandler globalVariables)
    {
        var pattern = @"\{{(.*?)\}}";
        var matches = Regex.Matches(value, pattern);
        foreach (var match in matches)
        {
            string escapedMatch = match.ToString().Replace("{{", "").Replace("}}", "");
            if (environmentVariables.ContainsKey(escapedMatch))
                value = value.Replace(match.ToString(), environmentVariables[escapedMatch]);
            else if (globalVariables.ContainsKey(escapedMatch))
                value = value.Replace(match.ToString(), globalVariables[escapedMatch]);
        }
        return value;
    }

    private System.Net.Http.HttpMethod SetRequestMethod()
    {
        System.Net.Http.HttpMethod httpMethod = System.Net.Http.HttpMethod.Post;

        switch (Method)
        {
            case HttpMethod.DELETE:
                httpMethod = System.Net.Http.HttpMethod.Delete;
                break;
            case HttpMethod.GET:
                httpMethod = System.Net.Http.HttpMethod.Get;
                break;
            case HttpMethod.POST:
                httpMethod = System.Net.Http.HttpMethod.Post;
                break;
            case HttpMethod.PUT:
                httpMethod = System.Net.Http.HttpMethod.Put;
                break;
            case HttpMethod.HEAD:
                httpMethod = System.Net.Http.HttpMethod.Head;
                break;
            case HttpMethod.OPTIONS:
                httpMethod = System.Net.Http.HttpMethod.Options;
                break;
            case HttpMethod.TRACE:
                httpMethod = System.Net.Http.HttpMethod.Trace;
                break;
        }

        return httpMethod;
    }
}

public class Header
{
    public string Key { get; set; }
    public string Value { get; set; }
}

public enum HttpMethod
{
    POST,
    GET,
    DELETE,
    PUT,
    HEAD,
    OPTIONS,
    TRACE
}

public enum RequestBodyType
{
    [Display("none")]
    None,
    [Display("raw")]
    Raw,
    [Display("form-data")]
    FormData,
    [Display("binary")]
    Binary,
    [Display("x-www-form-urlencoded")]
    FormUrlEncoded
}

[Flags]
public enum ResponseAction
{
    [Display("Run Tests")]
    RunTests = 2,
    [Display("Print to log")]
    Print = 4,
    [Display("Save to file")]
    SaveToFile = 8
}

public class FileUpload
{
    [Display("Key", Order: 1)]
    public string Name { get; set; } = "file";

    [Display("Value", Order: 2)]
    [FilePath(FilePathAttribute.BehaviorChoice.Open)]
    public string Value { get; set; }

    [Display("Content Type", Order: 3)]
    public string ContentType { get; set; } = "application/octet-stream";
}

public class ValuePair
{
    public string Key { get; set; }
    public string Value { get; set; }
}

public enum TestFailBehavior
{
    Fail,
    Pass,
    Error,
    NotSet,
    Inconclusive
}