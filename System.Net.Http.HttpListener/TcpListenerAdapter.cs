using System.Net.Sockets;
using System.Threading.Tasks;

namespace System.Net.Http
{
    class TcpListenerAdapter
    {
        private TcpListener _tcpListener;

        public TcpListenerAdapter(IPEndPoint localEndpoint)
        {
            LocalEndpoint = localEndpoint;

            Initialize();
        }

        public IPEndPoint LocalEndpoint { get; private set; }

        public Task<TcpClientAdapter> AcceptTcpClientAsync()
        {
            return acceptTcpClientInternalAsync();
        }

        private void Initialize()
        {
            _tcpListener = new TcpListener(LocalEndpoint);
        }

        private async Task<TcpClientAdapter> acceptTcpClientInternalAsync()
        {
            var tcpClient = await _tcpListener.AcceptTcpClientAsync();
            return new TcpClientAdapter(tcpClient);
        }

        public void Start()
        {
            _tcpListener.Start();
        }

        public void Stop()
        {
            _tcpListener.Stop();
        }

        public Socket Socket
        {
            get
            {
                return _tcpListener.Server;
            }
        }
    }
}