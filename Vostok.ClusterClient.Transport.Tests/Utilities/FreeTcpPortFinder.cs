using System.Net;
using System.Net.Sockets;

namespace Vostok.ClusterClient.Transport.Tests.Utilities
{
    internal class FreeTcpPortFinder
    {
        public static int GetFreePort()
        {
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
    }
}