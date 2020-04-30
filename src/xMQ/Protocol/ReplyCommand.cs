using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    internal class ReplyCommand : ProtocolCommand
    {
        public const byte CODE = 4;

        private static ReplyCommand _handlerInstance = null;
        public static ReplyCommand Handler
        {
            get
            {
                if (_handlerInstance == null)
                    _handlerInstance = new ReplyCommand();

                return _handlerInstance;
            }
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var msgId = envelop.ReadNext<uint>();

            if (!remote.StoredResponses.ContainsKey(msgId))
                return true;

            var responseAwaiter = remote.StoredResponses[msgId];
            responseAwaiter.Data = envelop;
            responseAwaiter.Signal.Set();

            return true;
        }
    }

}
