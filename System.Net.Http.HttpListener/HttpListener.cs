using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace System.Net.Http
{
    /// <summary>
    /// Listenes for Http requests.
    /// </summary>
    public sealed class HttpListener : IDisposable
    {
        Task _listener;
        private readonly TcpListenerAdapter _tcpListener;
        CancellationTokenSource _cts;
        private bool disposedValue = false; // To detect redundant calls
        private bool _isListening;

        private HttpListener()
        {
            _cts = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListener"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        public HttpListener(IPAddress address, int port) : this()
        {
            LocalEndpoint = new IPEndPoint(address, port);

            _tcpListener = new TcpListenerAdapter(LocalEndpoint);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListener"/> class.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public HttpListener(IPEndPoint endpoint) : this()
        {
            _tcpListener = new TcpListenerAdapter(endpoint);
        }

        /// <summary>
        /// Gets a value indicating whether the HttpListener is running or not.
        /// </summary>
        public bool IsListening => _isListening;

        /// <summary>
        /// Gets the underlying Socket.
        /// </summary>
        public Socket Socket => _tcpListener.Socket;

        /// <summary>
        /// Gets the local endpoint on which the StartListenerAsync is running.
        /// </summary>
        public IPEndPoint LocalEndpoint
        {
            get;
        }

        /// <summary>
        /// Starts the StartListenerAsync.
        /// </summary>
        public void Start()
        {
            if (disposedValue)
                throw new ObjectDisposedException("Object has been disposed.");

            if (_cts != null)
                throw new InvalidOperationException("HttpListener is already running.");

            _cts = new CancellationTokenSource();
            _isListening = true;
            _listener = Task.Run(StartListenerAsync, _cts.Token);
        }

        private async Task StartListenerAsync()
        {
            try
            {
                _tcpListener.Start();

                while (_isListening)
                {
                    // Await request.

                    var client = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);

                    var request = new HttpListenerRequest(client);

                    // Handle request in a separate thread.

                    Task.Run(async () =>
                    {
                        // Process request.

                        var response = new HttpListenerResponse(request, client);

                        try
                        {
                            await request.ProcessAsync();

                            response.Initialize();

                            if (Request == null)
                            {
                                // No Request handlers exist. Respond with "Not Found".

                                response.NotFound();
                                response.Close();
                            }
                            else
                            {
                                // Invoke Request handlers.

                                Request(this, new HttpListenerRequestEventArgs(request, response));
                            }
                        }
                        catch (Exception)
                        {
                            response.CloseSocket();
                        }
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _isListening = false;
                _cts = null;
            }
        }

        /// <summary>
        /// Closes the StartListenerAsync.
        /// </summary>
        public void Close()
        {
            if (_cts == null)
                throw new InvalidOperationException("HttpListener is not running.");

            Request = null;

            _cts.Cancel();
            _cts = null;

            _isListening = false;
            _tcpListener.Stop();

            try
            {
                // Stop task
                _listener.Wait(TimeSpan.FromMilliseconds(1));
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Awaits the next HTTP request and returns its context.
        /// </summary>
        /// <returns></returns>
        public Task<HttpListenerContext> GetContextAsync()
        {
            // Await a Request and return the context to caller.

            var tcs = new TaskCompletionSource<HttpListenerContext>();
            EventHandler<HttpListenerRequestEventArgs> requestHandler = null;
            requestHandler = (sender, evArgs) =>
            {
                var context = new HttpListenerContext(evArgs.Request, evArgs.Response);
                tcs.SetResult(context);
                Request -= requestHandler;
            };
            Request += requestHandler;
            return tcs.Task;
        }

        public event EventHandler<HttpListenerRequestEventArgs> Request;

        #region IDisposable Support

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                Close();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HttpListener() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed