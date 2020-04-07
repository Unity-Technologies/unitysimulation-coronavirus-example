using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Simulation.Games.Editor
{
    internal static class GameSimApiClient
    {
        public static string UploadBuild(string name, string location)
        {
            return Transaction.Upload($"https://api.prd.gamesimulation.unity3d.com/v1/builds?projectId={Application.cloudProjectId}", name, location);
        }
    }

    internal static class Transaction
    {
        public static string Upload(string url, string name, string inFile, bool useTransferUrls = true)
        {
            return Upload(url, name, File.ReadAllBytes(inFile), useTransferUrls);
        }

        public static string Upload(string url, string name, byte[] data, bool useTransferUrls = true)
        {
            string entityId = null;

            Action<UnityWebRequest> action = (UnityWebRequest webrx) =>
            {
                var headers = Utils.GetAuthHeader(CloudProjectSettings.accessToken);
                foreach (var k in headers)
                    webrx.SetRequestHeader(k.Key, k.Value);

                webrx.uploadHandler = new UploadHandlerRaw(data);
                webrx.SendWebRequest();
                while (!webrx.isDone)
                    ;

                if (webrx.isNetworkError || webrx.isHttpError)
				{
					Debug.LogError("Failed to upload with error \n" + webrx.error + "\n" + webrx.downloadHandler.text);
                    return;

				}

				if (!string.IsNullOrEmpty(webrx.downloadHandler.text))
                {
                    Debug.Assert(false, "Need to pull id from response");
                    // set entity return id here
                }
            };

            if (useTransferUrls)
            {
                var tuple = GetUploadURL(url, name);
                entityId = tuple.Item2;
                using (var webrx = UnityWebRequest.Put(tuple.Item1, data))
                {
                    action(webrx);
                }
            }
            else
            {
                using (var webrx = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    action(webrx);
                }
            }

            Debug.Assert(!string.IsNullOrEmpty(entityId));

            return entityId;
        }

        public static Tuple<string, string> GetUploadURL(string url, string path)
        {
            var payload = JsonUtility.ToJson(new UploadInfo(Path.GetFileName(path), "Placeholder description"));

            using (var webrx = UnityWebRequest.Post(url, payload))
            {
                var headers = Utils.GetAuthHeader(CloudProjectSettings.accessToken);
                foreach (var k in headers)
                    webrx.SetRequestHeader(k.Key, k.Value);

                webrx.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                webrx.timeout = 30;
                webrx.SendWebRequest();
                while (!webrx.isDone)
                    ;

                if (webrx.isNetworkError || webrx.isHttpError)
                {
                    throw new Exception("Failed to generate upload URL with error: " + webrx.error + "\n" + webrx.downloadHandler.text);
                }
                    
                var data = JsonUtility.FromJson<UploadUrlData>(webrx.downloadHandler.text);
                return new Tuple<string, string>(data.upload_uri, data.id);
            }
        }
    }

    [Serializable]
    internal struct UploadInfo
    {
        public string name;
        public string description;
        public UploadInfo(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }
#pragma warning disable CS0649
    [Serializable]
    internal struct UploadUrlData
    {
        public string id;
        public string upload_uri;
    }
#pragma warning restore CS0649
    internal static class Utils
    {
        internal static Dictionary<string, string> GetAuthHeader(string tokenString)
        {
            var dict = new Dictionary<string, string>();
            AddUserAgent(dict);
            AddContentTypeApplication(dict);
            AddAuth(dict, tokenString);
            return dict;
        }

        static void AddContentTypeApplication(Dictionary<string, string> dict)
        {
            dict["Content-Type"] = "application/json";
        }

        //TODO: Solve this somehow
        static void AddAuth(Dictionary<string, string> dict, string tokenString)
        {
            dict["Authorization"] = "Bearer " + tokenString;
        }

        //TODO: probs should change this bad boy
        static void AddUserAgent(Dictionary<string, string> dict)
        {
            dict["User-Agent"] = "gamesim/0.3.0-preview.4";
        }
    }
}
