using WebApiTester;
using NUnit.Framework;
using OpenTap;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Assert = NUnit.Framework.Assert;

namespace Tests
{
    public class PostmanConversion
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public void TryExpandTest()
        {
            string added = "added";
            RestApiEnvironment testSession = new RestApiEnvironment();
            testSession.EnvironmentVariables.Add("addNewStep", added);

            RequestStep step = new RequestStep();
            string expanded = step.TryExpand("{{addNewStep}}", testSession.EnvironmentVariables, testSession.GlobalVariables);
            Assert.AreEqual(added, expanded);
        }

        [Test]
        public void ConvertSetEnvironmentVariable()
        {
            string script = "pm.environment.set('guid', responseBody);";

            string converted = new ApiTestCliAction().ConvertScript(script.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList());

            Assert.AreEqual("tap.setEnvironmentVariable('guid', tap.response.body);", converted);
        }

        [Test]
        public void ConvertStatusCodeTest()
        {
            string script =
                @"pm.test('Status code is ok', function(){
                pm.response.to.have.status(200);
                });";

            string converted = new ApiTestCliAction().ConvertScript(script.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList());

            Assert.True(converted.Contains("assert.equals(tap.response.statusCode, 200);"));
        }

        [Test]
        public void ConvertResponseJsonTest()
        {
            string script = "var jsonData = pm.response.json();";
            string converted = new ApiTestCliAction().ConvertScript(script.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList());

            Assert.AreEqual("var jsonData = JSON.parse(tap.response.body);", converted);
        }

        [Test]
        public void ConvertPostmanCollectionTest()
        {
            var script = new[]{ "pm.test(\"Any names\", function () {",
                            "    var jsonData = pm.response.json();",
                            "    pm.expect(jsonData.length > 0).to.be.true;",
                            "    pm.expect(jsonData.includes(\"Keysight Test Automation Platform\")).to.be.true;",
                            "    ",
                            "    var count = jsonData.filter(n => n === \"Keysight Test Automation Platform\").length;",
                            "    pm.expect(count > 0).to.be.true;",
                            "});",
                            "",
                            "pm.test(\"Don't have TAP\", function () {",
                            "    var jsonData = pm.response.json();",
                            "    var jsonObject = xml2Json(responseBody);",
                            "    pm.expect(properties.length).to.eql(6);",
                            "    pm.expect(jsonData.includes(\"Demonstration\")).to.be.false;",
                            "    pm.expect(jsonData.ArrayOfString.string).to.include('TapPackage');",
                            "    pm.response.to.have.status(200);",
                            "});"};

            var test = PostmanTestCollectionConverter.Convert(script.ToList());
            Assert.IsFalse(test.Contains("pm"));
        }

        [TestCase("var jsonData = pm.response.json();", "var jsonData = JSON.parse(tap.response.body);")]
        [TestCase("pm.expect(jsonData.length > 0).to.be.true;", "assert.equals(jsonData.length > 0, true);")]
        [TestCase("pm.expect(jsonData.includes(\"Keysight Test Automation Platform\")).to.be.true;", "assert.equals(jsonData.includes(\"Keysight Test Automation Platform\"), true);")]
        [TestCase("var count = jsonData.filter(n => n === \"Keysight Test Automation Platform\").length;", "var count = jsonData.filter(n => n === \"Keysight Test Automation Platform\").length;")]
        [TestCase("pm.expect(count > 0).to.be.true;", "assert.equals(count > 0, true);")]
        [TestCase("var jsonObject = xml2Json(responseBody);", "var jsonObject = JSON.parse(tap.parseXmlToJson(tap.response.body));")]
        [TestCase("pm.expect(properties.length).to.eql(6);", "assert.equals(properties.length, 6);")]
        [TestCase("pm.expect(jsonData.includes(\"Demonstration\")).to.be.false;", "assert.equals(jsonData.includes(\"Demonstration\"), false);")]
        [TestCase("pm.expect(jsonData.ArrayOfString.string).to.include('TapPackage');", "assert.equals(jsonData.ArrayOfString.string.includes('TapPackage'), true);")]
        [TestCase("pm.response.to.have.status(200);", "assert.equals(tap.response.statusCode, 200);")]
        [TestCase("var xml = xml2Json(pm.response);", "var xml = JSON.parse(tap.parseXmlToJson(tap.response.body));")]
        [TestCase("pm.environment.set", "tap.setEnvironmentVariable")]
        [TestCase("pm.expect(jsonData.FailedToStart).to.equal(false);", "assert.equals(jsonData.FailedToStart, false);")]
        public void ConvertPostmanCollectionTestResult(string input, string expected)
        {
            var actual = PostmanTestCollectionConverter.Convert(new[] { input }.ToList());

            Assert.AreEqual(expected, actual);
        }
    }
}