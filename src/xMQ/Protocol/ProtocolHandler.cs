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
        internal List<ProtocolCommand> SupportedProtocol { get; } = new List<ProtocolCommand>();

        public ProtocolHandler()
        {
            InitSupportedProtocolHeader();
        }

        private void InitSupportedProtocolHeader()
        {
            SupportedProtocol.Add(NoneCommand.Handler);
            SupportedProtocol.Add(IdentityCommand.Handler);
            SupportedProtocol.Add(RequestCommand.Handler);
            SupportedProtocol.Add(ReplyCommand.Handler);
        }

        internal void HandleMessage(PairSocket me, PairSocket remote, Envelope envelope)
        {
            // TODO Verificar a necessidade de performance, talvez um loop pode perder performance quando tiver muitas requisicoes
            // TODO Talvez, ao inves de implementar uma sequencia longa de ifs, seja mais performantico e elegante realizar uma busca binaria a partir do primeiro element
            // fazendo com que a mensagem padra (CODE = 0) seja obtida sempre com prioridade
            for(var i = 0; i < SupportedProtocol.Count; i++)
            {
                var executed = SupportedProtocol[i].HandleMessage(me, remote, envelope);
                if (executed)
                    break;
            }
        }
    }
}
