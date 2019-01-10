using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Tests.Shared.Functional.Helpers;

namespace Vostok.Clusterclient.Transport.Tests.Shared.Functional
{
    public abstract class ContentStreamingTests<TConfig> : TransportTestsBase<TConfig>
        where TConfig : ITransportTestConfig, new()
    {
        [Test]
        public void Should_send_large_request_body()
        {
            var size = 10 * Constants.Gigabytes;

            using (var cts = new CancellationTokenSource())
            using (var server = TestServer.StartNew(
                ctx => { ctx.Response.StatusCode = 200; }))
            {
                server.BufferRequestBody = false;

                var sendTask = SendAsync(Request.Post(server.Url).WithContent(new LargeStream(size)), 10.Minutes(), cts.Token);
                var memoryMonitor = MonitorMemoryAsync(cts.Token, Process.GetCurrentProcess().WorkingSet64 + 250 * Constants.Megabytes);
                
                var task = Task.WhenAny(memoryMonitor, sendTask).GetAwaiter().GetResult();
                if (task == memoryMonitor && memoryMonitor.GetAwaiter().GetResult())
                {
                    cts.Cancel();
                    Assert.Fail();
                }

                server.LastRequest.BodySize.Should().Be(size);
            }
        }
        
        [Test]
        public void Should_read_large_response_body()
        {
            var serverBuffer = new byte[Constants.Megabytes];
            var clientBuffer = new byte[Constants.Megabytes];

            var iterations = 10 * 1024;
            
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
                            var response = Send(Request.Put(server.Url), 10.Minutes());

                            using (var stream = response.Stream)
                            {
                                long count = 0;
                                while (!cts.Token.IsCancellationRequested)
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

                var memoryMonitor = MonitorMemoryAsync(cts.Token, Process.GetCurrentProcess().WorkingSet64 + 250 * Constants.Megabytes);

                var task = Task.WhenAny(memoryMonitor, receive).GetAwaiter().GetResult();
                if (task == memoryMonitor && memoryMonitor.GetAwaiter().GetResult())
                {
                    cts.Cancel();
                    Assert.Fail();
                }

                receive.GetAwaiter().GetResult().Should().Be((long) iterations * serverBuffer.Length);
            }
        }

        private static async Task<bool> MonitorMemoryAsync(CancellationToken ctx, long limit)
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
        
        
        #region LargeStream

        private class LargeStream : Stream
        {
            private readonly long length;
            private long read;

            public LargeStream(long length)
            {
                this.length = length;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var r = (int) Math.Max(0, Math.Min(count, length - read));
                read += r;
                return r;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
        }

        #endregion

    }
}