using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;

namespace xMQ.Protocol
{
    internal class Unsubscribe : ProtocolCommand
    {
        private Unsubscribe()
        {
        }

        private static Unsubscribe _command;
        public static Unsubscribe Command
        {
            get
            {
                if (_command == null) _command = new Unsubscribe();
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
