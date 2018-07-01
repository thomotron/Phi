using System;
using System.Net;
using SocketLibrary;
using PhiClient;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Collections.Concurrent;
using System.Linq;
using PhiClient.TransactionSystem;

namespace PhiServer
{
    public class Program
    {
        private Server server;
        private RealmData realmData;
        private ConcurrentDictionary<ServerClient, User> connectedUsers = new ConcurrentDictionary<ServerClient, User>();
        private Dictionary<int, string> userKeys = new Dictionary<int, string>();
        private Dictionary<int, DateTime> userLastLoginTime = new Dictionary<int, DateTime>();
        private List<string> bannedKeys = new List<string>();
        private List<IPAddress> bannedIPs = new List<IPAddress>();

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

        public void Stop()
        {
            Log(LogLevel.INFO, "Stopping server...");
            this.server.Stop();
        }

        private void ConnectionCallback(ServerClient client)
        {
            Log(LogLevel.INFO, "Connection from " + client.ID);

            // Check if the connecting client's IP is banned
            if (bannedIPs.Contains(client.Context.UserEndPoint.Address))
            {
                Log(LogLevel.INFO, $"Client {client.ID} is connecting from a banned IP address, disconnecting...");

                // Close the connection
                client.Close();
            }
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
                this.connectedUsers.TryRemove(client, out _);
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

                    // Check if the user's key is banned
                    if (bannedKeys.Contains(authPacket.hashedKey))
                    {
                        Log(LogLevel.INFO, $"Client {client.ID} is authenticating with a banned key ({authPacket.hashedKey}), disconnecting...");

                        // Close the connection
                        client.Close();
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

                    if (userLastLoginTime.ContainsKey(userId))
                    {
                        userLastLoginTime[userId] = DateTime.Now;
                    }
                    else
                    {
                        userLastLoginTime.Add(userId, DateTime.Now);
                    }

                    user.lastTransactionTime = userLastLoginTime[userId];

                    Log(LogLevel.DEBUG, $"Set last transaction time for user {user.name} as {DateTime.Now}");
                    
                    this.connectedUsers.TryAdd(client, user);
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

        private void AddIdBan(int id)
        {
            string key = userKeys[id];

            // Check if user is already banned
            if (bannedKeys.Contains(key))
            {
                // User is already banned
                return;
            }

            // Add the key to the ban list
            bannedKeys.Add(key);

            // Find the connection for the newly banned user
            ServerClient client = connectedUsers.First(connectedUserPair => connectedUserPair.Value.id == id).Key;

            // Terminate the connection
            client.Close();
        }

        private void RemoveIdBan(int id)
        {
            string key = userKeys[id];

            if (bannedKeys.Contains(key))
            {
                bannedKeys.Remove(key);
            }
        }

        private void AddIpBan(IPAddress ipAddress)
        {
            // Check if IP is already banned
            if (bannedIPs.Contains(ipAddress))
            {
                // IP is already banned
                return;
            }

            // Add the IP to the ban list
            bannedIPs.Add(ipAddress);

            // Disconnect any clients connected from the newly banned IP
            foreach (ServerClient client in connectedUsers.Keys)
            {
                if (client.Context.UserEndPoint.Address.Equals(ipAddress))
                {
                    client.Close();
                }
            }
        }

        private void RemoveIpBan(IPAddress ipAddress)
        {
            if (bannedIPs.Contains(ipAddress))
            {
                bannedIPs.Remove(ipAddress);
            }
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

            bool exit = false;
            while (!exit)
            {
                string line = Console.ReadLine();
                
                List<string> commandArgs = line.Split(' ').ToList();
                string command = commandArgs.First();
                commandArgs.RemoveAt(0);

                bool result;
                switch (command)
                {
                    case "help":
                        result = cmdHelp();
                        break;
                    case "exit":
                        result = cmdExit(program);
                        exit = true;
                        break;
                    case "version":
                        result = cmdVersion();
                        break;
                    case "clients":
                        result = cmdClients(program);
                        break;
                    case "ban":
                        result = cmdBan(program, commandArgs);
                        break;
                    case "unban":
                        result = cmdUnban(program, commandArgs);
                        break;
                    default:
                        Console.WriteLine($"Unrecognised command: {command}");
                        Console.WriteLine("Type \"help\" to get a list of commands.");
                        result = true;
                        break;
                }

                if (!result) Console.WriteLine($"Command \"{command}\" failed with args: {string.Join(" ", commandArgs.ToArray())}");
            }
        }

        private static bool cmdHelp()
        {
            Console.WriteLine("help                         Display this help list.\n" +
                              "version                      Display the server version.\n" +
                              "exit                         Stop the server.\n" +
                              "clients                      Display all connected clients.\n" +
                              "\n" +
                              "ban <id>                     Ban a user's key.\n" +
                              "ban ip <ip>                  Ban an ip address from connecting.\n" +
                              "unban <id>                   Unban a banned user's key.\n" +
                              "unban ip <ip>                Unban an ip address");

            return true;
        }

        private static bool cmdExit(Program program)
        {
            program.Stop();

            return true;
        }

        private static bool cmdVersion()
        {
            Console.WriteLine($"PhiServer running realm data version {RealmData.VERSION}");

            return true;
        }

        private static bool cmdClients(Program program)
        {
            // Check if anyone is connected
            if (program.connectedUsers.Count == 0)
            {
                Console.WriteLine("No clients connected");
                return true;
            }

            // Print out a header for the table
            Console.WriteLine($"{"Name".PadRight(70)} {"ID".PadRight(10)} {"IP".PadRight(15)}");

            // Print out each connected user's name, id, and ip
            foreach (KeyValuePair<ServerClient, User> pair in program.connectedUsers)
            {
                string name = TextHelper.StripRichText(pair.Value.name);
                string id = pair.Value.id.ToString();
                string ip = pair.Key.Context.UserEndPoint.Address.ToString();
                
                Console.WriteLine($"{name.PadRight(70)} {id.PadRight(10)} {ip.PadRight(15)}");
            }

            return true;
        }

        private static bool cmdBan(Program program, List<string> args, bool unban = false)
        {
            // Check if any arguments were included
            if (args.Count == 0)
            {
                Console.WriteLine("No user id specified.");
                return false;
            }

            // Is this an IP ban?
            if (args.ElementAt(0) == "ip" && args.Count > 1)
            {
                // Parse the ip included in the arguments
                IPAddress ipAddress;
                if (IPAddress.TryParse(args.ElementAt(1), out ipAddress))
                {
                    if (unban)
                    {
                        // Remove the ban
                        program.RemoveIpBan(ipAddress);
                    }
                    else
                    {
                        // Apply the ban
                        program.AddIpBan(ipAddress);
                    }
                }
                else
                {
                    Console.WriteLine($"{args.ElementAt(1)} is not a valid IP address");
                    return false;
                }
            }
            else
            {
                // Not an IP ban, so assume an id ban
                // Parse the user id included in the arguments
                int id;
                if (int.TryParse(args.ElementAt(0), out id) && id > 0)
                {
                    // Check whether a user exists matching this id
                    if (program.userKeys.ContainsKey(id))
                    {
                        if (unban)
                        {
                            // Remove the ban
                            program.RemoveIdBan(id);
                        }
                        else
                        {
                            // Apply the ban
                            program.AddIdBan(id);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Unable to find user matching id {id}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"{args.ElementAt(0)} is not a valid user id");
                    return false;
                }
            }
            
            return true;
        }

        private static bool cmdUnban(Program program, List<string> args)
        {
            return cmdBan(program, args, true);
        }
    }
}
