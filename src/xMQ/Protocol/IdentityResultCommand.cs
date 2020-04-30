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

        private static RequestCommand _handlerInstance = null;
        public static RequestCommand Handler
        {
            get
            {
                if (_handlerInstance == null)
                    _handlerInstance = new RequestCommand();

                return _handlerInstance;
            }
        }

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
