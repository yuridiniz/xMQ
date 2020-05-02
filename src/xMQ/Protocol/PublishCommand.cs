﻿using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;
using xMQ.Util;

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

            var senderDate = DateConverter.ConvertToUnixTimestamp(DateTime.Now);

            var queueEnvelop = new Envelope(envelop.GetMessage());
            queueEnvelop.Append(MsgPublishedCommand.CODE);
            queueEnvelop.Append(queue);
            queueEnvelop.Append((byte)PubSubQueueLostType.None);

            foreach (var item in pubSubQueue.GetClients())
            {
                if (item.PairSocket == remote)
                    continue;

                item.PairSocket.Socket.Send(queueEnvelop.ToByteArray());
            }

            var lastMsg = new Envelope(envelop.GetMessage());
            queueEnvelop.Append(MsgPublishedCommand.CODE);
            queueEnvelop.Append(queue);
            queueEnvelop.Append((byte)PubSubQueueLostType.LastMessage);
            queueEnvelop.Append(senderDate);

            pubSubQueue.LastMessage = lastMsg;

            return true;
        }
      
    }
    
}
