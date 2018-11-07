using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Vostok.Commons.Environment;
using Vostok.Commons.Threading;

namespace Vostok.Clusterclient.Transport.Tests.Shared.Utilities
{
    internal class FreeTcpPortFinder
    {
        public static int GetFreePort()
        {
            if (RuntimeDetector.IsDotNetCore20 && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetFreePortOnLinuxNetCore20();
            }
            
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        private static int GetFreePortOnLinuxNetCore20()
        {
            // a ugly workaround for bug described here:
            // https://stackoverflow.com/questions/46972797/dotnet-core-2-httplistener-not-working-on-ubuntu
            // https://github.com/dotnet/corefx/issues/25016

            return ThreadSafeRandom.Next(10000, 60000);
        }
    }
}