using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;

namespace xMQ.Protocol
{
    internal class SubscribeCommand : ProtocolCommand
    {
        public const byte CODE = 7;

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var queueName = envelop.ReadNext<string>();
            var lostMessageType = envelop.ReadNext<byte>();

            PubSubQueue queue;
            lock (me.Queue)
            {
                if (!me.Queue.ContainsKey(queueName))
                {
                    queue = new PubSubQueue();
                    me.Queue[queueName] = queue;
                }
                else
                {
                    queue = me.Queue[queueName];
                }
            }

            var subsConfig = new PairSocketSubscribe(remote, (PubSubQueueLostType) lostMessageType);
            var dropedMessages = queue.AddSubscriber(subsConfig);

            if(dropedMessages != null)
            {
                lock(dropedMessages)
                {
                    while(dropedMessages.Count != 0 )
                    {
                        var msg = dropedMessages[0];
                        dropedMessages.RemoveAt(0);

                        var success = remote.Send(msg);
                        if (!success)
                            subsConfig.AddDropedMessage(msg);
                    }
                }
            }

            return true;
        }
    }
    
}
