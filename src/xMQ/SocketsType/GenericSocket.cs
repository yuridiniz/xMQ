using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace xMQ.SocketsType
{
    public abstract class GenericSocket
    {
        private uint nextMessageId;

        internal Dictionary<uint, ResponseAwaiter> StoredResponses { get; }
        internal Dictionary<byte[], PairSocket> IdentitySocketsMap { get; }

        public bool ServerRunning { get; set; }
        public bool ClientRunning { get; set; }

        public GenericSocket()
        {
            StoredResponses = new Dictionary<uint, ResponseAwaiter>();
            IdentitySocketsMap = new Dictionary<byte[], PairSocket>();
        }

        internal uint GenerateStoredAwaiter()
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

    }
}
