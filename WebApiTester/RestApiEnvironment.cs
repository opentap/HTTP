using OpenTap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("WebApiTester.Tests")]

namespace WebApiTester
{
    [AllowAnyChild]
    [Display("API Environment", Group: "Web API Tester")]
    public class RestApiEnvironment : TestStep
    {
        [Display("Host", Group: "Web Api Address")]
        public string BaseAddress { get; set; } = "http://127.0.0.1:5000";

        [Display("Environment Variables", Group: "Variables")]
        public List<Variable> PublicEnvironmentVariables { get; set; } = new List<Variable>();

        [Browsable(false)]
        public VariableHandler EnvironmentVariables { get => new VariableHandler(PublicEnvironmentVariables); }

        [Display("Global Variables", Group: "Variables")]
        public List<Variable> PublicGlobalVariables { get => _GlobalVariables; set => _GlobalVariables = value; }

        private static List<Variable> _GlobalVariables { get; set; } = new List<Variable>();

        [Browsable(false)]
        public VariableHandler GlobalVariables = new VariableHandler(_GlobalVariables);
        public override void Run()
        {
            if (GetParent<RestApiEnvironment>() is object)
            {
                foreach (KeyValuePair<string, string> variable in GetParent<RestApiEnvironment>().EnvironmentVariables)
                    if (!EnvironmentVariables.ContainsKey(variable.Key))
                        EnvironmentVariables[variable.Key] = variable.Value;
            }
            RunChildSteps();
        }
        public RestApiEnvironment()
        {
            Name = "{Host} Environment";
            Rules.Add(() => !string.IsNullOrWhiteSpace(BaseAddress), "No host address specified", nameof(BaseAddress));
            Rules.Add(() => !BaseAddress.StartsWith("http://") || !BaseAddress.StartsWith("https://"), "Host must start with 'http://' or 'https://'", nameof(BaseAddress));
        }
    }

    public class Variable
    {
        public Variable()
        {

        }
        public Variable(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class VariableHandler : IDictionary<string, string>
    {
        private List<Variable> Variables = null;

        public VariableHandler(List<Variable> variables)
        {
            Variables = variables;
        }
        public string this[string key] { get => Variables.FirstOrDefault(s => s.Key == key).Value; set => RemoveAndAdd(key, value); }

        private void RemoveAndAdd(string key, string value)
        {
            Variables.RemoveAll(s => s.Key == key);
            Variables.Add(new Variable(key, value));
        }

        public ICollection<string> Keys => Variables.Select(s => s.Key).ToList();

        public ICollection<string> Values => Variables.Select(s => s.Value).ToList();

        public int Count => Variables.Count;

        public bool IsReadOnly => false;

        public void Add(string key, string value)
        {
            if (Variables.Any(s => s.Key == key))
                throw new InvalidOperationException("A key already exists");
            Variables.Add(new Variable(key, value));
        }

        public void Add(KeyValuePair<string, string> item)
        {
            if (Variables.Any(s => s.Key == item.Key))
                throw new InvalidOperationException("A key already exists");
            Variables.Add(new Variable(item.Key, item.Value));
        }

        public void Clear()
        {
            Variables.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return Variables.Any(s => s.Key == item.Key && s.Value == item.Value);
        }

        public bool ContainsKey(string key)
        {
            return Variables.Any(s => s.Key == key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {

        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return Variables.ToDictionary(s => s.Key, p => p.Value).GetEnumerator();
        }

        public bool Remove(string key)
        {
            if (!ContainsKey(key))
                return false;
            Variables.RemoveAll(s => s.Key == key);
            return true;

        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            if (!Contains(item))
                return false;
            Variables.RemoveAll(s => s.Key == item.Key && s.Value == item.Value);
            return true;
        }

        public bool TryGetValue(string key, out string value)
        {
            value = "";
            if (!ContainsKey(key))
                return false;
            value = this[key];
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Variables.GetEnumerator();

        }
    }
}
