using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xMQ.Protocol;
using xMQ.Util;

namespace xMQ
{
    public abstract class NetworkPackage
    {
        protected List<byte> senderBuffer = new List<byte>();
        protected byte[] receivedData;
        protected int pointer = 0;

        public NetworkPackage()
        {
        }

        public NetworkPackage(params object[] appends)
        {
            for (var i = 0; i < appends.Length; i++)
                Append(appends[i]);
        }

        internal NetworkPackage(byte[] socketData)
        {
            receivedData = socketData;
        }

        public bool Success { get; internal set; }

        public int Length { get { return receivedData.Length; } }

        public void Reset()
        {
            pointer = 0;
        }
        public void Move(int position)
        {
            pointer = position;
        }
        public byte[] ReadNext()
        {
            var strB = new List<byte>();
            while (pointer < receivedData.Length && receivedData[pointer] != 0)
            {
                strB.Add(receivedData[pointer]);
                pointer++;
            }

            pointer++;

            return strB.ToArray();
        }

        public byte[] ReadNext(int size)
        {
            var arr = new byte[size];
            Array.Copy(receivedData, pointer, arr, 0, size);

            pointer += size;
            return arr;
        }

        public void Append(object value)
        {
            if (value is ProtocolCommand)
            {
                senderBuffer.Add(((ProtocolCommand)value).CODE);
            }
            else
            {
                senderBuffer.AddRange(GenericBitConverter.GetBytes(value));

                if (value.GetType() == typeof(string))
                {
                    AppendEmptyFrame();
                }
            }
        }

        public void AppendEmptyFrame()
        {
            senderBuffer.Add(0);
        }

        public virtual byte[] ToByteArray()
        {
            return ToByteArray(true);
        }

        internal byte[] ToByteArray(bool generateSizeHeader)
        {
            //Caso não haja dados para enviar, pode ser um roteamente de uma mensagem recebida pela rede
            var result = senderBuffer.Count > 0 ? senderBuffer.ToArray() : receivedData;
            if (!generateSizeHeader)
                return result;

            var headerSize = 2;
            byte[] packageSend = new byte[result.Length + headerSize];

            var sizeByte = BitConverter.GetBytes((short)result.Length);

            Array.Copy(sizeByte, packageSend, sizeByte.Length);
            Array.Copy(result, 0, packageSend, headerSize, result.Length);

            return packageSend;
        }

        public T ReadNext<T>()
        {
            if (typeof(T) == typeof(string))
            {
                var bytes = ReadNext();
                var value = Encoding.UTF8.GetString(bytes.ToArray());
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else
            {
                int size;
                var result = GenericBitConverter.GetValue<T>(receivedData, pointer, out size);
                pointer += size;

                return result;
            }
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(receivedData ?? senderBuffer.ToArray());
        }
    }
}
