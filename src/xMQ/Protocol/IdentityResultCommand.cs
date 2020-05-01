using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    /// <summary>
    /// Implementação para processamento de mensagem para identificação dos clientes
    /// </summary>
    internal class IdentityResultCommand : ProtocolCommand
    {
        public const byte CODE = 2;

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            me.ConnectionId = envelop.ReadNext<Guid>();
            me.idAwaiter.Set();

            return true;
        }

    }
}
