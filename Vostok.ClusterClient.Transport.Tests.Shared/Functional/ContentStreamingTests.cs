using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Tests.Shared.Functional.Helpers;

namespace Vostok.Clusterclient.Transport.Tests.Shared.Functional
{
    public abstract class ContentStreamingTests<TConfig> : TransportTestsBase<TConfig>
        where TConfig : ITransportTestConfig, new()
    {
        [Test]
        public void Should_read_large_response_body()
        {
            var serverBuffer = new byte[Constants.Megabytes];
            var clientBuffer = new byte[Constants.Megabytes];

            var iterations = 10000;
            
            using (var cts = new CancellationTokenSource())
            using (var server = TestServer.StartNew(
                ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    
                    for (var i = 0; i < iterations && !cts.Token.IsCancellationRequested; ++i)
                        ctx.Response.OutputStream.Write(serverBuffer, 0, serverBuffer.Length);
                }))
            {
     
                SetSettings(s => s.UseResponseStreaming = l => true);
                var receive = Task.Run(
                    () =>
                    {
                        try
                        {
                            var response = Send(Request.Put(server.Url));

                            using (var stream = response.Stream)
                            {
                                long count = 0;
                                while (true)
                                {
                                    var c = stream.Read(clientBuffer, 0, clientBuffer.Length);
                                    if (c == 0)
                                        break;
                                    count += c;
                                }

                                return count;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            return 0;
                        }
                    });

                var memoryMonitor = MonitorMemoryAsync(cts.Token, 500 * Constants.Megabytes);

                var task = Task.WhenAny(memoryMonitor, receive).GetAwaiter().GetResult();
                if (task == memoryMonitor && memoryMonitor.GetAwaiter().GetResult())
                {
                    cts.Cancel();
                    Assert.Fail();
                }

                receive.GetAwaiter().GetResult().Should().Be(iterations * serverBuffer.Length);
            }
        }

        private async Task<bool> MonitorMemoryAsync(CancellationToken ctx, long limit)
        {
            try
            {
                while (!ctx.IsCancellationRequested)
                {
                    if (Process.GetCurrentProcess().PrivateMemorySize64 > limit)
                        return true;
                    await Task.Delay(1000, ctx);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }
    }
}