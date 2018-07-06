using PhiClient;
using SocketLibrary;
using System;
using System.Linq;
using Verse;
using RimWorld;
using System.Text;
using System.Collections;
using HugsLib.Settings;
using WebSocketSharp;

namespace PhiClient
{
    public class PhiClient : HugsLib.ModBase
    {
        public override string ModIdentifier => "PhiClient";
        
        public static PhiClient Instance;
        public WebSocketState ClientState => client.state;

        private WebsocketClient client;
        private SettingHandle<string> serverAddressHandle;
        public string ServerAddress => serverAddressHandle;
        private SettingHandle<int> serverPortHandle;
        public int ServerPort => serverPortHandle;

        /// <summary>
        /// Instantiate a new <c>PhiClient</c> object and update the <c>PhiClient.instance</c> property.
        /// </summary>
        public PhiClient()
        {
            // Set this as the static instance
            PhiClient.Instance = this;

            // Load in settings
            serverAddressHandle = Settings.GetHandle(
                settingName: "serverAddress",
                title: "Server Address",
                description: null,
                defaultValue: ConnectionDefaults.DEFAULT_SERVER_ADDRESS
            );
            serverPortHandle = Settings.GetHandle(
                settingName: "serverPort",
                title: "Server Port",
                description: null,
                defaultValue: ConnectionDefaults.DEFAULT_SERVER_PORT,
                validator: value => int.TryParse(value, out _)
            );
        }

        /// <summary>
        /// Called when the mod is first initialised.
        /// Used to set up the first instance.
        /// </summary>
        public override void Initialize()
        {
            PhiClient phiClient = new PhiClient();
            phiClient.Connect();
        }
        
        /// <summary>
        /// Called by Unity at a set interval.
        /// Used to keep the client in sync and process any incoming messages.
        /// </summary>
        public override void FixedUpdate()
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
            client = new WebsocketClient(ServerAddress, ServerPort);
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
