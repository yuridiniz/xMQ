using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ
{
    internal class Envelope : NetworkPackage
    {
        internal Message messageData;

        internal Envelope(Message msgSender)
        {
            messageData = msgSender;
            messageData.Envelope = this;
        }

        internal Envelope(byte[] received)
            : base(received)
        {
        }

        internal Message GetMessage()
        {
            //Caso já possua uma mensagem, pode ter sido carregada durante o construtor ou esse metodo ja foi chamado
            if (messageData != null)
                return messageData;

            var buffer = new byte[receivedData.Length - pointer];

            Array.Copy(receivedData, pointer, buffer, 0, buffer.Length);

            messageData = new Message(buffer);
            messageData.Envelope = this;

            return messageData;
        }

        public override byte[] ToByteArray()
        {
            //Caso seja um envelope de dados recebidos, retorna todos os dados recebidos
            if (receivedData != null)
                return receivedData;

            //Caso nao, ele deve possua uma mensagem e bytes de cabecalho (envelope)
            senderBuffer.AddRange(messageData.ToByteArray(false));
            return base.ToByteArray();
        }

    }
}
