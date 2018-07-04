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

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            server.Start();
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            server.Stop();
            connections.Clear();
        }

        /// <summary>
        /// Callback for incoming connections.
        /// </summary>
        /// <param name="connection"></param>
        private void OnConnect(WebsocketServerConnection connection)
        {
            connections.Add(connection);
        }

        /// <summary>
        /// Callback for client disconnections.
        /// </summary>
        /// <param name="connection"></param>
        private void OnDisconnect(WebsocketServerConnection connection)
        {
            connections.Remove(connection);
        }

        /// <summary>
        /// Callback for messages received from clients.
        /// </summary>
        /// <param name="client">The connection the message originated from</param>
        /// <param name="data">The message payload</param>
        private void OnMessage(WebsocketServerConnection client, byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}