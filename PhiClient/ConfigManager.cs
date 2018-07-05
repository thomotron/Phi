using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PhiClient
{
    /// <summary>
    /// Manages a configuration file and allows attribute storage and retrieval.
    /// </summary>
    public class ConfigManager
    {
        private Dictionary<string, object> config;
        private string configPath;

        /// <summary>
        /// Instantiates an empty ConfigManager object.
        /// </summary>
        /// <param name="configPath">Path to the configuration file</param>
        public ConfigManager(string configPath)
        {
            this.config = new Dictionary<string, object>();
            this.configPath = configPath;
        }

        /// <summary>
        /// Returns the value related to <c>key</c> if <c>key</c> exists.
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        /// <exception cref="TypeMismatchException">Thrown if type <c>T</c> does not match the value type</exception>
        /// <exception cref="KeyNotFoundException">Thrown if <c>key</c> cannot be found in the loaded config</exception>
        public T GetValue<T>(string key)
        {
            object value;
            if (config.TryGetValue(key, out value))
            {
                if (value is T variable)
                {
                    return variable;
                }
                else
                {
                    throw new TypeMismatchException(typeof(T), value.GetType());
                }
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        /// <summary>
        /// Tries to retrieve the value related to <c>key</c> if <c>key</c> exists.
        /// Returns true if the value was retrieved successfully, otherwise false.
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="value">Result value</param>
        /// <returns>Value retrieved successfully</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            try
            {
                value = this.GetValue<T>(key);
                return true;
            }
            catch (Exception e)
            {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Adds or updates a key-value pair.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public void SetValue(string key, object value)
        {
            if (config.ContainsKey(key))
            {
                config[key] = value;
            }
            else
            {
                config.Add(key, value);
            }
        }

        /// <summary>
        /// Clears all config entries.
        /// </summary>
        public void Clear()
        {
            config.Clear();
        }

        /// <summary>
        /// Loads the config file.
        /// </summary>
        public void Load()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = File.OpenRead(configPath))
            {
                config = (Dictionary<string, object>) bf.Deserialize(fs);
            }
        }

        /// <summary>
        /// Saves the config file.
        /// </summary>
        public void Save()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = File.OpenWrite(configPath))
            {
                bf.Serialize(fs, config);
            }
        }
    }
}