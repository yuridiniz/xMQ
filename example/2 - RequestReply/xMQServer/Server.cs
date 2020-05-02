using System;
using System.Collections.Generic;
using xMQ;

namespace xMQExample
{
    class Server
    {
        public List<string> storedvalues = new List<string>();

        private PairSocket pairSocket;

        static void Main(string[] args)
        {
            var client = new Server();
            client.Start();

        }

        private void Start()
        {
            Console.Title = "Server";

            pairSocket = new PairSocket();

            while (true)
            {
                var success = pairSocket.TryBind("tcp://127.0.0.1:5001");
                if (success)
                {
                    DoCommunication();
                }
                else
                {
                    Console.WriteLine("> Conexão não foi estabelecida, pressione qualquer tecla para tentar novamente");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>
        /// My basic protocol send first frame with a ushort, this contains the command
        /// </summary>
        private void DoCommunication()
        {
            Console.WriteLine("> Iniciando comunicação básica");
            Console.WriteLine("> Comandos (uint): ");
            Console.WriteLine("> 0: Escreve no console remoto");
            Console.WriteLine("> 1: Envia um dado para ser salvos");
            Console.WriteLine("> 2: Solicita todos os dados salvos");
            Console.WriteLine("> 3: Envia um Echo");
            Console.WriteLine("> ");

            pairSocket.OnMessage += OnMessage;

            var keepCommunication = true;
            while (keepCommunication)
            {
                Console.Write("> Operação (uint): ");

                var operation = Console.ReadLine();

                uint opCode;
                if (uint.TryParse(operation, out opCode))
                {
                    Console.Write("> Mensagem (string): ");
                    var txtMessage = Console.ReadLine();

                    var package = new Message();
                    package.Append(opCode);
                    package.Append(txtMessage);

                    var clients = pairSocket.GetAllClients();
                    if (clients.Count > 0)
                    {
                        var client = clients[0];

                        var response = client.Request(package);
                        if (response.Success)
                        {
                            Console.WriteLine(">>> Response:");

                            var serverOperationCode = response.ReadNext<uint>();

                            Console.WriteLine("");

                            Console.WriteLine("Operation from Remote: " + serverOperationCode);
                            string frame = "";
                            while ((frame = response.ReadNext<string>()) != "")
                                Console.WriteLine(frame);

                            Console.WriteLine("");
                        }
                        else
                        {
                            Console.WriteLine(">>> Request fail!");
                        }
                       
                    } else
                    {
                        Console.WriteLine("> Nenhum cliente conectado");
                        Console.WriteLine("");
                    }
                }
                else
                {
                    Console.WriteLine("> Operação não é um valor válido, informe um uint válido");
                }
            }
        }

        private void OnMessage(Message msg, PairSocket socket, MessageData data)
        {
            var op = msg.ReadNext<uint>();
            if (op == 1)
            {
                storedvalues.Add(msg.ReadNext<string>());

                msg.Append(0);
                msg.Append("Adicionado");

                socket.Send(msg);
            }
            else if (op == 2)
            {
                msg.Append(0);
                foreach (var item in storedvalues)
                    msg.Append(item);

                socket.Send(msg);
            }
            else if (op == 3)
            {
                var echoValue = msg.ReadNext<string>();

                msg.Append(0);
                msg.Append(echoValue);

                socket.Send(msg);
            }
            else
            {
                Console.WriteLine(msg.ToString());
            }
        }
    }
}
