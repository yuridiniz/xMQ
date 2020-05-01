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


        private enum ResultCode
        {
            SUCCESS = 0,
            CONFLICT = 1
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            return true;
        }

    }
}
