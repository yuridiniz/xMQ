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

        public ISocketController ConnectionController { get; }
        internal Dictionary<Socket, ISocket> MappedInstance { get; }

        public TcpSocket()
        {
            resetEvent = new ManualResetEvent(false);
            clients = new List<Socket>();
            MappedInstance = new Dictionary<Socket, ISocket>();
        }

        public TcpSocket(Socket _socket)
           : this()
        {
            socket = _socket;
        }

        public TcpSocket(Uri uri, ISocketController connectionController)
          : this()
        {
            UriAddress = uri;
            ConnectionController = connectionController;
        }

        public TcpSocket(Socket _socket, ISocketController connectionController)
            :this(_socket)
        {
            ConnectionController = connectionController;
        }

        public void Bind()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            socket.Bind(new IPEndPoint(IPAddress.Parse(UriAddress.Host), UriAddress.Port));

            ServerRunning = true;

            Task.Run(() => { Listen(); });
            Task.Run(() => { Handler(); });
            //Task.Run(() => { KeepAlive(); });

            socket.Listen((int)SocketOptionName.MaxConnections);
        }

        public void Connect()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);

            socket.Connect(new IPEndPoint(IPAddress.Parse(UriAddress.Host), UriAddress.Port));

            ClientRunning = true;

            Task.Run(() => { Handler(); });
        }

        public void Close()
        {
            socket.Close();
            clients.Clear();
            MappedInstance.Clear();

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

                    MappedInstance[clientSocket] = tcpClient;

                    ConnectionController?.OnConnected(tcpClient);

                    clients.Add(clientSocket);
                    resetEvent.Set();

                }
                else if (socket.Poll(10, SelectMode.SelectError))
                {
                    ConnectionController?.OnError(this);
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

                    var bytes = ReceiveBytes(socketSpeaker, 1);
                    if (bytes.Length > 0)
                    {
                        ConnectionController?.OnMessage(MappedInstance[socketSpeaker], bytes);
                    }
                    else
                    {
                        //Notify Disconnect
                        socketSpeaker.Close();
                        ConnectionController.OnDisconnect(MappedInstance[socketSpeaker]);
                        clients.Remove(socketSpeaker);
                        MappedInstance.Remove(socketSpeaker);
                    }
                }
            }

            while (ClientRunning)
            {
                if (socket.Poll(-1, SelectMode.SelectRead))
                {
                    var bytes = ReceiveBytes(socket, 100);
                    if (bytes.Length > 0)
                    {
                        ConnectionController?.OnMessage(null, bytes);
                    }
                    else
                    {
                        //Notify Disconnect
                        ClientRunning = false;
                        ConnectionController?.OnDisconnect(null);
                    }
                }

            }
        }

        private int GetLength(Socket thisSocket)
        {
            try
            {
                byte[] buffer = new byte[2];

                var bytesReceived = thisSocket.Receive(buffer);
                if (bytesReceived == 0)
                    return bytesReceived;

                var length = BitConverter.ToInt16(buffer, 0);

                return length;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private byte[] ReceiveBytes(Socket thisSocket, int timeout)
        {
            try
            {
                if (!thisSocket.Poll(timeout, SelectMode.SelectRead))
                    return new byte[0];

                var len = GetLength(thisSocket);
                byte[] msg = new byte[len];
                var bytesReceived = thisSocket.Receive(msg);

                if (bytesReceived == 0)
                    return new byte[0];

                return msg;
            }
            catch (Exception)
            {
                return new byte[0];
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
    }
}
