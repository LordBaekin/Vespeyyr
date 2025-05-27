using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TagDebugSystem;

namespace Vespeyr.Network
{
    public static class UnityHttpClient
    {
        // Set this to your server URL (no trailing slash)
        private const string BASE_URL = "http://127.0.0.1:5000";

        public struct HttpResult
        {
            public bool success;
            public string response;
            public int statusCode;
        }

        private const string Tag = "UnityHttpClient";

        public static async Task<string> GetAsync(string route)
        {
            string url = BASE_URL + route;
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                await req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tag, $"GET failed: {req.error}");
                    return null;
                }
                TD.Verbose(Tag, $"GET {url} success, status={req.responseCode}");
                return req.downloadHandler.text;
            }
        }

        public static async Task<HttpResult> PostAsync(string route, string json)
        {
            string url = BASE_URL + route;
            using (UnityWebRequest req = UnityWebRequest.Post(url, json, "application/json"))
            {
                req.SetRequestHeader("Content-Type", "application/json");
                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tag, $"POST {url} failed: {req.error} (status {req.responseCode})");
                }
                else
                {
                    TD.Verbose(Tag, $"POST {url} success, status={req.responseCode}");
                }

                return new HttpResult
                {
                    success = req.result == UnityWebRequest.Result.Success || req.responseCode == 200,
                    response = req.downloadHandler.text,
                    statusCode = (int)req.responseCode
                };
            }
        }

        public static async Task<HttpResult> PutAsync(string route, string json)
        {
            string url = BASE_URL + route;
            var req = new UnityWebRequest(url, "PUT");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                TD.Error(Tag, $"PUT {url} failed: {req.error} (status {req.responseCode})");
            }
            else
            {
                TD.Verbose(Tag, $"PUT {url} success, status={req.responseCode}");
            }

            return new HttpResult
            {
                success = req.result == UnityWebRequest.Result.Success || req.responseCode == 200,
                response = req.downloadHandler.text,
                statusCode = (int)req.responseCode
            };
        }

        public static async Task<HttpResult> DeleteAsync(string route)
        {
            string url = BASE_URL + route;
            using (UnityWebRequest req = UnityWebRequest.Delete(url))
            {
                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tag, $"DELETE {url} failed: {req.error} (status {req.responseCode})");
                }
                else
                {
                    TD.Verbose(Tag, $"DELETE {url} success, status={req.responseCode}");
                }

                return new HttpResult
                {
                    success = req.result == UnityWebRequest.Result.Success || req.responseCode == 200,
                    response = req.downloadHandler.text,
                    statusCode = (int)req.responseCode
                };
            }
        }
    }
}
