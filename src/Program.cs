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

            Console.WriteLine("輸入文字可傳送，輸入 exit 離開");

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