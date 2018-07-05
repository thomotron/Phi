using PhiClient;
using SocketLibrary;
using System;
using System.Linq;
using Verse;
using RimWorld;
using System.Text;
using System.Collections;
using WebSocketSharp;

namespace PhiClient
{
    public class PhiClient
    {
        public static PhiClient instance;

        public WebSocketState ClientState => client.state;

        private WebsocketClient client;
        private ConfigManager config = new ConfigManager("phi.dat");

        public string ServerAddress => serverAddress;
        private string serverAddress
        {
            get
            {
                if (config.TryGetValue("ServerAddress", out string address)) return address;
                else return ConnectionDefaults.DEFAULT_SERVER_ADDRESS;
            }
            set => config.SetValue("ServerAddress", value);
        }

        public int ServerPort => serverPort;
        private int serverPort
        {
            get
            {
                if (config.TryGetValue("ServerPort", out int address)) return address;
                else return ConnectionDefaults.DEFAULT_SERVER_PORT;
            }
            set => config.SetValue("ServerPort", value);
        }

        /// <summary>
        /// Instantiate a new <c>PhiClient</c> object and update the <c>PhiClient.instance</c> property.
        /// </summary>
        public PhiClient()
        {
            PhiClient.instance = this;
        }

        /// <summary>
        /// Hook called by Unity every tick.
        /// </summary>
        public void OnUpdate()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disconnects and attempts to connect to the server.
        /// </summary>
        public void Connect()
        {
            // Disconnect just in case we are already connected
            Disconnect();

            // Set up a new client
            client = new WebsocketClient(serverAddress, serverPort);
            client.OnConnect += OnConnect;
            client.OnDisconnect += OnDisconnect;
            client.Message += OnMessage;
            
            // Try to connect
            client.Connect();
        }

        /// <summary>
        /// Disconnects from the server if there is an active connection.
        /// </summary>
        public void Disconnect()
        {
            client?.Disconnect();
        }

        /// <summary>
        /// Called when a successful connection to the websocket server is made.
        /// </summary>
        private void OnConnect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when the connection to the websocket server is terminated.
        /// </summary>
        private void OnDisconnect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when a message is received from the websocket server.
        /// </summary>
        /// <param name="obj">The message payload</param>
        private void OnMessage(byte[] obj)
        {
            throw new NotImplementedException();
        }
    }
}
