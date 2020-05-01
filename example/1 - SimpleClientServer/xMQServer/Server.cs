using System;
using System.Collections.Generic;
using xMQ;

namespace xMQExample
{
    class Server
    {
        private const uint PRINT = 0;
        private const uint ADD = 1;
        private const uint GET = 2;

        public List<string> storedvalues = new List<string>();
        private PairSocket pairSocket;

        static void Main(string[] args)
        {
            var client = new Server();
            client.Start();

        }

        private void Start()
        {
            pairSocket = new PairSocket();

            bool connected = false;
            while (!connected)
            {
                connected = pairSocket.TryBind("tcp://127.0.0.1:5001");
                if (connected)
                    pairSocket.OnMessage += OnMessage;
                else
                    Console.WriteLine("> Conexão não foi estabelecida, pressione qualquer tecla para tentar novamente");
            }

            Console.WriteLine("> Iniciando comunicação básica");
            Console.WriteLine("> Pressione qualquer tecla para encerrar");

            Console.ReadKey();
        }

        private void OnMessage(Message msg, PairSocket socket)
        {
            Console.WriteLine("> Recebido via OnMessage");

            var op = msg.ReadNext<uint>();
            if (op == ADD)
            {
                storedvalues.Add(msg.ReadNext<string>());
            }
            else if (op == GET)
            {
                msg.Append(PRINT);
                foreach (var item in storedvalues)
                    msg.Append(item);

                socket.Send(msg);
            }
            else
            {
                Console.WriteLine(msg.ToString()); //Show all package frames as String
            }
        }
    }
}
