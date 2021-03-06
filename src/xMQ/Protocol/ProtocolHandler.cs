﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    /// <summary>
    /// Processa as mensagens suportadas pelo protocolo
    /// </summary>
    public class ProtocolHandler
    {
        internal Dictionary<int, ProtocolCommand> SupportedProtocol { get; } = new Dictionary<int, ProtocolCommand>();

        internal void HandleMessage(PairSocket me, PairSocket remote, Envelope envelope)
        {
            envelope.Move(0);

            var code = envelope.ReadNext<int>();

            if (SupportedProtocol.ContainsKey(code))
                SupportedProtocol[code].HandleMessage(me, remote, envelope);
        }
    }
}
