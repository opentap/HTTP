using HTTP;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Tests;

public class VariablesTest
{
    [Test]
    public void AddVariables()
    {
        RestApiEnvironment testSession = new RestApiEnvironment();
        HttpTest httpTest = new HttpTest(new Request(), testSession.EnvironmentVariables, testSession.GlobalVariables);

        string javascript = @"
            tap.SetEnvironmentVariable('Check', 'Nice');
            tap.SetGlobalVariable('Global', 'Variable');
";
        httpTest.RunTests(javascript);

        Assert.True(testSession.EnvironmentVariables.ContainsKey("Check"));
        Assert.AreEqual(testSession.EnvironmentVariables["Check"], "Nice");


        Assert.True(testSession.GlobalVariables.ContainsKey("Global"));
        Assert.AreEqual(testSession.GlobalVariables["Global"], "Variable");
    }

    [Test]
    public void UserAddVariables()
    {
        RestApiEnvironment testSession = new RestApiEnvironment();
        testSession.PublicEnvironmentVariables.Add(new Variable("Test", "One"));
        HttpTest httpTest = new HttpTest(new Request(), testSession.EnvironmentVariables, testSession.GlobalVariables);

        string javascript = @"
            tap.SetGlobalVariable('Global', tap.GetEnvironmentVariable('Test'));
";
        httpTest.RunTests(javascript);

        Assert.True(testSession.GlobalVariables.ContainsKey("Global"));
        Assert.AreEqual(testSession.GlobalVariables["Global"], "One");
    }

    [Test]
    public void UserAddVariablesDifferentEnvironment()
    {
        RestApiEnvironment testSession = new RestApiEnvironment();
        testSession.PublicEnvironmentVariables.Add(new Variable("Test", "One"));
        HttpTest httpTest = new HttpTest(new Request(), testSession.EnvironmentVariables, testSession.GlobalVariables);

        string javascript = @"
            tap.SetGlobalVariable('Global', tap.GetEnvironmentVariable('Test'));
";
        httpTest.RunTests(javascript);

        Assert.True(testSession.GlobalVariables.ContainsKey("Global"));
        Assert.AreEqual(testSession.GlobalVariables["Global"], "One");

        RestApiEnvironment testSession2 = new RestApiEnvironment();
        HttpTest httpTest2 = new HttpTest(new Request(), testSession2.EnvironmentVariables, testSession.GlobalVariables);
        string javascript2 = @"
            tap.SetEnvironmentVariable('Env', tap.GetGlobalVariable('Global'));
";
        httpTest2.RunTests(javascript2);

        Assert.True(testSession2.EnvironmentVariables.ContainsKey("Env"));
        Assert.AreEqual(testSession2.EnvironmentVariables["Env"], "One");

    }

    [Test]
    public void CheckGlobalVsEnvironment()
    {
        RestApiEnvironment testSession = new RestApiEnvironment();

        HttpTest httpTest = new HttpTest(new Request(), testSession.EnvironmentVariables, testSession.GlobalVariables);

        string javascript = @"
            tap.SetEnvironmentVariable('Check', 'Nice');
            tap.SetGlobalVariable('Global', 'Variable');
";
        httpTest.RunTests(javascript);

        RestApiEnvironment testSession2 = new RestApiEnvironment();

        HttpTest httpTest2 = new HttpTest(new Request(), testSession2.EnvironmentVariables, testSession.GlobalVariables);
        Assert.True(testSession2.GlobalVariables.ContainsKey("Global"));
        Assert.False(testSession2.EnvironmentVariables.ContainsKey("Check"));
    }

    [Test]
    public void GetGlobalVariableInNewSession()
    {
        RestApiEnvironment testSession = new RestApiEnvironment();
        HttpTest httpTest = new HttpTest(new Request(), testSession.EnvironmentVariables, testSession.GlobalVariables);

        string javascript = @"
            tap.SetGlobalVariable('Global', 'Variable');
";
        httpTest.RunTests(javascript);

        RestApiEnvironment testSession2 = new RestApiEnvironment();

        HttpTest httpTest2 = new HttpTest(new Request(), testSession2.EnvironmentVariables, testSession.GlobalVariables);

        string javascript2 = @"
            tap.SetEnvironmentVariable('Check', tap.GetGlobalVariable('Global'));
";
        httpTest2.RunTests(javascript2);

        Assert.True(testSession2.EnvironmentVariables.ContainsKey("Check"));
        Assert.AreEqual(testSession2.EnvironmentVariables["Check"], "Variable");
    }

    [Test]
    public void ExpandEndpointEnvironment()
    {
        RestApiEnvironment testSession = new RestApiEnvironment();
        testSession.PublicEnvironmentVariables.Add(new Variable("api", "3.0"));
        RequestStep step = new RequestStep()
        {
            Endpoint = "/{{api}}/lol"
        };

        var expanded = step.TryExpand(step.Endpoint, testSession.EnvironmentVariables, testSession.GlobalVariables);

        Assert.AreEqual("/3.0/lol", expanded);
    }

    [Test]
    public void ExpandEndpointGlobal()
    {
        RestApiEnvironment testSession = new RestApiEnvironment();
        testSession.PublicGlobalVariables.Add(new Variable("api", "3.0"));
        RequestStep step = new RequestStep()
        {
            Endpoint = "/{{api}}/lol"
        };

        var expanded = step.TryExpand(step.Endpoint, testSession.EnvironmentVariables, testSession.GlobalVariables);

        Assert.AreEqual("/3.0/lol", expanded);
    }
}