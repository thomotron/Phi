using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using PhiClient;
using PhiClient.Messages;

namespace PhiServer
{
    class ServerConfig
    {
        public IPAddress Address = IPAddress.Any;
        private const string ADDRESS_KEY = "Address";

        public int Port = ConnectionDefaults.DEFAULT_SERVER_PORT;
        private const string PORT_KEY = "Port";

        private string configPath;

        public ServerConfig(string configPath)
        {
            this.configPath = configPath;
        }

        /// <summary>
        /// Deletes any existing config file and generates a default one.
        /// </summary>
        public void GenerateDefault()
        {
            if (File.Exists(configPath)) File.Delete(configPath);

            string defaultConfig = "# Default configuration file for the Phi server\n" +
                                   "# Comment out lines with a '#' to prevent them from being read. Defaults will be used for missing values.\n" +
                                   "\n" +
                                   $"# Local IP address and port that the server will run on. Defaults to any local address and port {ConnectionDefaults.DEFAULT_SERVER_PORT}.\n" +
                                   "# Only change these if you really have to.\n" +
                                   $"#{ADDRESS_KEY}=\n" +
                                   $"{PORT_KEY}={ConnectionDefaults.DEFAULT_SERVER_PORT}\n";

            File.WriteAllText(configPath, defaultConfig);
        }

        /// <summary>
        /// Loads values from the config file. Generates a default config file if it does not already exist
        /// </summary>
        public void Load()
        {
            if (File.Exists(configPath))
            {
                string[] lines = File.ReadAllLines(configPath);
                foreach (string line in lines)
                {
                    // Check if the line is not commented and contains a key-value pair
                    if (!line.StartsWith("#") && line.Split('=').Length > 1)
                    {
                        string key = line.Split('=')[0];
                        string value = line.Split('=')[1];

                        // Try parse the value respective to its key and use defaults if parsing fails
                        switch (key)
                        {
                            case ADDRESS_KEY:
                                if (!tryParseAddress(value, out Address))
                                    Address = IPAddress.Any;
                                break;
                            case PORT_KEY:
                                if (!tryParsePort(value, out Port))
                                    Port = ConnectionDefaults.DEFAULT_SERVER_PORT;
                                break;
                        }
                    }
                }
            }
            else
            {
                GenerateDefault();
            }
        }

        /// <summary>
        /// Tries to parse the given value as an IP address.
        /// Returns true if the address was successfully parsed, otherwise false.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="parsedValue">Parsed result</param>
        /// <returns>Successfully parsed value</returns>
        private bool tryParseAddress(string value, out IPAddress parsedValue)
        {
            if (IPAddress.TryParse(value, out parsedValue))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to parse the given value as a port number between 1 and 65535 inclusive.
        /// Returns true if the port was successfully parsed, otherwise false.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="parsedValue">Parsed result</param>
        /// <returns>Successfully parsed value</returns>
        private bool tryParsePort(string value, out int parsedValue)
        {
            if (int.TryParse(value, out parsedValue))
            {
                if (parsedValue > 0 && parsedValue < 65536)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
