using System.IO;
using System.Net.Sockets;

namespace System.Net.Http
{
    class TcpClientAdapter
    {
        private readonly TcpClient tcpClient;

        public TcpClientAdapter(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;

            LocalEndPoint = (IPEndPoint)tcpClient.Client.LocalEndPoint;
            RemoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
        }

        public IPEndPoint LocalEndPoint
        {
            get;
            private set;
        }

        public IPEndPoint RemoteEndPoint
        {
            get;
            private set;
        }

        public Stream GetInputStream()
        {
            return tcpClient.GetStream();
        }

        public Stream GetOutputStream()
        {
            return tcpClient.GetStream();
        }

        public void Dispose()
        {
            tcpClient.Dispose();
        }
    }
}