using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;

namespace xMQ.Protocol
{
    internal class PublishCommand : ProtocolCommand
    {
        public const byte CODE = 5;

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var queue = envelop.ReadNext<string>();

            PubSubQueue pubSubQueue;
            lock (me.Queue)
            {
                if (!me.Queue.ContainsKey(queue))
                    return true;

                pubSubQueue = me.Queue[queue];
            }

            var queueEvenlop = new Envelope(envelop.GetMessage());

            foreach (var item in pubSubQueue.GetClients())
            {
                var success = item.PairSocket.Send(queueEvenlop);
                if (!success && item.LostType == PubSubQueueLostType.Persitent)
                    item.AddDropedMessage(queueEvenlop);
            }

            pubSubQueue.LastMessage = queueEvenlop;

            return true;
        }
      
    }
    
}
