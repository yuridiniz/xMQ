using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    internal abstract class ProtocolCommand
    {
        protected bool CanHandler(int handlerCode, Envelope envelop)
        {
            envelop.Move(0);

            var code = envelop.ReadNext<byte>();
            return code == handlerCode;
        }

        public abstract bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop);
    }
}
