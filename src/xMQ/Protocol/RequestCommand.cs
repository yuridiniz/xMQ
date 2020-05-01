using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    internal class RequestCommand : ProtocolCommand
    {
        public const byte CODE = 3;


        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var msgId = envelop.ReadNext<uint>();

            me.OnMessage?.Invoke(envelop.GetMessage(), remote, null);

            return true;
        }
    }

}
