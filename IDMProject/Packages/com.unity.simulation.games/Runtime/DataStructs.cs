using System.Collections;
using System.Collections.Generic;
using Unity.RemoteConfig;
using UnityEngine;

namespace Unity.Simulation.Games
{
    /// <summary>
    /// Struct used to fetch values from keys in the GameSim run.
    /// </summary>
    public struct GameSimConfigResponse
    {
        /// <summary>
        /// Retrieves the int value of a corresponding key, if one exists.
        /// </summary>
        /// <param name="key">The key identifying the corresponding setting.</param>
        /// <param name="defaultValue">The default value to use if the specified key cannot be found or is unavailable.</param>
        public int GetInt(string key, int defaultValue = 0)
        {
            return ConfigManager.appConfig.GetInt(key, defaultValue);
        }

        /// <summary>
        /// Retrieves the boolean value of a corresponding key, if one exists.
        /// </summary>
        /// <param name="key">The key identifying the corresponding setting.</param>
        /// <param name="defaultValue">The default value to use if the specified key cannot be found or is unavailable.</param>
        public bool GetBool(string key, bool defaultValue = false)
        {
            return ConfigManager.appConfig.GetBool(key, defaultValue);
        }

        /// <summary>
        /// Retrieves the float value of a corresponding key from the remote service, if one exists.
        /// </summary>
        /// <param name="key">The key identifying the corresponding setting.</param>
        /// <param name="defaultValue">The default value to use if the specified key cannot be found or is unavailable.</param>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            return ConfigManager.appConfig.GetFloat(key, defaultValue);
        }

        /// <summary>
        /// Retrieves the long value of a corresponding key from the remote service, if one exists.
        /// </summary>
        /// <param name="key">The key identifying the corresponding setting.</param>
        /// <param name="defaultValue">The default value to use if the specified key cannot be found or is unavailable.</param>
        public long GetLong(string key, long defaultValue = 0L)
        {
            return ConfigManager.appConfig.GetLong(key, defaultValue);
        }

        /// <summary>
        /// Retrieves the string value of a corresponding key from the remote service, if one exists.
        /// </summary>
        /// <param name="key">The key identifying the corresponding setting.</param>
        /// <param name="defaultValue">The default value to use if the specified key cannot be found or is unavailable.</param>
        public string GetString(string key, string defaultValue = "")
        {
            return ConfigManager.appConfig.GetString(key, defaultValue);
        }
    }
}
