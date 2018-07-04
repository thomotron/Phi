using System;
using System.Collections.Generic;
using System.Net;
using SocketLibrary;

namespace PhiServer
{
    public class PhiServer
    {
        private WebsocketServer server;

        private List<WebsocketServerConnection> connections = new List<WebsocketServerConnection>();

        public PhiServer(IPAddress ipAddress, int port)
        {
            server = new WebsocketServer(ipAddress, port);

            server.OnConnect += OnConnect;
            server.OnDisconnect += OnDisconnect;
            server.Message += OnMessage;
        }

        public void Start()
        {
            server.Start();
        }

        public void Stop()
        {
            server.Stop();
            connections.Clear();
        }

        private void OnConnect(WebsocketServerConnection connection)
        {
            connections.Add(connection);
        }

        private void OnDisconnect(WebsocketServerConnection connection)
        {
            connections.Remove(connection);
        }

        private void OnMessage(WebsocketServerConnection client, byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}