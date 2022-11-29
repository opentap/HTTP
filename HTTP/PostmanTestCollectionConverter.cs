using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HTTP;

public class PostmanTestCollectionConverter
{
    public static string Convert(List<string> script)
    {
        for (int i = 0; i < script.Count; i++)
            script[i] = script[i].Trim();

        var js = string.Join(Environment.NewLine, script);

        js = Regex.Replace(js, @"pm.test\((?:""|')(.*?)(?:""|')(?:.|\n)*?{((?:.|\n)*?)}\);?", m => {
            return $"console.log('Running test: {m.Groups[1].Value}');\n{m.Groups[2].Value}";
        });


        // Sorted by specificity

        js = Regex.Replace(js, @"pm\.response\.to\.have\.status\((.*?)\)(?:;|$)", m => $"assert.equals(tap.response.statusCode, {m.Groups[1].Value});");

        js = Regex.Replace(js, @"pm\.expect\((.*?)\)\.to\.be\.(.*?)(?:;|$)", m => $"assert.equals({m.Groups[1].Value}, {m.Groups[2].Value});");
        js = Regex.Replace(js, @"pm\.expect\((.*?)\)\.to\.include(.*?)(?:;|$)", m => $"assert.equals({m.Groups[1].Value}.includes{m.Groups[2].Value}, true);");
        js = Regex.Replace(js, @"pm\.expect\((.*?)\)\.to\.eql?\((.*?)\)", m => $"assert.equals({m.Groups[1].Value}, {m.Groups[2].Value})");
        js = Regex.Replace(js, @"pm\.expect\((.*?)\)\.to\.equals?\((.*?)\)", m => $"assert.equals({m.Groups[1].Value}, {m.Groups[2].Value})");

        js = Regex.Replace(js, @"xml2Json\((.*?)\)", m => $"JSON.parse(tap.parseXmlToJson({m.Groups[1].Value}))");
        js = Regex.Replace(js, @"\s((?:\w|\.)*?)\.json\(\)", m => $" JSON.parse({m.Groups[1].Value})");

        js = Regex.Replace(js, "responseBody", m => $"tap.response.body");
        js = Regex.Replace(js, @"pm\.response", "tap.response.body");
        js = Regex.Replace(js, @"pm\.environment\.set", "tap.setEnvironmentVariable");
        js = Regex.Replace(js, @"pm\.environment\.get", "tap.getEnvironmentVariable");
        js = Regex.Replace(js, @"pm\.environment\.unset", "tap.removeEnvironmentVariable");
        js = Regex.Replace(js, @"pm\.variables\.get", "tap.getEnvironmentVariable");


        return js;
    }
}