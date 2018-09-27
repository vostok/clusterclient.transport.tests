using System;
using System.Collections.Specialized;

namespace Vostok.ClusterClient.Transport.Tests.Functional.Helpers
{
    public class ReceivedRequest
    {
        public string Method { get; set; }
        public Uri Url { get; set; }
        public byte[] Body { get; set; }
        public long BodySize { get; set; }
        public NameValueCollection Headers { get; set; }
        public NameValueCollection Query { get; set; }
    }
}