using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xMQ.Protocol;
using xMQ.Util;

namespace xMQ.SocketsType
{
    internal class TcpSocket : GenericSocket, ISocket
    {
        public const string SCHEME = "tcp";

        private ManualResetEventSlim resetEvent;
        private Socket socket;
        private List<Socket> clients;

        public Uri UriAddress { get; }

        public SocketProtocolController ConnectionController { get; }
        public List<Socket> SocketsSelector { get; private set; } = new List<Socket>();
        public List<Socket> SocketsErrors { get; private set; } = new List<Socket>();

        public TcpSocket()
        {
            resetEvent = new ManualResetEventSlim(false);
            clients = new List<Socket>();
        }

        public TcpSocket(Socket _socket)
           : this()
        {
            socket = _socket;
        }

        public TcpSocket(Uri uri, SocketProtocolController connectionController)
          : this()
        {
            UriAddress = uri;
            ConnectionController = connectionController;
        }

        public TcpSocket(Socket _socket, SocketProtocolController connectionController)
            :this(_socket)
        {
            ConnectionController = connectionController;
        }

        public void Bind()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);

            socket.Bind(new IPEndPoint(IPAddress.Parse(UriAddress.Host), UriAddress.Port));

            ServerRunning = true;

            Task.Run(() => { Listen(); });
            Task.Run(() => { Handler(); });

            socket.Listen((int)SocketOptionName.MaxConnections);
        }

        public void Connect(int timeout)
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);

            var endpoint = new IPEndPoint(IPAddress.Parse(UriAddress.Host), UriAddress.Port);
            var result = socket.BeginConnect(endpoint, null, null);

            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                socket.EndConnect(result);

                ClientRunning = true;

                Task.Run(() => { Handler(); });
            }
            else
            {
                socket.Close();
                throw new SocketException(10060); // Connection timed out.
            }
        }

        public void Close()
        {
            socket.Close();

            for (var i = 0; i < clients.Count; i++)
                SocketMapper.RemoveISocketMapper(clients[i]);

            clients.Clear();
            ServerRunning = false;
            ClientRunning = false;
        }

        private void Listen()
        {
            while (ServerRunning)
            {
                if (socket.Poll(-1, SelectMode.SelectRead))
                {
                    var task = socket.AcceptAsync();
                    task.ContinueWith((taskResult) =>
                    {
                        var clientSocket = taskResult.Result;

                        clients.Add(clientSocket);

                        var tcpClient = new TcpSocket(clientSocket);

                        SocketMapper.Mapper(clientSocket, tcpClient);

                        ConnectionController?.HandleConnection(tcpClient);

                        resetEvent.Set();
                    });
                }
                else if (socket.Poll(10, SelectMode.SelectError))
                {
                    ConnectionController?.HandleError(this);
                    break;
                }
            }
        }

        private void Handler()
        {
            while (ServerRunning)
            {
                if (clients.Count == 0)
                {
                    resetEvent.Reset();
                    resetEvent.Wait(-1);
                }

                SocketsSelector.Clear();
                SocketsErrors.Clear();

                SocketsSelector.AddRange(clients);
                SocketsErrors.AddRange(clients);

                try
                {

                    Socket.Select(SocketsSelector, null, SocketsErrors, -1);

                    for (var i = 0; i < SocketsSelector.Count; i++)
                    {
                        var socketSpeaker = SocketsSelector[i];
                        var bytes = ConnectionController?.HandleMesage(SocketMapper.GetISocket(socketSpeaker));
                        if (bytes == 0)
                        {
                            SocketMapper.RemoveISocketMapper(socketSpeaker);
                            clients.Remove(socketSpeaker);
                        }
                    }
                }
                catch (Exception ex)
                {
                    for (var i = 0; i < SocketsErrors.Count; i++)
                    {
                        var socketError = SocketsErrors[i];

                        ConnectionController?.HandleError(SocketMapper.GetISocket(socketError));
                        SocketMapper.RemoveISocketMapper(socketError);
                        clients.Remove(socketError);
                    }
                }
            }

            while (ClientRunning)
            {
                if (socket.Poll(-1, SelectMode.SelectRead))
                {
                    ConnectionController?.HandleMesage(null);
                }
            }
        }

        public bool Send(byte[] msg)
        {
            try
            {
                var sendEvent = new SocketAsyncEventArgs();
                sendEvent.SetBuffer(msg, 0, msg.Length);
                socket.SendAsync(sendEvent);

                return true;
            }
            catch (SocketException ex)
            {
                return false;
            }
        }

        public int Read(byte[] buffer)
        {
            return socket.Receive(buffer);
        }

        public void Dispose()
        {
            
        }
    }
}
