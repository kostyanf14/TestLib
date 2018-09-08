using System;
using System.Net.Http;

namespace TestLib.Worker.ClientApi.Models
{
    public class RequestMessage
    {
        public string RequestUri;
        public HttpContent Data;

        public RequestMessage(string requestUri, HttpContent data)
        {
            RequestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
            Data = data;
        }
    }
}