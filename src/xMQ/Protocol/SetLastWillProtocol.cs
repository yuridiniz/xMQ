using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;

namespace xMQ.Protocol
{
    internal class SetLastWillProtocol : ProtocolCommand
    {
        private SetLastWillProtocol()
        {
        }

        private static SetLastWillProtocol _command;
        public static SetLastWillProtocol Command
        {
            get
            {
                if (_command == null) _command = new SetLastWillProtocol();
                return _command;
            }
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var queueName = envelop.ReadNext<string>();

            PubSubQueue queue;
            lock (me.Queue)
            {
                if (!me.Queue.ContainsKey(queueName))
                {
                    queue = new PubSubQueue();
                    queue.Name = queueName;

                    me.Queue[queueName] = queue;
                }
                else
                {
                    queue = me.Queue[queueName];
                }
            }

            var lastWillEnvelop = new Envelope(envelop.GetMessage());
            lastWillEnvelop.Append(PublishDeliveredProtocol.Command);
            lastWillEnvelop.Append(queue);
            lastWillEnvelop.Append((byte)PubSubQueueLostType.None);

            remote.LastWill = lastWillEnvelop;

            return true;
        }
    }
    
}
