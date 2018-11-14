using System.Net;
using System.Net.Sockets;
using Vostok.Commons.Threading;

namespace Vostok.Clusterclient.Transport.Tests.Shared.Utilities
{
    internal class FreeTcpPortFinder
    {
        public static int GetFreePort()
        {
            while (true)
            {
                var port = ThreadSafeRandom.Next(11000, 65000);
                if (IsAvailable(port))
                    return port;
            }
        }

        private static bool IsAvailable(int port)
        {
            
            var listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}