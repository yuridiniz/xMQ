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
            SupportedProtocol.Add(NoneCommand.CODE, new NoneCommand());
            SupportedProtocol.Add(IdentityCommand.CODE, new IdentityCommand());
            SupportedProtocol.Add(IdentityResultCommand.CODE, new IdentityResultCommand());
            SupportedProtocol.Add(RequestCommand.CODE, new RequestCommand());
            SupportedProtocol.Add(ReplyCommand.CODE, new ReplyCommand());
            SupportedProtocol.Add(PublishCommand.CODE, new PublishCommand());
            SupportedProtocol.Add(MsgPublishedCommand.CODE, new MsgPublishedCommand());
            SupportedProtocol.Add(SubscribeCommand.CODE, new SubscribeCommand());
            SupportedProtocol.Add(UnsubscribeCommand.CODE, new UnsubscribeCommand());
            SupportedProtocol.Add(SetLastWillCommand.CODE, new SetLastWillCommand());
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
