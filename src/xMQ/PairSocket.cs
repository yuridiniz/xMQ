﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xMQ.Extension;
using xMQ.Protocol;
using xMQ.SocketsType;
using xMQ.Util;

namespace xMQ
{
    public class PairSocket : ISocketController
    {
        private uint nextMessageId;

        internal ISocket socket;

        private ProtocolHandler protocolHandler;

        public delegate void ClientConnection(PairSocket socket);
        public ClientConnection OnClientConnection;

        public delegate void ClientDisconnect(PairSocket socket);
        public ClientDisconnect OnClientDisconnect;

        public delegate void MessageHandler(Message msg, PairSocket socket);
        public MessageHandler OnMessage;

        internal Dictionary<uint, ResponseAwaiter> StoredResponses { get; }
        internal Dictionary<byte[], PairSocket> IdentitySocketsMap { get; }
        internal Dictionary<ISocket, PairSocket> WrappedSocketsMap { get; }


        public PairSocket()
        {
            protocolHandler = new ProtocolHandler();
            StoredResponses = new Dictionary<uint, ResponseAwaiter>();
            IdentitySocketsMap = new Dictionary<byte[], PairSocket>();
            WrappedSocketsMap = new Dictionary<ISocket, PairSocket>();
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

        private uint GenerateStoredAwaiter()
        {
            uint msgId = 0;
            var responseAwaiter = new ResponseAwaiter();
            responseAwaiter.Signal = new ManualResetEventSlim();

            lock (StoredResponses)
            {
                nextMessageId = nextMessageId % uint.MaxValue;
                msgId = ++nextMessageId;

                StoredResponses[msgId] = responseAwaiter;
            }

            return msgId;
        }

        public bool TryBind(string serverAddress) => Bind(serverAddress, true);

        public void Bind(string serverAddress) => Bind(serverAddress, false);

        private bool Bind(string serverAddress, bool silence)
        {
            Uri serverUri = ValidateAddress(serverAddress);
            var _socket = GetSocketConnectionProtocol(serverUri);

            try
            {
                _socket.Bind();
                socket = _socket;

                return true;
            }
            catch (Exception ex)
            {
                if (!silence)
                    throw ex;

                return false;
            }
        }

        public bool TryConnect(string pairAddress) => Connect(pairAddress, true);

        public void Connect(string pairAddress) => Connect(pairAddress, false);

        private bool Connect(string pairAddress, bool silence)
        {
            Uri pairAddressUri = ValidateAddress(pairAddress);
            var _socket = GetSocketConnectionProtocol(pairAddressUri);

            try
            {
                _socket.Connect();
                socket = _socket;

                return true; 
            }
            catch (Exception ex)
            {
                if(!silence)
                    throw ex;

                return false;
            }
        }

        public bool Send(Envelope envelope)
        {
            return socket.Send(envelope.ToByteArray());
        }

        public bool Send(Message msg)
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            var originalEnvelope = msg.Envelope;
            var envelopeToSend = new Envelope(msg);

            var isReply = false;
            uint msgId = 0;
            if (originalEnvelope != null)
            {
                originalEnvelope.Move(0);
                var command = originalEnvelope.ReadNext<byte>();

                isReply = command == RequestCommand.CODE;

                if(isReply)
                    msgId = originalEnvelope.ReadNext<uint>();
            }

            if(isReply && msgId > 0)
            {
                envelopeToSend.Append(ReplyCommand.CODE);
                envelopeToSend.Append(msgId);
            } else
            {
                envelopeToSend.Append(NoneCommand.CODE);
            }

            return socket.Send(envelopeToSend.ToByteArray());
        }

        public bool Send(string msg)
        {
            var msgPack = new Message();
            msgPack.Append(msg);

            return Send(msgPack);
        }

        public Message Request(Message msg, int millisecondsTimeout = -1)
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            var msgId = GenerateStoredAwaiter();
            var responseAwaiter = StoredResponses[msgId];

            var envelop = new Envelope(msg);
            envelop.Append(RequestCommand.CODE);
            envelop.Append(msgId);

            var networkSuccess = socket.Send(envelop.ToByteArray());
            var msgReceived = responseAwaiter.Signal.Wait(millisecondsTimeout);

            if (networkSuccess && msgReceived)
            {
                var dataEnvelop = responseAwaiter.Data.GetMessage();
                dataEnvelop.Success = true;

                StoredResponses.Remove(msgId);

                return dataEnvelop;
            }
            else
            {
                var errorPackage = new Message();
                errorPackage.Success = false;
                return errorPackage;
            }
        }

        public List<PairSocket> GetAllClients()
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            return WrappedSocketsMap.Values.ToList();
        }

        public PairSocket GetClient<T>(T identifier)
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            var key = GenericBitConverter.GetBytes(identifier);

            PairSocket pairSocket;
            IdentitySocketsMap.TryGetValue(key, out pairSocket);

            return pairSocket;
        }

        public bool Close(Message msg)
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            try
            {
                socket.Close();
                socket = null;

                WrappedSocketsMap.Clear();
                IdentitySocketsMap.Clear();
                StoredResponses.Clear();

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }


        public void AddProtocolCommand(ExtendableProtocolCommand customProtocol)
        {
            var nextCode = protocolHandler.SupportedProtocol.Keys.Max() + 1;

            if (nextCode > byte.MaxValue)
                throw new InvalidOperationException("Limite de protocolos atingido");

            customProtocol.CODE = (byte)nextCode;
            protocolHandler.SupportedProtocol.Add(customProtocol.CODE, customProtocol);
        }


        void ISocketController.OnMessage(ISocket remote, byte[] message)
        {
            PairSocket remotePair = remote != null ? WrappedSocketsMap[remote] : this;

            var envelop = new Envelope(message);
            protocolHandler.HandleMessage(this, remotePair, envelop);
        }

        void ISocketController.OnDisconnect(ISocket remote)
        {
            WrappedSocketsMap.Remove(remote);
        }

        void ISocketController.OnConnected(ISocket remote)
        {
            WrappedSocketsMap[remote] = new PairSocket(remote);
        }

        void ISocketController.OnError(ISocket remote)
        {
        }
    }
}
