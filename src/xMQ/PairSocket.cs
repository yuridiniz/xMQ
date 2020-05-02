using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xMQ.Extension;
using xMQ.Protocol;
using xMQ.PubSubProtocol;
using xMQ.SocketsType;
using xMQ.Util;

namespace xMQ
{
    public class PairSocket : SocketProtocolController
    {
        private uint nextMessageId;

        private ProtocolHandler protocolHandler;

        public delegate void ClientConnection(PairSocket socket);
        public ClientConnection OnClientConnection;

        public delegate void ClientDisconnect(PairSocket socket);
        public ClientDisconnect OnClientDisconnect;

        public delegate void MessageHandler(Message msg, PairSocket socket, MessageData queue);
        public MessageHandler OnMessage;

        internal ISocket Socket { get => socket; }

        internal Dictionary<ISocket, PairSocket> WrappedSocketsMap { get; }

        internal Dictionary<uint, ResponseAwaiter> StoredResponses { get; }
        internal Dictionary<byte[], PairSocket> IdentitySocketsMap { get; }
        internal Dictionary<PairSocket, byte[]> IdentityConnectionSocketsMap { get; }

        internal Dictionary<string, PubSubQueue> Queue { get; }
        public Guid ConnectionId { get; internal set; }

        internal ManualResetEventSlim idAwaiter = new ManualResetEventSlim(false);

        public PairSocket()
        {
            protocolHandler = new ProtocolHandler();
            StoredResponses = new Dictionary<uint, ResponseAwaiter>();
            IdentitySocketsMap = new Dictionary<byte[], PairSocket>();
            IdentityConnectionSocketsMap = new Dictionary<PairSocket, byte[]>();
            WrappedSocketsMap = new Dictionary<ISocket, PairSocket>();
            Queue = new Dictionary<string, PubSubQueue>();

            //Task.Run(() => { KeepAlive(); });
            //TODO Start Queue Cleaner
        }

        public PairSocket(Guid identifier)
            :this()
        {
            ConnectionId = identifier;
        }

        internal PairSocket(ISocket _socket)
            :this()
        {
            socket = _socket;
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

        private void InitProtocolJobs()
        {
            //Task.Run(() => { KeepAlive(); });
            //TODO Start Queue Cleaner
        }

        private void StopProtocolJobs()
        {
            //Para os processos iniciados por InitProtocolJobs
        }

        public override bool TryBind(string serverAddress)
        {
            if (!base.TryBind(serverAddress))
                return false;

            try
            {
                InitProtocolJobs();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public override void Bind(string serverAddress)
        {
            base.Bind(serverAddress);
            InitProtocolJobs();
        }

        public override bool TryConnect(string pairAddress, int timeout = 1000)
        {
            if (!base.TryConnect(pairAddress, timeout))
                return false;

            try
            {
                InitProtocolJobs();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public override void Connect(string pairAddress, int timeout = 1000)
        {
            base.Connect(pairAddress, timeout);
            SendConnectionIdentification();
            InitProtocolJobs();
        }
      
        private void SendConnectionIdentification()
        {
            var msg = new Message();
            if(ConnectionId != Guid.Empty)
                msg.Append(ConnectionId);

            var envelope = new Envelope(msg);
            msg.Append(IdentityCommand.CODE);

            if (!socket.Send(envelope.ToByteArray()))
                throw new Exception("Não foi possível realizar a identificação");

            idAwaiter.Wait(-1);
        }

        protected bool Send(Envelope envelope)
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

        public bool Publish(string queue, Message msg)
        {
            var msgPack = new Envelope(msg);
            msgPack.Append(PublishCommand.CODE);
            msgPack.Append(queue);

            return socket.Send(msgPack.ToByteArray());
        }

        public bool Subscribe(string queue, PubSubQueueLostType lostType)
        {
            var msgPack = new Envelope();
            msgPack.Append(SubscribeCommand.CODE);
            msgPack.Append(queue);
            msgPack.Append((byte) lostType);

            return socket.Send(msgPack.ToByteArray());
        }

        public bool Unsubscribe(string queue)
        {
            var msgPack = new Envelope();
            msgPack.Append(SubscribeCommand.CODE);
            msgPack.Append(queue);

            return socket.Send(msgPack.ToByteArray());
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

        internal Envelope Read()
        {
            try
            {
                byte[] buffer = new byte[2];

                var bytesReceived = socket.Read(buffer);
                if (bytesReceived == 0)
                    return null;

                var length = BitConverter.ToInt16(buffer, 0);

                byte[] msg = new byte[length];
                bytesReceived = socket.Read(msg);

                return new Envelope(msg);
            }
            catch (Exception ex)
            {
                return null;
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

        public bool Close()
        {
            if (socket == null)
                throw new NotSupportedException("There is no connection established, use the Connect() or Bind() method to initiate a connection");

            try
            {
                socket.Close();
                socket.Dispose();
                socket = null;

                return true;
            }
            catch (SocketException)
            {
                socket.Dispose();
                socket = null;
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

        protected override void OnRemoteMessage(ISocket remote)
        {
            PairSocket remotePair = remote != null ? WrappedSocketsMap[remote] : this;

            var envelope = remotePair.Read();

            if (envelope == null)
            {
                OnClientDisconnect?.Invoke(remotePair);
                Close();
            }
            else
            {
                protocolHandler.HandleMessage(this, remotePair, envelope);
            }
        }

        protected override void OnRemoteConnected(ISocket remote)
        {
            WrappedSocketsMap[remote] = new PairSocket(remote);
        }

        protected override void OnError(ISocket remote)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            WrappedSocketsMap.Clear();
            IdentitySocketsMap.Clear();
            StoredResponses.Clear();
            StopProtocolJobs();
            socket?.Dispose();
            socket = null;
        }
    }
}
