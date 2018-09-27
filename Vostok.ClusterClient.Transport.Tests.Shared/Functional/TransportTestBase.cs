using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Core.Transport;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Tests.Functional
{
    [TestFixture]
    public abstract class TransportTestsBase<TConfig>
        where TConfig : ITransportTestConfig, new()
    {
        protected ILog log;
        protected ITransport transport;
        
        private readonly TConfig config = new TConfig();

        static TransportTestsBase()
        {
            ThreadPool.SetMaxThreads(32767, 32767);
            ThreadPool.SetMinThreads(2048, 2048);
        }

        [SetUp]
        public virtual void SetUp()
        {
            log = config.CreateLog();
            transport = config.CreateTransport(new TestTransportSettings(), log);
        }

        protected Task<Response> SendAsync(Request request, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return transport.SendAsync(request, timeout ?? 1.Minutes(), cancellationToken);
        }

        protected Response Send(Request request, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return transport.SendAsync(request, timeout ?? 1.Minutes(), cancellationToken).GetAwaiter().GetResult();
        }

        protected void SetSettings(Action<TestTransportSettings> update)
        {
            var settings = config.CreateDefaultSettings();
            update(settings);
            transport = config.CreateTransport(settings, log);
        }
    }

    public interface ITransportTestConfig
    {
        ILog CreateLog();
        ITransport CreateTransport(TestTransportSettings settings, ILog log);
        TestTransportSettings CreateDefaultSettings();
    }

    public class TestTransportSettings
    {
        public IWebProxy Proxy { get; set; }
        public int MaxConnectionsPerEndpoint { get; set; } = 10 * 1000;
        public long? MaxResponseBodySize { get; set; }
        public int ConnectionAttempts { get; set; } = 2;
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMilliseconds(750);
        public Func<int, byte[]> BufferFactory { get; set; } = size => new byte[size];
        public Predicate<long?> UseResponseStreaming { get; set; } = _ => false;
        public bool AllowAutoRedirect { get; set; }
    }
}