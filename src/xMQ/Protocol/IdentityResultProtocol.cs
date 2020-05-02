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
    internal class IdentityResult : ProtocolCommand
    {
        private IdentityResult()
        {
        }

        private static IdentityResult _command;
        public static IdentityResult Command
        {
            get
            {
                if (_command == null) _command = new IdentityResult();
                return _command;
            }
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            me.ConnectionId = envelop.ReadNext<Guid>();
            me.idAwaiter.Set();

            return true;
        }

    }
}
