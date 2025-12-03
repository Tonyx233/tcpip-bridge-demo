using System;

namespace MesTcpClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== MES TCP/IP Client Demo ===");

            var client = new MesTcpClient("127.0.0.1", 9000);

            client.OnConnected = () => Console.WriteLine("[EVENT] Connected.");
            client.OnDisconnected = () => Console.WriteLine("[EVENT] Disconnected.");
            client.OnReconnected = () => Console.WriteLine("[EVENT] Reconnected!");
            client.OnMessageReceived = msg => Console.WriteLine($"<SERVER> {msg}");

            client.Start();

            Console.WriteLine("Type message and press ENTER. Type 'exit' to quit.");

            while (true)
            {
                string input = Console.ReadLine();

                if (input == "exit")
                    break;

                client.Send(input);
            }

            client.Stop();
        }
    }
}