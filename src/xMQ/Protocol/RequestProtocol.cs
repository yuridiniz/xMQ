using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    internal class Request : ProtocolCommand
    {
        private Request()
        {
        }

        private static Request _command;
        public static Request Command
        {
            get
            {
                if (_command == null) _command = new Request();
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
