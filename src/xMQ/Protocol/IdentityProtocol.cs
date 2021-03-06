﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    /// <summary>
    /// Implementação para processamento de mensagem para identificação dos clientes
    /// </summary>
    internal class IdentityProtocol : ProtocolCommand
    {
        private IdentityProtocol()
        {
        }

        private static IdentityProtocol _command;
        public static IdentityProtocol Command
        {
            get
            {
                if (_command == null) _command = new IdentityProtocol();
                return _command;
            }
        }

        public override bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelop)
        {
            var msg = envelop.GetMessage();

            if(me.IdentityConnectionSocketsMap.ContainsKey(remote))
            {
                //Evita de várias conexões fake no servidor
                remote.Close();
            }

            byte[] identifier;
            if (msg.Length >= 16)
            {
                identifier = msg.ReadNext(16);
                remote.ConnectionId = new Guid(identifier);
            }
            else
            {
                remote.ConnectionId = Guid.NewGuid();
                identifier = remote.ConnectionId.ToByteArray();
            }

            me.IdentitySocketsMap[identifier] = remote;
            SendResultCode(remote, identifier);

            return true;
        }

        private void SendResultCode(PairSocket remote, byte[] identityArray)
        {
            var msg = new Message();
            msg.Append(identityArray);

            var envelope = new Envelope(msg);
            envelope.Append(IdentityResultProtocol.Command);

            remote.Socket.Send(envelope.ToByteArray());
        }
    }
}
