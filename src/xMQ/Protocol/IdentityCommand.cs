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
    internal class IdentityCommand : ProtocolCommand
    {
        public const byte CODE = 1;

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

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var canHandler = CanHandler(CODE, envelop);

            if (!canHandler)
                return false;

            return true;
        }
    }
}
