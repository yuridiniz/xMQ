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

        private ManualResetEvent resetEvent;
        private Socket socket;
        private List<Socket> clients;

        public Uri UriAddress { get; }

        public SocketProtocolController ConnectionController { get; }

        public TcpSocket()
        {
            resetEvent = new ManualResetEvent(false);
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
            var delay = (int)TimeSpan.FromSeconds(1).TotalMilliseconds * 1000;

            while (ServerRunning)
            {
                if (socket.Poll(delay, SelectMode.SelectRead))
                {
                    var clientSocket = socket.Accept();
                    var tcpClient = new TcpSocket(clientSocket);

                    SocketMapper.Mapper(clientSocket, tcpClient);

                    ConnectionController?.HandleConnection(tcpClient);

                    clients.Add(clientSocket);
                    resetEvent.Set();

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
                    resetEvent.WaitOne(-1);

                var socketSelector = new List<Socket>(clients);
                Socket.Select(socketSelector, null, null, -1);

                for (var i = 0; i < socketSelector.Count; i++)
                {
                    var socketSpeaker = socketSelector[i];
                    ConnectionController?.HandleMesage(SocketMapper.GetISocket(socketSpeaker));
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
                socket.Send(msg);
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
