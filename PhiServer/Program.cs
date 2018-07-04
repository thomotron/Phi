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
            PhiServer server = new PhiServer(IPAddress.Any, 16180);

            server.Start();

            Console.Read();
        }
    }
}
