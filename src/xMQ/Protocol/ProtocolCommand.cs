using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    public abstract class ProtocolCommand
    {
        public byte CODE { get; internal set; }

        public abstract bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelope);
    }
}
