using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xMQ.Protocol;
using xMQ.SocketsType;

namespace xMQ
{
    public class PairSocket : IController
    {
        internal ISocket socket;

        private ProtocolHandler protocolHandler;

        public delegate void ClientConnection(PairSocket socket);
        public ClientConnection OnClientConnection;

        public delegate void ClientDisconnect(PairSocket socket);
        public ClientDisconnect OnClientDisconnect;

        public delegate void MessageHandler(Message msg, PairSocket socket);
        public MessageHandler OnMessage;

        public PairSocket()
        {
            protocolHandler = new ProtocolHandler();
        }

        internal PairSocket(ISocket _socket)
            :this()
        {
            socket = _socket;
        }

        private Uri ValidateAddress(string serverAddress)
        {
            if (socket != null)
                throw new NotSupportedException("Unable to start another connection on a connection that has already started");

            Uri serverUri;
            if (!Uri.TryCreate(serverAddress, UriKind.Absolute, out serverUri))
                throw new ArgumentException("serverAddress has a invalid value, use the format protocol://ip:port. Exemple: tcp://127.0.0.1:5000, see documentations for more protocol information");

            if (serverUri.Port <= 0 || serverUri.Port > 65535)
                throw new ArgumentException("Port range is not valid, enter a value between 1 and 65535");

            return serverUri;
        }

        private ISocket GetSocketConnectionProtocol(Uri serverUri)
        {
            ISocket socketConnection;
            if (serverUri.Scheme == TcpSocket.SCHEME)
            {
                socketConnection = new TcpSocket(serverUri, this);
            }
            else if (serverUri.Scheme == UdpSocket.SCHEME)
            {
                socketConnection = new UdpSocket(serverUri);
            }
            else if (serverUri.Scheme == TcpSocket.SCHEME)
            {
                socketConnection = new IpcSocket(serverUri.Host);
            }
            else
            {
                throw new NotSupportedException($"Scheme '{serverUri.Scheme}' is not valid, see documentations for more protocol information");
            }

            return socketConnection;
        }

        public bool TryBind(string serverAddress)
        {
            Uri serverUri = ValidateAddress(serverAddress);
            socket = GetSocketConnectionProtocol(serverUri);

            return Bind(socket, true);
        }

        public void Bind(string serverAddress)
        {
            Uri serverUri = ValidateAddress(serverAddress);
            socket = GetSocketConnectionProtocol(serverUri);

            Bind(socket, false);
        }

        private bool Bind(ISocket socketConnection, bool silence)
        {
            try
            {
                socketConnection.Bind();
                return true;
            }
            catch (Exception ex)
            {
                if (!silence)
                    throw ex;

                return false;
            }
        }

        public bool TryConnect(string pairAddress)
        {
            Uri pairAddressUri = ValidateAddress(pairAddress);
            socket = GetSocketConnectionProtocol(pairAddressUri);

            return Connect(socket, true);
        }

        public void Connect(string pairAddress)
        {
            Uri pairAddressUri = ValidateAddress(pairAddress);
            socket = GetSocketConnectionProtocol(pairAddressUri);

            Connect(socket, false);
        }

        private bool Connect(ISocket socketConnection, bool silence)
        {
            try
            {
                socketConnection.Connect();
                return true; 
            }
            catch (Exception ex)
            {
                if(!silence)
                    throw ex;

                return false;
            }
        }

        public bool Send(Message msg)
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            try
            {
                socket.Send(msg);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public bool Send(string msg)
        {
            var msgPack = new Message();
            msgPack.Append(msg);

            return Send(msgPack);
        }

        public Message Request(Message msg)
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            return socket.Request(msg);
        }

        public List<PairSocket> GetAllClients()
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            return socket.GetAllClients();
        }

        public PairSocket GetClient<T>(T identifier)
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            return socket.GetClient(identifier);
        }

        public bool Close(Message msg)
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            try
            {
                socket.Close();
                return true;
            }
            catch (SocketException ex)
            {
                return false;
            }
        }

        void IController.OnMessage(PairSocket remote, Envelope envelop)
        {
            protocolHandler.HandleMessage(this, remote ?? this, envelop);
        }

        void IController.OnDisconnect(PairSocket remote)
        {
        }

        void IController.OnConnected(PairSocket remote)
        {
        }

        internal static PairSocket Wrapper(ISocket genericSocket)
        {
            return new PairSocket(genericSocket);
        }
    }
}
