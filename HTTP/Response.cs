using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
namespace HTTP;

public class Response
{
    public byte[] arrayBuffer { get; private set; }
    public string body { get; private set; }
    public int statusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    public Response(HttpResponseMessage response)
    {
        body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        arrayBuffer = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        statusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
            Headers.Add(header.Key, header.Value.FirstOrDefault());
        
        foreach (var header in response.Content.Headers)
            Headers.Add(header.Key, header.Value.FirstOrDefault());
    }

    public Response()
    {
    }
}