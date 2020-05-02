using System;
using System.Collections.Generic;
using System.Threading;
using xMQ;

namespace xMQExample
{
    class Client
    {
        public List<string> storedvalues = new List<string>();

        private PairSocket pairSocket;

        static void Main(string[] args)
        {
            Console.Title = "Client";

            var client = new Client();
            client.Start();
            
        }

        private void Start()
        {
            pairSocket = new PairSocket();

            while (true)
            {
                var success = pairSocket.TryConnect("tcp://127.0.0.1:5001", 500);
                if (success)
                {
                    // Tokens podem ser adicioandos assim também
                    var msg = new Message(1, "Hello Server :)");
                    pairSocket.Send(msg);

                    pairSocket.Subscribe("connecteds", PubSubQueueLostType.LastMessage);
                    pairSocket.Publish("connecteds", new Message(0, "Chegando ae na area " + pairSocket.ConnectionId));
                    pairSocket.SetLastWill("offline", new Message(0, "Meti o pé " + pairSocket.ConnectionId));
                    // Sleep apenas para que o Console não fique com mensagens misturadas e atrapalhe o entendimento do exemplo
                    // Em um cenário real, não precisaria de qualquer tipo de delay
                    Thread.Sleep(500);

                    //pairSocket.Close();

                    //var _success = false;
                    //while(!_success)
                    //{
                    //    Thread.Sleep(10000);
                    //    success = pairSocket.TryReconnect();
                    //}

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
                if(uint.TryParse(operation, out opCode))
                {
                    pairSocket.TryReconnect(5000);
                    Console.Write("> Mensagem (string): ");
                    var txtMessage = Console.ReadLine();

                    var package = new Message();
                    package.Append(opCode);
                    package.Append(txtMessage);

                    var response = pairSocket.Request(package, 10); //2ms Timeout
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
                    } else
                    {
                        Console.WriteLine(">>> Request fail!");
                    }

                }
                else
                {
                    pairSocket.Close();
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
