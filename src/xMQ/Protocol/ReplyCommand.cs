using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    internal class ReplyCommand : ProtocolCommand
    {
        public const byte CODE = 3;

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
            var canHandler = CanHandler(CODE, envelop);

            if (!canHandler)
                return false;

            var msgId = envelop.ReadNext<uint>();

            var storedResponseAwaiter = remote.socket.GetStoredResponse();
            if (!storedResponseAwaiter.ContainsKey(msgId))
                return true;

            var responseAwaiter = storedResponseAwaiter[msgId];
            responseAwaiter.Data = envelop;
            responseAwaiter.Signal.Set();

            return true;
        }
    }

}
