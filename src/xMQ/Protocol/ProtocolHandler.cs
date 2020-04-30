using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    /// <summary>
    /// Processa as mensagens suportadas pelo protocolo
    /// </summary>
    internal class ProtocolHandler
    {
        internal Dictionary<byte, ProtocolCommand> SupportedProtocol { get; } = new Dictionary<byte, ProtocolCommand>();

        public ProtocolHandler()
        {
            InitSupportedProtocolHeader();
        }

        private void InitSupportedProtocolHeader()
        {
            SupportedProtocol.Add(NoneCommand.CODE, NoneCommand.Handler);
            SupportedProtocol.Add(IdentityCommand.CODE, IdentityCommand.Handler);
            SupportedProtocol.Add(IdentityResultCommand.CODE, IdentityResultCommand.Handler);
            SupportedProtocol.Add(RequestCommand.CODE, RequestCommand.Handler);
            SupportedProtocol.Add(ReplyCommand.CODE, ReplyCommand.Handler);
        }

        internal void HandleMessage(PairSocket me, PairSocket remote, Envelope envelope)
        {
            envelope.Move(0);

            var code = envelope.ReadNext<byte>();

            if (SupportedProtocol.ContainsKey(code))
                SupportedProtocol[code].HandleMessage(me, remote, envelope);
        }
    }
}
