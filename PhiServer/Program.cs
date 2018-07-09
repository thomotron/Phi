using System;
using System.Net;
using PhiClient;
using System.Collections;

namespace PhiServer
{
    public class Program
    {
        static void Main(string[] args)
        {
            ServerConfig config = new ServerConfig("server.conf");
            config.Load();

            PhiServer server = new PhiServer(config.Address, config.Port);

            server.Start();

            Console.Read();
        }
    }
}
