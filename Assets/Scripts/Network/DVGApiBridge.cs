// Filename: DVGApiBridge.cs
// Drop in Assets/Scripts/Network/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Assets.Scripts.Data; // For CharacterDTO, InventoryDTO, etc.
using TagDebugSystem;


namespace DevionGames.CharacterSystem
{
    public static class DVGApiBridge
    {
        public static string BaseUrl = "http://127.0.0.1:5000"; // Set to your Flask API

        private static string TokenKey = "jwt_token";
        private static string RefreshKey = "jwt_refresh";
        public static string GetToken()
        {
            string token = PlayerPrefs.GetString(TokenKey, "");
            TD.Verbose(Tags.Network, $"GetToken called, found: {(string.IsNullOrEmpty(token) ? "NO" : "YES")}", null);
            return token;
        }
        public static void SetToken(string token)
        {
            TD.Info(Tags.Network, $"SetToken called, length: {token?.Length ?? 0}", null);
            PlayerPrefs.SetString(TokenKey, token);
        }
        public static void SetRefresh(string token)
        {
            TD.Info(Tags.Network, "SetRefresh called.", null);
            PlayerPrefs.SetString(RefreshKey, token);
        }

        private static void SetAuth(UnityWebRequest req)
        {
            string token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                req.SetRequestHeader("Authorization", $"Bearer {token}");
                TD.Verbose(Tags.Network, "SetAuth: Authorization header set.", null);
            }
            else
            {
                TD.Warning(Tags.Network, "SetAuth: No JWT token found!", null);
            }
        }

        // --------- Auth ---------

        public static async Task<AuthResponse> Login(string username, string password)
        {
            TD.Info(Tags.Login, $"Login called for username: {username}", null);
            string url = BaseUrl + "/auth/login";
            var payload = new { username, password };
            var json = JsonUtility.ToJson(payload);

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                var response = req.downloadHandler.text;
                var result = JsonUtility.FromJson<AuthResponse>(response);
                if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(result.access_token))
                {
                    SetToken(result.access_token);
                    SetRefresh(result.refresh_token);
                    TD.Info(Tags.Login, $"Login SUCCESS for username: {username}", null);
                }
                else
                {
                    TD.Error(Tags.Login, $"Login failed for username: {username}. Error: {req.error}, Response: {response}", null);
                }
                return result;
            }
        }

        public static async Task<AuthResponse> Register(string username, string email, string password)
        {
            TD.Info(Tags.Login, $"Register called for username: {username}, email: {email}", null);
            string url = BaseUrl + "/auth/register";
            var payload = new { username, email, password };
            var json = JsonUtility.ToJson(payload);
            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                var response = req.downloadHandler.text;
                var result = JsonUtility.FromJson<AuthResponse>(response);
                if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(result.access_token))
                {
                    SetToken(result.access_token);
                    SetRefresh(result.refresh_token);
                    TD.Info(Tags.Login, $"Register SUCCESS for username: {username}", null);
                }
                else
                {
                    TD.Error(Tags.Login, $"Register failed for username: {username}. Error: {req.error}, Response: {response}", null);
                }
                return result;
            }
        }

        // --------- Characters ---------

        public static async Task<List<CharacterDTO>> GetCharacters(string worldKey)
        {
            TD.Info(Tags.Character, $"GetCharacters called for worldKey: {worldKey}", null);
            string url = BaseUrl + $"/characters/{worldKey}";
            using (var req = UnityWebRequest.Get(url))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tags.Character, $"GetCharacters failed: {req.error}, URL: {url}", null);
                    return new List<CharacterDTO>();
                }
                string json = req.downloadHandler.text;
                TD.Verbose(Tags.Character, $"GetCharacters JSON: {json}", null);
                return new List<CharacterDTO>(JsonHelper.FromJson<CharacterDTO>(json));
            }
        }

        public static async Task<CharacterDTO> GetCharacterById(string characterId)
        {
            TD.Info(Tags.Character, $"GetCharacterById called: {characterId}", null);
            string url = BaseUrl + $"/characters/by-id/{characterId}";
            using (var req = UnityWebRequest.Get(url))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tags.Character, $"GetCharacterById failed: {req.error}, ID: {characterId}", null);
                    return null;
                }
                var result = JsonUtility.FromJson<CharacterDTO>(req.downloadHandler.text);
                TD.Verbose(Tags.Character, $"GetCharacterById result: {req.downloadHandler.text}", null);
                return result;
            }
        }

        public static async Task<string> SaveCharacter(string worldKey, CharacterDTO character)
        {
            TD.Info(Tags.Character, $"SaveCharacter called for worldKey: {worldKey}", null);

            if (string.IsNullOrEmpty(worldKey))
            {
                TD.Error(Tags.Character, "SaveCharacter failed: worldKey is null or empty", null);
                return null;
            }

            if (character == null)
            {
                TD.Error(Tags.Character, "SaveCharacter failed: character is null", null);
                return null;
            }

            string url = BaseUrl + "/characters";

            // Serialize character data and escape it properly for JSON string
            string characterJson = JsonUtility.ToJson(character);
            string escapedCharacterJson = characterJson.Replace("\"", "\\\"");

            // Manually construct the JSON to ensure character_data is a string
            string json = $"{{\"world_key\":\"{worldKey}\",\"character_data\":\"{escapedCharacterJson}\"}}";

            TD.Info(Tags.Character, $"Final payload: {json}", null);

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                SetAuth(req);
                req.SetRequestHeader("Content-Type", "application/json");

                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tags.Character, $"SaveCharacter failed: {req.error}, Payload: {json}", null);
                }
                else
                {
                    TD.Verbose(Tags.Character, $"SaveCharacter success, Response: {req.downloadHandler.text}", null);
                }
                return req.downloadHandler.text;
            }
        }

        public static async Task<bool> DeleteCharacter(string worldKey, string characterId)
        {
            TD.Info(Tags.Character, $"DeleteCharacter called: {characterId} in world {worldKey}", null);
            string url = BaseUrl + $"/characters/{worldKey}/{characterId}";
            using (var req = UnityWebRequest.Delete(url))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                bool success = req.result == UnityWebRequest.Result.Success;
                if (success)
                {
                    TD.Info(Tags.Character, $"DeleteCharacter succeeded: {characterId}", null);
                }
                else
                {
                    TD.Error(Tags.Character, $"DeleteCharacter failed: {req.error}, URL: {url}", null);
                }
                return success;
            }
        }

        // --------- Inventory ---------

        public static async Task<InventoryDTO> GetInventory(string worldKey, string key, string scene)
        {
            TD.Info(Tags.Inventory, $"GetInventory called: worldKey={worldKey}, key={key}, scene={scene}", null);
            string url = BaseUrl + $"/inventory/{worldKey}/{key}/{scene}";
            using (var req = UnityWebRequest.Get(url))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tags.Inventory, $"GetInventory failed: {req.error}, URL: {url}", null);
                    return new InventoryDTO();
                }
                var dto = JsonUtility.FromJson<InventoryDTO>(req.downloadHandler.text);
                TD.Verbose(Tags.Inventory, $"GetInventory result: {req.downloadHandler.text}", null);
                return dto;
            }
        }

        public static async Task<string> SaveInventory(string worldKey, string key, string scene, InventoryDTO inventory)
        {
            TD.Info(Tags.Inventory, $"SaveInventory called: worldKey={worldKey}, key={key}, scene={scene}", null);
            string url = BaseUrl + "/inventory";
            var payload = new
            {
                world_key = worldKey,
                key = key,
                scene = scene,
                ui_data = inventory.ui_data,
                scene_data = inventory.scene_data
            };
            var json = JsonUtility.ToJson(payload);
            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                SetAuth(req);
                req.SetRequestHeader("Content-Type", "application/json");
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tags.Inventory, $"SaveInventory failed: {req.error}, Payload: {json}", null);
                }
                else
                {
                    TD.Verbose(Tags.Inventory, $"SaveInventory success, Response: {req.downloadHandler.text}", null);
                }
                return req.downloadHandler.text;
            }
        }

        public static async Task<bool> DeleteInventory(string worldKey, string key, string scene)
        {
            TD.Info(Tags.Inventory, $"DeleteInventory called: worldKey={worldKey}, key={key}, scene={scene}", null);
            string url = BaseUrl + $"/inventory/{worldKey}/{key}/{scene}";
            using (var req = UnityWebRequest.Delete(url))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                bool success = req.result == UnityWebRequest.Result.Success;
                if (success)
                {
                    TD.Info(Tags.Inventory, "DeleteInventory succeeded.", null);
                }
                else
                {
                    TD.Error(Tags.Inventory, $"DeleteInventory failed: {req.error}, URL: {url}", null);
                }
                return success;
            }
        }

        // --------- Quests ---------

        public static async Task<QuestsDTO> GetQuests(string worldKey, string key)
        {
            TD.Info(Tags.Quest, $"GetQuests called: worldKey={worldKey}, key={key}", null);
            string url = BaseUrl + $"/quests/{worldKey}/{key}";
            using (var req = UnityWebRequest.Get(url))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tags.Quest, $"GetQuests failed: {req.error}, URL: {url}", null);
                    return new QuestsDTO();
                }
                var dto = JsonUtility.FromJson<QuestsDTO>(req.downloadHandler.text);
                TD.Verbose(Tags.Quest, $"GetQuests result: {req.downloadHandler.text}", null);
                return dto;
            }
        }

        public static async Task<string> SaveQuests(string worldKey, string key, QuestsDTO quests)
        {
            TD.Info(Tags.Quest, $"SaveQuests called: worldKey={worldKey}, key={key}", null);
            string url = BaseUrl + "/quests";
            var payload = new
            {
                world_key = worldKey,
                key = key,
                active_quests = quests.active_quests,
                completed_quests = quests.completed_quests,
                failed_quests = quests.failed_quests
            };
            var json = JsonUtility.ToJson(payload);
            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                SetAuth(req);
                req.SetRequestHeader("Content-Type", "application/json");
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tags.Quest, $"SaveQuests failed: {req.error}, Payload: {json}", null);
                }
                else
                {
                    TD.Verbose(Tags.Quest, $"SaveQuests success, Response: {req.downloadHandler.text}", null);
                }
                return req.downloadHandler.text;
            }
        }

        public static async Task<bool> DeleteQuests(string worldKey, string key)
        {
            TD.Info(Tags.Quest, $"DeleteQuests called: worldKey={worldKey}, key={key}", null);
            string url = BaseUrl + $"/quests/{worldKey}/{key}";
            using (var req = UnityWebRequest.Delete(url))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                bool success = req.result == UnityWebRequest.Result.Success;
                if (success)
                {
                    TD.Info(Tags.Quest, "DeleteQuests succeeded.", null);
                }
                else
                {
                    TD.Error(Tags.Quest, $"DeleteQuests failed: {req.error}, URL: {url}", null);
                }
                return success;
            }
        }

        // --------- Stats ---------

        public static async Task<StatsDTO> GetStats(string worldKey, string key)
        {
            TD.Info(Tags.Stats, $"GetStats called: worldKey={worldKey}, key={key}", null);
            string url = BaseUrl + $"/stats/{worldKey}/{key}";
            using (var req = UnityWebRequest.Get(url))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tags.Stats, $"GetStats failed: {req.error}, URL: {url}", null);
                    return new StatsDTO();
                }
                var dto = JsonUtility.FromJson<StatsDTO>(req.downloadHandler.text);
                TD.Verbose(Tags.Stats, $"GetStats result: {req.downloadHandler.text}", null);
                return dto;
            }
        }

        public static async Task<string> SaveStats(string worldKey, string key, StatsDTO stats)
        {
            TD.Info(Tags.Stats, $"SaveStats called: worldKey={worldKey}, key={key}", null);
            string url = BaseUrl + "/stats";
            var payload = new
            {
                world_key = worldKey,
                key = key,
                stats_json = stats.stats_json
            };
            var json = JsonUtility.ToJson(payload);
            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                SetAuth(req);
                req.SetRequestHeader("Content-Type", "application/json");
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(Tags.Stats, $"SaveStats failed: {req.error}, Payload: {json}", null);
                }
                else
                {
                    TD.Verbose(Tags.Stats, $"SaveStats success, Response: {req.downloadHandler.text}", null);
                }
                return req.downloadHandler.text;
            }
        }

        public static async Task<bool> DeleteStats(string worldKey, string key)
        {
            TD.Info(Tags.Stats, $"DeleteStats called: worldKey={worldKey}, key={key}", null);
            string url = BaseUrl + $"/stats/{worldKey}/{key}";
            using (var req = UnityWebRequest.Delete(url))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                bool success = req.result == UnityWebRequest.Result.Success;
                if (success)
                {
                    TD.Info(Tags.Stats, "DeleteStats succeeded.", null);
                }
                else
                {
                    TD.Error(Tags.Stats, $"DeleteStats failed: {req.error}, URL: {url}", null);
                }
                return success;
            }
        }

        // --------- Auth helper ---------
        public static async Task<AuthResponse> LoginAndGetJwtAsync(string username, string password)
        {
            TD.Info(Tags.Login, $"LoginAndGetJwtAsync called for username: {username}", null);
            string url = BaseUrl + "/auth/login";
            AuthRequest payload = new AuthRequest { username = username, password = password };
            string jsonBody = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                var operation = req.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (req.result == UnityWebRequest.Result.Success)
#else
                if (!req.isNetworkError && !req.isHttpError)
#endif
                {
                    TD.Info(Tags.Login, $"LoginAndGetJwtAsync succeeded for username: {username}", null);
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
                    return response;
                }
                else
                {
                    TD.Error(Tags.Login, $"LoginAndGetJwtAsync failed: {req.responseCode} - {req.error} - {req.downloadHandler.text}", null);
                    return new AuthResponse { error = req.error, message = req.downloadHandler.text };
                }
            }
        }

        [System.Serializable]
        public class AuthRequest
        {
            public string username;
            public string password;
        }

        [System.Serializable]
        public class AuthResponse
        {
            public string id;
            public string username;
            public string access_token;
            public string refresh_token;
            public int expires_in;
            public string token_type;
            public string message;
            public string error;
        }

        // --- Helper for Unity's broken array parsing ---
        public static class JsonHelper
        {
            public static T[] FromJson<T>(string json)
            {
                string newJson = "{\"array\":" + json + "}";
                Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
                return wrapper.array;
            }

            [Serializable]
            private class Wrapper<T> { public T[] array; }
        }
    }
}
