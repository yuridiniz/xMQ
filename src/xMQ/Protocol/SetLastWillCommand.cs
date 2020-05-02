using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;

namespace xMQ.Protocol
{
    internal class SetLastWillCommand : ProtocolCommand
    {
        private SetLastWillCommand()
        {
        }

        private static SetLastWillCommand _command;
        public static SetLastWillCommand Command
        {
            get
            {
                if (_command == null) _command = new SetLastWillCommand();
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
            lastWillEnvelop.Append(PublishDeliveredCommand.Command);
            lastWillEnvelop.Append(queue);
            lastWillEnvelop.Append((byte)PubSubQueueLostType.None);

            remote.LastWill = lastWillEnvelop;

            return true;
        }
    }
    
}
