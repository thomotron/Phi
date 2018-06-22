using System;
using System.Net;
using SocketLibrary;
using PhiClient;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using PhiClient.TransactionSystem;

namespace PhiServer
{
    public class Program
    {
        private Server server;
        private RealmData realmData;
        private Dictionary<ServerClient, User> connectedUsers = new Dictionary<ServerClient, User>();
        private Dictionary<int, string> userKeys = new Dictionary<int, string>();
        private LogLevel logLevel;

        private object lockProcessPacket = new object();

        public void Start(IPAddress ipAddress, int port, LogLevel logLevel)
        {
            this.logLevel = logLevel;

            this.server = new Server(ipAddress, port);
            this.server.Start();
            Log(LogLevel.INFO, string.Format("Server launched on port {0}", port));

            this.server.Connection += this.ConnectionCallback;
            this.server.Message += this.MessageCallback;
            this.server.Disconnection += this.DisconnectionCallback;

            Log(LogLevel.DEBUG, "Registered callbacks");

            this.realmData = new RealmData();
            this.realmData.PacketToClient += this.RealmPacketCallback;
            this.realmData.Log += Log;

            Log(LogLevel.DEBUG, "Initialised RealmData");
        }

        private void ConnectionCallback(ServerClient client)
        {
            Log(LogLevel.INFO, "Connection from " + client.ID);
        }

        private void Log(LogLevel level, string message)
        {
            if (level < this.logLevel)
            {
                return;
            }

            string tag = "";
            if (level == LogLevel.DEBUG)
            {
                tag = "DEBUG";
            }
            else if (level == LogLevel.ERROR)
            {
                tag = "ERROR";
            }
            else if (level == LogLevel.INFO)
            {
                tag = "INFO";
            }

            string logLine = string.Format("[{0}] [{1}] {2}", DateTime.Now, tag, message);

            Console.WriteLine(logLine);
            AppendLog(logLine);
        }

        private void AppendLog(string line)
        {
            using (StreamWriter sw = new StreamWriter("server.log", true))
            {
                sw.WriteLine(line);
            }
        }

        private void RealmPacketCallback(User user, Packet packet)
        {
            // We have to transmit a packet to a user, we find the connection
            // attached to this user and transmit the packet
            foreach (ServerClient client in this.connectedUsers.Keys)
            {
                User u;
                this.connectedUsers.TryGetValue(client, out u);
                if (u == user)
                {
                    this.SendPacket(client, user, packet);

                    Log(LogLevel.DEBUG, string.Format("Sent packet to {0}", user.id));

                    return; // No need to continue iterating once we've found the right user
                }
            }
        }

		private void SendPacket(ServerClient client, User user, Packet packet)
		{
            Log(LogLevel.DEBUG, string.Format("Server -> {0}: {1}", user != null ?  user.name : "No", packet));
			client.Send(Packet.Serialize(packet, realmData, user));
        }

        private void DisconnectionCallback(ServerClient client)
        {
            User user;
            this.connectedUsers.TryGetValue(client, out user);
            if (user != null)
            {
                Log(LogLevel.INFO, string.Format("{0} disconnected", user.name));
                this.connectedUsers.Remove(client);
                Log(LogLevel.DEBUG, string.Format("Removed {0} from connected users list", user.name));
                user.connected = false;
                this.realmData.BroadcastPacket(new UserConnectedPacket { user = user, connected = false });
                Log(LogLevel.DEBUG, string.Format("Broadcast disconnected user {0}", user.name));
            }
        }

        private void MessageCallback(ServerClient client, byte[] data)
        {
            lock (lockProcessPacket)
			{
			    User user;
				this.connectedUsers.TryGetValue(client, out user);

				Packet packet = Packet.Deserialize(data, realmData, user);
                Log(LogLevel.DEBUG, string.Format("{0} -> Server: {1}", user != null ? user.name : client.ID, packet));

                if (packet is AuthentificationPacket)
                {
                    Log(LogLevel.DEBUG, "Received authentication packet");

                    // Special packets, (first sent from the client)
                    AuthentificationPacket authPacket = (AuthentificationPacket)packet;

                    // We first check if the version corresponds
                    if (authPacket.version != RealmData.VERSION)
                    {
                        Log(LogLevel.DEBUG, $"Authentication packet version ({authPacket.version}) does not match server version ({RealmData.VERSION}), discarding");

                        this.SendPacket(client, user, new AuthentificationErrorPacket
                        {
                            error = "Server is version " + RealmData.VERSION + " but client is version " + authPacket.version
                        }
                        );
                        return;
                    }

                    // Check if the user wants to use a specific id
                    int userId;
                    if (authPacket.id != null)
                    {
                        Log(LogLevel.DEBUG, $"Client is requesting id {authPacket.id.Value}");

                        // Link key to existing id, or a new one if it doesn't exist or the keys don't match
                        userId = RegisterUserKey(authPacket.id.Value, authPacket.hashedKey);
                    }
                    else
                    {
                        // Generate a new id and link the key to it
                        userId = RegisterUserKey(++realmData.lastUserGivenId, authPacket.hashedKey);
                    }

                    user = this.realmData.users.FindLast(delegate (User u) { return userId == u.id; });
                    if (user == null)
                    {
                        Log(LogLevel.DEBUG, $"No user found matching id {userId}");

                        user = this.realmData.ServerAddUser(authPacket.name, userId);
                        user.connected = true;

                        Log(LogLevel.DEBUG, $"Registered user {user.name}");

                        // We send a notify to all users connected about the new user
                        this.realmData.BroadcastPacketExcept(new NewUserPacket { user = user }, user);

                        Log(LogLevel.DEBUG, $"Broadcast new user {user.name}");
                    }
                    else
                    {
                        Log(LogLevel.DEBUG, $"User found matching id {userId}");

                        user.connected = true;

                        // We send a connect notification to all users
                        this.realmData.BroadcastPacketExcept(new UserConnectedPacket { user = user, connected = true }, user);

                        Log(LogLevel.DEBUG, $"Broadcast connected user {user.name}");
                    }

                    Log(LogLevel.DEBUG, $"Set last transaction time for user {user.name} as {DateTime.Now}");

                    this.connectedUsers.Add(client, user);
                    Log(LogLevel.INFO, string.Format("Client {0} connected as {1} ({2})", client.ID, user.name, user.id));

                    // We respond with a StatePacket that contains all synchronisation data
                    this.SendPacket(client, user, new SynchronisationPacket { user = user, realmData = this.realmData });

                    Log(LogLevel.DEBUG, $"Sent synchronisation data to user {user.name}");
                }
                else if (packet is StartTransactionPacket)
                {
                    if (user == null)
                    {
                        // We ignore this packet
                        Log(LogLevel.ERROR, string.Format("{0} ignored because unknown user {1}", packet, client.ID));
                        return;
                    }

                    Log(LogLevel.DEBUG, $"Received transaction start packet from {user.name}");

                    // Check whether the packet was sent too quickly
                    TimeSpan timeSinceLastTransaction = DateTime.Now - user.lastTransactionTime;

                    Log(LogLevel.DEBUG, $"{user.name} last logged in or started a transaction at {user.lastTransactionTime} ({timeSinceLastTransaction.Seconds} seconds from now)");

                    if (timeSinceLastTransaction > TimeSpan.FromSeconds(3))
                    {
                        Log(LogLevel.DEBUG, $"Time since last transaction is greater than 3 seconds");

                        // Apply the packet as normal
                        packet.Apply(user, this.realmData);

                        Log(LogLevel.DEBUG, $"Approved transaction");
                    }
                    else
                    {
                        Log(LogLevel.DEBUG, $"Time since last transaction is less than or equal to 3 seconds");

                        // Intercept the packet, returning it to sender
                        StartTransactionPacket transactionPacket = packet as StartTransactionPacket;
                        transactionPacket.transaction.state = TransactionResponse.TOOFAST;
                        this.SendPacket(client, user, new ConfirmTransactionPacket { response = transactionPacket.transaction.state, toSender = true, transaction = transactionPacket.transaction});

                        Log(LogLevel.DEBUG, $"Sent a spam warning to user {user.name}");

                        // Report the packet to the log
                        Log(LogLevel.ERROR, string.Format("{0} ignored because user {1} sent a packet less than 3 seconds ago", packet, client.ID));
                    }
                }
                else
                {
                    Log(LogLevel.DEBUG, $"Received packet");

                    if (user == null)
                    {
                        // We ignore this package
                        Log(LogLevel.ERROR, string.Format("{0} ignored because unknown user {1}", packet, client.ID));
                        return;
                    }

                    // Normal packets, we defer the execution
                    packet.Apply(user, this.realmData);

                    Log(LogLevel.DEBUG, $"Packet applied");
                }
            }
        }

        /// <summary>
        /// Checks if the key matches an existing id. If it does not match, returns new id which the key is linked to. Returns the input id otherwise.
        /// </summary>
        /// <param name="id">The user's id</param>
        /// <param name="hashedKey">The user's hashed key. This should only be kept on the server.</param>
        private int RegisterUserKey(int id, string hashedKey)
        {
            Log(LogLevel.DEBUG, string.Format("Attempting to register user id {0} to hashed key {1}", id, hashedKey));

            // Check if this user exists
            if (userKeys.ContainsKey(id) && id <= realmData.lastUserGivenId)
            {
                Log(LogLevel.DEBUG, $"User id {id} exists");

                // Check if the two keys are different
                if (hashedKey != userKeys[id])
                {
                    Log(LogLevel.DEBUG, $"Hashed key does not match, registering to new id");

                    // Register a new id and key pair
                    id = ++realmData.lastUserGivenId;
                    userKeys.Add(id, hashedKey);
                }
            }
            else
            {
                Log(LogLevel.DEBUG, $"Invalid id, registering to new id");

                // Register a new id and key pair
                id = ++realmData.lastUserGivenId;
                userKeys.Add(id, hashedKey);
            }

            Log(LogLevel.DEBUG, $"Registered key {hashedKey} to id {id}");

            return id;
        }

        static void Main(string[] args)
        {
            Program program = new Program();

            LogLevel logLevel = LogLevel.ERROR;
            if (args.Length > 0)
            {
                if (args[0].Equals("debug"))
                {
                    logLevel = LogLevel.DEBUG;
                }
            }

            program.Start(IPAddress.Any, 16180, logLevel);

            Console.Read();
        }
    }
}
