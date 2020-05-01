using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;

namespace xMQ.Protocol
{
    internal class UnsubscribeCommand : ProtocolCommand
    {
        public const byte CODE = 8;

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var queueName = envelop.ReadNext<string>();

            PubSubQueue queue;
            lock (me.Queue)
            {
                if (me.Queue.ContainsKey(queueName))
                {
                    queue = me.Queue[queueName];

                    lock (queue)
                    {
                        queue.RemoveSubscriber(remote, true);
                        if (queue.CanDispose)
                            me.Queue.Remove(queueName);
                    }
                }
            }

            return true;
        }
    }
    
}
