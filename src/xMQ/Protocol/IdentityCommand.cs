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


        private enum ResultCode
        {
            SUCCESS = 0,
            CONFLICT = 1
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var msg = envelop.GetMessage();
            var bytesIdentity = msg.ReadNext();

            if (!me.IdentitySocketsMap.ContainsKey(bytesIdentity))
            {
                me.IdentitySocketsMap[bytesIdentity] = remote;
                SendResultCode(remote, ResultCode.SUCCESS);
                return true;
            }

            var registredConnection = me.IdentitySocketsMap[bytesIdentity];
            if (registredConnection == remote)
            {
                SendResultCode(remote, ResultCode.SUCCESS);
                return true;
            }

            SendResultCode(remote, ResultCode.CONFLICT);

            return true;
        }

        private void SendResultCode(PairSocket remote, ResultCode result)
        {
            var msg = new Message();
            msg.Append(result);

            var envelope = new Envelope(msg);
            envelope.Append(IdentityResultCommand.CODE);

            remote.socket.Send(envelope.ToByteArray());
        }
    }
}
