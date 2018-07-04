﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Xml.Serialization;

namespace SocketLibrary
{
    public class WebsocketServer
    {
        WebSocketServer server;
        List<WebsocketServerConnection> clients = new List<WebsocketServerConnection>();
        
        public event Action<WebsocketServerConnection> OnConnect;
        public event Action<WebsocketServerConnection> OnDisconnect;

        public delegate void MessageHandler(WebsocketServerConnection client, byte[] data);
        public event MessageHandler Message;

        public WebsocketServer(IPAddress address, int port)
        {
            this.server = new WebSocketServer(address, port);
        }

        public void SendAll(string data)
        {
            foreach (WebsocketServerConnection client in this.clients)
            {
                client.Send(data);
            }
        }

        public void Start()
        {
            this.server.Start();
            this.server.AddWebSocketService<WebsocketServerConnection>("/", () =>
            {
                return new WebsocketServerConnection(this);
            });
        }

        public void Stop()
        {
            this.server.Stop();
            this.server.RemoveWebSocketService("/");
        }

        internal void ConnectionCallback(WebsocketServerConnection client)
        {
            this.clients.Add(client);

            this.OnConnect(client);
        }

        internal void MessageCallback(WebsocketServerConnection client, byte[] data)
        {
            this.Message(client, data);
        }

        internal void CloseCallback(WebsocketServerConnection client)
        {
            this.clients.Remove(client);

            this.OnDisconnect(client);
        }
    }
}
