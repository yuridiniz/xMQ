using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;

namespace xMQ.Protocol
{
    internal class SubscribeCommand : ProtocolCommand
    {
        private SubscribeCommand()
        {
        }

        private static SubscribeCommand _command;
        public static SubscribeCommand Command
        {
            get
            {
                if (_command == null) _command = new SubscribeCommand();
                return _command;
            }
        }

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
                    queue.Name = queueName;

                    me.Queue[queueName] = queue;
                }
                else
                {
                    queue = me.Queue[queueName];
                }
            }

            List<PubSubQueue> queuesByPair;
            lock (me.SubscriberSockets)
            {
                if (!me.SubscriberSockets.ContainsKey(remote))
                {
                    queuesByPair = new List<PubSubQueue>();
                    me.SubscriberSockets[remote] = queuesByPair;
                } else
                {
                    queuesByPair = me.SubscriberSockets[remote];
                }
            }

            var subsConfig = new PairSocketSubscribe(remote, (PubSubQueueLostType) lostMessageType);

            bool hasConfiguration = false;
            var dropedMessages = queue.AddSubscriber(subsConfig, out hasConfiguration);

            // Caso seja uma nova fila para o usuário adiciona na lista de filas que ele está
            if(!hasConfiguration)
                queuesByPair.Add(queue);

            if (dropedMessages != null)
            {
                lock(dropedMessages)
                {
                    while(dropedMessages.Count != 0 )
                    {
                        var msg = dropedMessages[0];
                        dropedMessages.RemoveAt(0);

                        var success = remote.Socket.Send(msg.ToByteArray());
                        if (!success)
                            subsConfig.AddDropedMessage(msg);
                    }
                }
            }

            return true;
        }
    }
    
}
