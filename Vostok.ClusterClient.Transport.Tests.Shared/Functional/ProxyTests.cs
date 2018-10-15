using System;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Tests.Shared.Functional.Helpers;

namespace Vostok.Clusterclient.Transport.Tests.Shared.Functional
{
    public class ProxyTests<TConfig> : TransportTestsBase<TConfig>
        where TConfig : ITransportTestConfig, new()
    {
        [Test]
        public void Should_send_request_to_proxy_if_setting_is_true()
        {
            using (var proxy = TestServer.StartNew(ctx => ctx.Response.StatusCode = 201))
            {
                SetSettings(s => s.Proxy = new WebProxy(proxy.Url, false));

                var expectedUrl = new Uri($"http://vostok:{proxy.Port}");
                
                var request = Request.Get("http://vostok");
                var response = Send(request);
                response.Code.Should().Be(ResponseCode.Created);
                proxy.LastRequest.Url.Should().Be(expectedUrl);
            }
        }
    }
}