using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;
using xMQ.Util;

namespace xMQ.Protocol
{
    internal class PublishCommand : ProtocolCommand
    {
        private PublishCommand()
        {
        }

        private static PublishCommand _command;
        public static PublishCommand Command
        {
            get
            {
                if (_command == null) _command = new PublishCommand();
                return _command;
            }
        }

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

            var senderDate = DateConverter.ConvertToUnixTimestamp(DateTime.Now);

            var queueEnvelop = new Envelope(envelop.GetMessage());
            queueEnvelop.Append(PublishDeliveredCommand.Command);
            queueEnvelop.Append(queue);
            queueEnvelop.Append((byte)PubSubQueueLostType.None);

            foreach (var item in pubSubQueue.GetClients())
            {
                if (item.PairSocket == remote)
                    continue;

                item.PairSocket.Socket.Send(queueEnvelop.ToByteArray());
            }

            var lastMsg = new Envelope(envelop.GetMessage());
            queueEnvelop.Append(PublishDeliveredCommand.Command);
            queueEnvelop.Append(queue);
            queueEnvelop.Append((byte)PubSubQueueLostType.LastMessage);
            queueEnvelop.Append(senderDate);

            pubSubQueue.LastMessage = lastMsg;

            return true;
        }
      
    }
    
}
