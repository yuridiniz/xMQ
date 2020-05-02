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
    internal class None : ProtocolCommand
    {
        private None()
        {
        }

        private static None _command;
        public static None Command
        {
            get
            {
                if (_command == null) _command = new None();
                return _command;
            }
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            me.OnMessage?.Invoke(envelop.GetMessage(), remote, null);

            return true;
        }
    }
}
