using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    internal class Reply : ProtocolCommand
    {
        private Reply()
        {
        }

        private static Reply _command;
        public static Reply Command
        {
            get
            {
                if (_command == null) _command = new Reply();
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
