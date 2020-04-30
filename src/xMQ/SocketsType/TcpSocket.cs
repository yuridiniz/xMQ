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

        private Dictionary<Socket, PairSocket> WrappedSocketsMap { get; set; }

        public IController ConnectionController { get; }

        public TcpSocket()
        {
            resetEvent = new ManualResetEvent(false);
            clients = new List<Socket>();
            WrappedSocketsMap = new Dictionary<Socket, PairSocket>();

        }

        public TcpSocket(Socket _socket)
           : this()
        {
            socket = _socket;
        }

        public TcpSocket(Uri uri, IController connectionController)
          : this()
        {
            UriAddress = uri;
            ConnectionController = connectionController;
        }

        public TcpSocket(Socket _socket, IController connectionController)
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

        public bool Connect()
        {
            try
            {
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(IPAddress.Parse(UriAddress.Host), UriAddress.Port));

                ClientRunning = true;

                Task.Run(() => { Handler(); });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Close()
        {
            socket.Close();
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
                    var pairSocket = PairSocket.Wrapper(tcpClient);

                    WrappedSocketsMap[clientSocket] = pairSocket;

                    clients.Add(clientSocket);
                    resetEvent.Set();

                    ConnectionController?.OnConnected(pairSocket);
                }
                else if (socket.Poll(10, SelectMode.SelectError))
                {
                    break;
                }
            }
        }

        public bool Send(Message msg)
        {
            try
            {
                var originalEnvelop = msg.Envelope;
                originalEnvelop.Move(0);
                var command = originalEnvelop.ReadNext<byte>();

                var isReply = command == RequestCommand.CODE;

                var envelopToSend = new Envelope(msg);

                if (isReply)
                {
                    var msgId = originalEnvelop.ReadNext<uint>();

                    envelopToSend.Append(ReplyCommand.CODE);
                    envelopToSend.Append(msgId);
                }
                else
                {
                    envelopToSend.Append(NoneCommand.CODE);
                }

                socket.Send(envelopToSend.ToByteArray());

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Message Request(Message msg, int millisecondsTimeout = -1)
        {
            var envelop = new Envelope(msg);
            envelop.Append(RequestCommand.CODE);

            var msgId = GenerateStoredAwaiter();
            var responseAwaiter = StoredResponses[msgId];

            envelop.Append(msgId);

            bool success = false;

            try
            {
                socket.Send(envelop.ToByteArray());
                success = responseAwaiter.Signal.Wait(millisecondsTimeout);
            }
            catch (Exception ex)
            {

            }

            if (success)
            {
                var responseEnvelop = responseAwaiter.Data;
                var dataEnvelop = responseEnvelop.GetMessage();
                dataEnvelop.Success = true;

                StoredResponses.Remove(msgId);
                return dataEnvelop;

            }
            else
            {
                var errorMsg = new Message();
                errorMsg.Success = false;

                return errorMsg;
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

                    var msg = new Envelope(bytes);
                    if (msg.Length > 0)
                    {
                        ConnectionController?.OnMessage(WrappedSocketsMap[socketSpeaker], msg);
                    }
                    else
                    {
                        //Notify Disconnect
                        socketSpeaker.Close();
                        ConnectionController.OnDisconnect(WrappedSocketsMap[socketSpeaker]);
                        clients.Remove(socketSpeaker);
                        WrappedSocketsMap.Remove(socketSpeaker);
                    }
                }
            }

            while (ClientRunning)
            {
                if (socket.Poll(-1, SelectMode.SelectRead))
                {
                    var bytes = ReceiveBytes(socket, 100);
                    var msg = new Envelope(bytes);
                    if (msg.Length > 0)
                    {
                        ConnectionController?.OnMessage(null, msg);
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

        internal static int GetLength(Socket thisSocket)
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

        internal static byte[] ReceiveBytes(Socket thisSocket, int timeout)
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

        public List<PairSocket> GetAllClients()
        {
            return WrappedSocketsMap.Values.ToList();
        }

        public PairSocket GetClient<T>(T identifier)
        {
            var bytes = GenericBitConverter.GetBytes(identifier);

            PairSocket result = null;
            IdentitySocketsMap.TryGetValue(bytes, out result);

            return result;
        }

        public Dictionary<uint, ResponseAwaiter> GetStoredResponse()
        {
            return StoredResponses;
        }

        public Dictionary<byte[], PairSocket> GetIdentitySocketsMap()
        {
            return IdentitySocketsMap;
        }
    }
}
