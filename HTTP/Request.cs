using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
namespace HTTP;

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