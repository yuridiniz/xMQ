using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;

namespace xMQ.Protocol
{
    internal class UnsubscribeProtocol : ProtocolCommand
    {
        private UnsubscribeProtocol()
        {
        }

        private static UnsubscribeProtocol _command;
        public static UnsubscribeProtocol Command
        {
            get
            {
                if (_command == null) _command = new UnsubscribeProtocol();
                return _command;
            }
        }

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
