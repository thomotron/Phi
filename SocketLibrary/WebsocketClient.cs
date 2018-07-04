using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;

namespace SocketLibrary
{
    public class WebsocketClient
    {
        WebSocket client;

        public event Action<byte[]> Message;

        public event Action OnConnect;
        public event Action OnDisconnect;

        public WebSocketState state
        {
            get
            {
                return this.client.ReadyState;
            }
        }

        public WebsocketClient(string address, int port)
        {
            this.client = new WebSocket("ws://" + address + ":" + port + "/");
            this.client.OnMessage += this.MessageCallback;
            this.client.OnOpen += this.OpenCallback;
            this.client.OnClose += this.CloseCallback;
        }

        public void Connect()
        {
            this.client.ConnectAsync();
        }

        public void Disconnect()
        {
            this.client.CloseAsync();
        }

        public void Send(string data)
        {
            this.client.SendAsync(data, null);
        }

        public void Send(byte[] data)
        {
            this.client.SendAsync(data, null);
        }

        private void OpenCallback(object sender, EventArgs e)
        {
            this.OnConnect();
        }

        private void CloseCallback(object sender, CloseEventArgs e)
        {
            this.OnDisconnect();
        }

        private void MessageCallback(object sender, MessageEventArgs e)
        {
            byte[] rawData = e.RawData;
            this.Message(e.RawData);
        }
    }
}
