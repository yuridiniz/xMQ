using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    internal class RequestCommand : ProtocolCommand
    {
        private RequestCommand()
        {
        }

        private static RequestCommand _command;
        public static RequestCommand Command
        {
            get
            {
                if (_command == null) _command = new RequestCommand();
                return _command;
            }
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var msgId = envelop.ReadNext<uint>();

            me.OnMessage?.Invoke(envelop.GetMessage(), remote, null);

            return true;
        }
    }

}
