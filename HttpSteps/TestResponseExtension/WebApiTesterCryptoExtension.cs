using System.Linq;
using System.Text;
using OpenTap;

namespace HttpSteps.TestResponseExtension;

[Display("crypto")]
public class Crypto : ITestResponseExtension
{
    public string SHA1(string text)
    {
        return string.Join("", System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(text)).Select(x => x.ToString("X2")));
    }
}