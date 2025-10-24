using System;
using Unity.Simulation;
using UnityEngine;
using Unity.RemoteConfig;


namespace Unity.Simulation.Games
{
    internal class RemoteConfigProvider
    {
        private RemoteConfigProvider() { }

        public static RemoteConfigProvider Instance { get; } = new RemoteConfigProvider();

        public struct UserAttributes
        {
            public long gameSimInstanceId;
            public string gameSimDefinitionId;
            public string gameSimExecutionId;
            public string gameSimDecisionEngineId;
            public string gameSimDecisionEngineType;
        }
        
        [Serializable]
        public struct GameSimAppParams
        {
            public string gameSimDecisionEngineId;
            public string gameSimDecisionEngineType;
            public string environmentId;
        }

        private Action<GameSimConfigResponse> ConfigHandler;

        public void FetchRemoteConfig(Action<GameSimConfigResponse> remoteConfigFetchComplete = null)
        {
            Log.I("Fetching App Config from Remote Config");
            ConfigManager.FetchCompleted += Instance.ApplyRemoteConfigChanges;
            long num = 0;

            UserAttributes _userAtt = default(UserAttributes);
            if (Configuration.Instance.IsSimulationRunningInCloud())
            {
                string execution_id = Configuration.Instance.SimulationConfig.execution_id.Split(':')[2];
                string definition_id = Configuration.Instance.SimulationConfig.definition_id.Split(':')[2];

                Debug.Log("Instance id = " + Configuration.Instance.GetInstanceId().ToString()
                                           + " execution id - " + execution_id.ToString()
                                           + " defitnion id - " + definition_id.ToString());
                var appParams = Configuration.Instance.GetAppParams<GameSimAppParams>();
                if (Int64.TryParse(Configuration.Instance.GetInstanceId(), out num))
                {
                    _userAtt = new UserAttributes()
                    {
                        gameSimInstanceId = num,
                        gameSimExecutionId = execution_id,
                        gameSimDefinitionId = definition_id,
                        gameSimDecisionEngineId = appParams.gameSimDecisionEngineId,
                        gameSimDecisionEngineType = appParams.gameSimDecisionEngineType
                    };
                }
                ConfigManager.SetEnvironmentID(appParams.environmentId);
            }

            ConfigHandler = remoteConfigFetchComplete;
            ConfigManager.FetchConfigs(_userAtt, new UserAttributes());
        }

        void ApplyRemoteConfigChanges(ConfigResponse response)
        {
            Log.I("Remote Config fetched with response " + response.status + " with origin " + response.requestOrigin);
            switch (response.requestOrigin)
            {
                case ConfigOrigin.Default:
                    Debug.Log("No settings loaded this session; using default values.");
                    break;
                case ConfigOrigin.Cached:
                    Debug.Log("No settings loaded this session; using cached values from a previous session.");
                    break;
                case ConfigOrigin.Remote:
                    GameSimManager.Instance.AddMetaData = () => ConfigManager.appConfig.config.ToString(Newtonsoft.Json.Formatting.None);
                    ConfigHandler?.Invoke(new GameSimConfigResponse());
                    break;
            }
        }
    }

}
