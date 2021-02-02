using OpenTap;
using System;
using System.Collections.Generic;

namespace WebApiTester
{
    internal class Assert
    {

        public List<string> Errors = new List<string>();
        public bool Passed { get; private set; } = true;

        public void equals(object actual, object expected, string message = "", bool allowFail = false)
        {
            if (!(expected is null && actual is null) && !expected.Equals(actual))
            {
                if (string.IsNullOrEmpty(message) == false)
                    Errors.Add(message);
                Errors.Add($"Expected '{actual}' to equal '{expected}'");
                if (!allowFail)
                    Passed = false;
            }
        }

        public void contains(string fullString, string substring, string message = "", bool allowFail = false)
        {
            if (!fullString.Contains(substring))
            {
                if (string.IsNullOrEmpty(message) == false)
                    Errors.Add(message);
                Errors.Add($"Expected '{fullString}' to contain '{substring}'");

                if (!allowFail)
                    Passed = false;
            }
        }
    }
}