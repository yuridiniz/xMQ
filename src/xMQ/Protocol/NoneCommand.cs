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

        private static NoneCommand _handlerInstance  = null;
        public static NoneCommand Handler {
            get
            {
                if (_handlerInstance == null)
                    _handlerInstance = new NoneCommand();

                return _handlerInstance;
            }
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            me.OnMessage?.Invoke(envelop.GetMessage(), remote);

            return true;
        }
    }
}
