using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ.PubSubProtocol
{
    internal class PairSocketSubscribe
    {
        private const int LIMIT_DROPED_SIZE = 1024 * 1024; // 1MB

        public PairSocket PairSocket { get; internal set; }

        public PubSubQueueLostType LostType { get; }

        private List<Envelope> DropedMessages { get; } = new List<Envelope>();
        private int dropedLength;

        public PairSocketSubscribe(PairSocket pairSocket, PubSubQueueLostType persistent)
        {
            PairSocket = pairSocket;
        }

        internal List<Envelope> GetDropedMessages()
        {
            return DropedMessages;
        }

        internal void AddDropedMessage(Envelope envelope)
        {
            //TODO Remover mensagens antigas caso o limite da fila estoure?

            if (envelope.Length + dropedLength <= LIMIT_DROPED_SIZE)
                DropedMessages.Add(envelope);
        }

    }
}
