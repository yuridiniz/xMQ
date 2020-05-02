using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xMQ.Protocol;
using xMQ.PubSubProtocol;
using xMQ.SocketsType;
using xMQ.Util;

namespace xMQ
{
    public class PairSocket : SocketProtocolController
    {
        private uint nextMessageId;

        public delegate void ClientConnection(PairSocket socket);
        public ClientConnection OnClientConnection;

        public delegate void ClientDisconnect(PairSocket socket);
        public ClientDisconnect OnClientDisconnect;

        public delegate void MessageHandler(Message msg, PairSocket socket, MessageData queue);
        public MessageHandler OnMessage;

        internal Dictionary<ISocket, PairSocket> WrappedSocketsMap { get; }

        internal Dictionary<uint, ResponseAwaiter> StoredResponses { get; }
        internal Dictionary<byte[], PairSocket> IdentitySocketsMap { get; }
        internal Dictionary<PairSocket, byte[]> IdentityConnectionSocketsMap { get; }

        internal Dictionary<string, PubSubQueue> Queue { get; }
        internal Dictionary<PairSocket, List<PubSubQueue>> SubscriberSockets { get; }

        internal ManualResetEventSlim idAwaiter = new ManualResetEventSlim(false);

        public Guid ConnectionId { get; internal set; }
        internal Envelope LastWill { get; set; }

        public PairSocket()
        {
            StoredResponses = new Dictionary<uint, ResponseAwaiter>();
            IdentitySocketsMap = new Dictionary<byte[], PairSocket>();
            IdentityConnectionSocketsMap = new Dictionary<PairSocket, byte[]>();
            WrappedSocketsMap = new Dictionary<ISocket, PairSocket>();
            Queue = new Dictionary<string, PubSubQueue>();
            SubscriberSockets = new Dictionary<PairSocket, List<PubSubQueue>>();

            InitSupportedProtocolHeader();

            //Task.Run(() => { KeepAlive(); });
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


        private void InitSupportedProtocolHeader()
        {
            AddProtocolCommand(NoneProtocol.Command);
            AddProtocolCommand(IdentityProtocol.Command);
            AddProtocolCommand(IdentityResultProtocol.Command);
            AddProtocolCommand(RequestProtocol.Command);
            AddProtocolCommand(ReplyProtocol.Command);
            AddProtocolCommand(PublishProtocol.Command);
            AddProtocolCommand(PublishDeliveredProtocol.Command);
            AddProtocolCommand(SubscribeProtocol.Command);
            AddProtocolCommand(UnsubscribeProtocol.Command);
            AddProtocolCommand(SetLastWillProtocol.Command);
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
                SendConnectionIdentification();
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
            msg.Append(IdentityProtocol.Command);

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
                return false;

            var originalEnvelope = msg.Envelope;
            var envelopeToSend = new Envelope(msg);

            var isReply = false;
            uint msgId = 0;
            if (originalEnvelope != null)
            {
                originalEnvelope.Move(0);
                var command = originalEnvelope.ReadNext<int>();

                isReply = command == RequestProtocol.Command;

                if(isReply)
                    msgId = originalEnvelope.ReadNext<uint>();
            }

            if(isReply && msgId > 0)
            {
                envelopeToSend.Append(ReplyProtocol.Command);
                envelopeToSend.Append(msgId);
            } else
            {
                envelopeToSend.Append(NoneProtocol.Command);
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
            msgPack.Append(PublishProtocol.Command);
            msgPack.Append(queue);

            return socket.Send(msgPack.ToByteArray());
        }

        public bool Subscribe(string queue, PubSubQueueLostType lostType)
        {
            var msgPack = new Envelope();
            msgPack.Append(SubscribeProtocol.Command);
            msgPack.Append(queue);
            msgPack.Append((byte) lostType);

            return socket.Send(msgPack.ToByteArray());
        }

        public bool Unsubscribe(string queue)
        {
            var msgPack = new Envelope();
            msgPack.Append(SubscribeProtocol.Command);
            msgPack.Append(queue);

            return socket.Send(msgPack.ToByteArray());
        }

        public bool SetLastWill(string queue, Message msg)
        {
            var msgPack = new Envelope(msg);
            msgPack.Append(SetLastWillProtocol.Command);
            msgPack.Append(queue);

            return socket.Send(msgPack.ToByteArray());
        }

        public Message Request(Message msg, int millisecondsTimeout = 500)
        {
            if (socket == null)
            {
                var errorPackage = new Message();
                errorPackage.Success = false;
                return errorPackage;
            }

            var msgId = GenerateStoredAwaiter();
            var responseAwaiter = StoredResponses[msgId];

            var envelop = new Envelope(msg);
            envelop.Append(RequestProtocol.Command);
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
            return WrappedSocketsMap.Values.ToList();
        }

        public PairSocket GetClient<T>(T identifier)
        {
            var key = GenericBitConverter.GetBytes(identifier);

            PairSocket pairSocket;
            IdentitySocketsMap.TryGetValue(key, out pairSocket);

            return pairSocket;
        }

        protected override int OnRemoteMessage(ISocket remote)
        {
            PairSocket remotePair = remote != null ? WrappedSocketsMap[remote] : this;

            var envelope = remotePair.Read();

            if (envelope == null)
            {
                HandleDisconnect(remotePair);
                return 0;
            }
            else
            {
                ProtocolHandler.HandleMessage(this, remotePair, envelope);
                return envelope.Length;
            }
        }

        private void HandleDisconnect(PairSocket remotePair)
        {
            if (remotePair.LastWill != null)
                ProtocolHandler.HandleMessage(this, remotePair, remotePair.LastWill);


            //Remove subscribers do cliente caso não seja do tipo Persistent
            List<PubSubQueue> queuesByPair;
            if (SubscriberSockets.TryGetValue(remotePair, out queuesByPair))
            {
                for (var i = 0; i < queuesByPair.Count; i++)
                {
                    var queue = queuesByPair[i];
                    queue.RemoveSubscriber(remotePair, false);
                    if (queue.CanDispose)
                        Queue.Remove(queue.Name);

                }
            }

            //Caso seja um cliente, libera os recursos alocados por ele
            if(remotePair != this)
            {
                WrappedSocketsMap.Remove(remotePair.Socket);
                IdentityConnectionSocketsMap.Remove(remotePair);
                remotePair.Dispose();
            }

            remotePair.Close();

            OnClientDisconnect?.Invoke(remotePair);
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
            IdentityConnectionSocketsMap.Clear();
            StoredResponses.Clear();
            StopProtocolJobs();
            socket?.Dispose();
            socket = null;
        }
    }
}
