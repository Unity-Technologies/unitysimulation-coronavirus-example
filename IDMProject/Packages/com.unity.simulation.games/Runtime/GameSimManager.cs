using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Unity.RemoteConfig;
using Newtonsoft.Json.Linq;

using Unity.Simulation;

using UnityEngine;

namespace Unity.Simulation.Games
{
    public class GameSimManager
    {
        public static GameSimManager Instance
        {
            get
            {
                return _instance;
            }
        }

        internal string RunId { get; }

        internal int InstanceId { get; }

        private int _countersSequence = 0;

        private bool _isShuttingDown = false;

        internal Func<string> AddMetaData;

        internal string AttemptId 
        { 
            get
            {
                return Configuration.Instance.GetAttemptId();
            }
        }

        internal T GetAppParams<T>()
        {
            return Configuration.Instance.GetAppParams<T>();
        }

        internal Counter GetCounter(string name)
        {
            lock(_mutex)
            {
                if (!_counters.ContainsKey(name))
                {
                    var counter = new Counter(name);
                    _counters[name] = counter;
                }
                return _counters[name];
            }
        }

        /// <summary>
        /// Increment the counter with a certain number
        /// </summary>
        /// <param name="name">Name of the counter that needs to be incremented</param>
        /// <param name="amount">Amount by which it needs to be incremented.</param>
        public void IncrementCounter(string name, Int64 amount)
        {
            if (!_isShuttingDown)
            {
                GetCounter(name).Increment(amount);
            }
        }

        /// <summary>
        /// Sets the counter to the value passed in
        /// </summary>
        /// <param name="name">Name of the counter that needs to be incremented</param>
        /// <param name="value">Value to set the counter to.</param>
        public void SetCounter(string name, Int64 value)
        {
            if (!_isShuttingDown)
            {
                GetCounter(name).Reset(value);
            }
        }

        /// <summary>
        /// Resets the counter back to 0
        /// </summary>
        /// <param name="name">Name of the counter</param>
        public void ResetCounter(string name)
        {
            SetCounter(name, 0);
        }

        //
        // Non-public
        //

        object _mutex = new object();

        Dictionary<string, Counter> _counters = new Dictionary<string, Counter>();

        GameSimManager()
        {
            Log.I("Initializing the Game Simulation package");
            Manager.Instance.ShutdownNotification += ShutdownHandler;
        }

        static readonly GameSimManager _instance = new GameSimManager();

        [Serializable]
        struct Counters
        {
            public Metadata metadata;
            public Counter[] items;
            public Counters(int size, Metadata metadata)
            {
                items = new Counter[size];
                this.metadata = metadata;
            }
        }

        [Serializable]
        struct Metadata
        {
            public string instanceId;
            public string attemptId;
            public string gameSimSettings;

            public Metadata(string instanceId, string attemptId)
            {
                this.instanceId = instanceId;
                this.attemptId = attemptId;
                this.gameSimSettings = "";
            }
        }
        
        /// <summary>
        /// This function flushes a particular counter to the file system. One can choose to not reset the counter if required
        /// </summary>
        /// <param name="counter">Name of the counter. (It is also the name of the file)</param>
        /// <param name="consumer">If you want to perform any operations on the file once it is generated. Write to the FS happens on a background thread</param>
        /// <param name="resetCounter">Resets the counter. Set to true by default</param>
        internal void ResetAndFlushCounterToDisk(string counter, Action<string> consumer = null, bool resetCounter = true)
        {
            var asyncWriteRequest = Manager.Instance.CreateRequest<AsyncRequest<String>>();
            Int64 currentCount = -1;
            
            lock (_mutex)
            {
                if (!_counters.ContainsKey(counter))
                    return;
                asyncWriteRequest.data = JsonUtility.ToJson(_counters[counter]);
                currentCount = _counters[counter]._count++;
                if (resetCounter)
                {
                    _counters[counter].Reset();   
                }
            }
            
            asyncWriteRequest.Start((request) =>
            {
                var filePath = Path.Combine(Manager.Instance.GetDirectoryFor("GameSim"),
                    counter + "_" + currentCount + ".json");
                FileProducer.Write(
                    filePath,
                    Encoding.ASCII.GetBytes(request.data));
                consumer?.Invoke(filePath);
                return AsyncRequest.Result.Completed;
            });

        }

        /// <summary>
        /// Flush all Counters in memory to the file system. This will create a file named counters_{sequencenumber}
        /// This can be called at any time you want to capture the state of the counters
        /// </summary>
        /// <param name="resetCounters">This tells if the counters needs to be reset. By default its set to true.</param>
        internal void FlushAllCountersToDiskAndReset(bool resetCounters = true)
        {
            FlushCountersToDisk();

            if (resetCounters)
            {
                ResetCounters();   
            }
        }

        void ResetCounters()
        {
            foreach (var kvp in _counters)
            {
                Counter c = GetCounter(kvp.Key);
                c.Reset();
            }
        }

        void ShutdownHandler()
        {
            _isShuttingDown = true;
            
            FlushCountersToDisk();
        }

        void FlushCountersToDisk()
        {
            Log.I("Flushing counters to disk");
            string json = null;

            lock(_mutex)
            {
                Counters counters = new Counters(_counters.Count, new Metadata()
                {
                    attemptId = Configuration.Instance.GetAttemptId(),
                    instanceId =  Configuration.Instance.GetInstanceId(),
                    gameSimSettings = AddMetaData?.Invoke()
                });

                int index = 0;
                foreach (var kvp in _counters)
                    counters.items[index++] = _counters[kvp.Key];
                json = JsonUtility.ToJson(counters);
            }

            if (json != null)
            {
                Log.I("Writing the GameSim Counters file..");
                var fileName = "counters_" + _countersSequence.ToString() + ".json";
                FileProducer.Write(Path.Combine(Manager.Instance.GetDirectoryFor("GameSim"), fileName), Encoding.ASCII.GetBytes(json));
                _countersSequence++;
            }
        }

        /// <summary>
        /// Fetch the GameSim config for this instance.
        /// </summary>
        /// <param name="configFetchCompleted">When the fetch is completed, the action with be called with a GameSimConfigResponse object
        /// that can be used to get GameSim values for the keys in the simulation.</param>
        public void FetchConfig(Action<GameSimConfigResponse> configFetchCompleted)
        {
            RemoteConfigProvider.Instance.FetchRemoteConfig(configFetchCompleted);
        }

    }
}
