using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ.Protocol
{
    internal class MsgPublishedCommand : ProtocolCommand
    {
        public const byte CODE = 6;

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var msgId = envelop.ReadNext<uint>();

            me.OnMessage?.Invoke(envelop.GetMessage(), remote);

            return true;
        }
    }
    
}
