using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    internal class ReplyProtocol : ProtocolCommand
    {
        private ReplyProtocol()
        {
        }

        private static ReplyProtocol _command;
        public static ReplyProtocol Command
        {
            get
            {
                if (_command == null) _command = new ReplyProtocol();
                return _command;
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
