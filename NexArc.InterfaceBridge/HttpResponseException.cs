using System.Net;

namespace NexArc.InterfaceBridge;

public class HttpResponseException(HttpResponseMessage response) : Exception
{
    public HttpResponseMessage Response => response;

    public HttpResponseException(HttpStatusCode statusCode) : this(new HttpResponseMessage(statusCode)) { }
    
    public HttpResponseException(HttpStatusCode statusCode, string message) : this(new HttpResponseMessage(statusCode) { Content = new StringContent(message) }) { }
}