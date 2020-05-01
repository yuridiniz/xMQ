using System;
using System.Collections.Generic;
using xMQ;

namespace xMQExample
{
    class Client
    {
        private const uint PRINT = 0;
        private const uint ADD = 1;
        private const uint GET = 2;

        private PairSocket pairSocket;

        static void Main(string[] args)
        {
            var client = new Client();
            client.Start();
            
        }

        private void Start()
        {
            pairSocket = new PairSocket();

            while (true)
            {
                var success = pairSocket.TryConnect("tcp://127.0.0.1:5001");
                if (success)
                {
                    DoCommunication();

                } else
                {
                    Console.WriteLine("> Conexão não foi estabelecida, pressione qualquer tecla para tentar novamente");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>
        /// My basic protocol send first frame with a uint, this contains the command
        /// </summary>
        private void DoCommunication()
        {
            Console.WriteLine("> Iniciando comunicação básica");

            pairSocket.OnMessage += OnMessage;

            var keepCommunication = true;
            while (keepCommunication)
            {
                var package = new Message();
                package.Append(1); // My command to ADD
                package.Append("Hello");

                pairSocket.Send(package);

                package = new Message();
                package.Append(1); // My command to ADD
                package.Append("World");

                pairSocket.Send(package);

                package = new Message();
                package.Append(1); // My command to ADD
                package.Append(":)");

                pairSocket.Send(package);

                package = new Message();
                package.Append(2); // My command to READ

                pairSocket.Send(package);

                Console.WriteLine("> Pressione qualquer tecla para enviar novamente");

                Console.ReadKey();
            }
        }

        private void OnMessage(Message msg, PairSocket socket)
        {
            Console.WriteLine("> Recebido via OnMessage");

            var op = msg.ReadNext<uint>();
            if (op == PRINT)
            {
                string frame = "";
                while ((frame = msg.ReadNext<string>()) != "")
                    Console.WriteLine(frame);
            }
            else
            {
                Console.WriteLine(msg.ToString()); //Show all package frames as String
            }
        }
    }
}
