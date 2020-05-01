using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    /// <summary>
    /// Processa mensagens de comunicação normal
    /// </summary>
    internal class NoneCommand : ProtocolCommand
    {
        public const byte CODE = 0;

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            me.OnMessage?.Invoke(envelop.GetMessage(), remote, null);

            return true;
        }
    }
}
