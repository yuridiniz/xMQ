using System;
using System.Collections.Generic;
using System.Text;
using xMQ.PubSubProtocol;
using xMQ.Util;

namespace xMQ.Protocol
{
    internal class PublishDeliveredCommand : ProtocolCommand
    {
        private PublishDeliveredCommand()
        {
        }

        private static PublishDeliveredCommand _command;
        public static PublishDeliveredCommand Command
        {
            get
            {
                if (_command == null) _command = new PublishDeliveredCommand();
                return _command;
            }
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var queueName = envelop.ReadNext<string>();
            var lostType = envelop.ReadNext<byte>();

            var queueData = new MessageData();
            queueData.Queue = queueName;
            queueData.IsLost = lostType != (byte)PubSubQueueLostType.None;

            if(queueData.IsLost)
            {
                var dateTime = envelop.ReadNext<long>();
                queueData.SendDate = DateConverter.ConvertFromUnixTimestamp(dateTime);
            }

            me.OnMessage?.Invoke(envelop.GetMessage(), remote, queueData);

            return true;
        }
    }
    
}
