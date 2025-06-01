// HybridPersistenceBridge.cs (Corrected TD logging, restored missing methods)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TagDebugSystem;

namespace DevionGames.CharacterSystem
{
    public class HybridPersistenceBridge : MonoBehaviour
    {
        [Header("Save Provider ScriptableObject")]
        public SaveProviderSelectorSO providerAsset;

        [Header("API Configuration")]
        public string apiRoot = "http://127.0.0.1:5000";

        [Header("Context (set via DevionGamesAdapter)")]
        public string currentWorldKey = "";
        public string currentCharacterId = "";

        private string currentToken;
        public static HybridPersistenceBridge Instance { get; private set; }

        private const string TAG = "HybridBridge";

        public SaveProviderSelectorSO.SaveProvider Provider => providerAsset != null ? providerAsset.currentProvider : SaveProviderSelectorSO.SaveProvider.PlayerPrefs;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (providerAsset == null)
                providerAsset = SaveProviderSelectorSO.Instance;

            currentToken = PlayerPrefs.GetString("jwt_token", "");
        }

        public void SetAuthToken(string token)
        {
            currentToken = token;
            PlayerPrefs.SetString("jwt_token", token);
            PlayerPrefs.Save();
        }

        public void SetContext(string worldKey, string characterId)
        {
            currentWorldKey = worldKey;
            currentCharacterId = characterId;
        }

        public void SaveCharacterData(string characterJson)
        {
            TD.Info(TAG, $"Saving character locally: world={currentWorldKey}, char={currentCharacterId}", this);
            var accountKey = PlayerPrefs.GetString("Account", "Player");
            PlayerPrefs.SetString(accountKey, characterJson);
            PlayerPrefs.Save();
        }

        public void LoadCharacterData(Action<string> callback)
        {
            TD.Info(TAG, $"Loading character locally: world={currentWorldKey}, char={currentCharacterId}", this);
            var accountKey = PlayerPrefs.GetString("Account", "Player");
            callback(PlayerPrefs.GetString(accountKey, ""));
        }

        public void SaveInventoryData(string scene, string uiData, string sceneData)
        {
            TD.Info(TAG, $"Saving inventory: world={currentWorldKey}, char={currentCharacterId}, scene={scene}", this);

            if (Provider != SaveProviderSelectorSO.SaveProvider.Server)
            {
                var key = $"{currentCharacterId}_{scene}";
                PlayerPrefs.SetString(key, uiData);
                PlayerPrefs.SetString(key + "_scene", sceneData);
                var keys = GetInventoryKeys();
                if (!keys.Contains(currentCharacterId)) keys.Add(currentCharacterId);
                PlayerPrefs.SetString("InventorySystemSavedKeys", string.Join(";", keys));
                PlayerPrefs.Save();
            }

            if (Provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
                StartCoroutine(SaveInventoryToServer(scene, uiData, sceneData));
        }

        public void LoadInventoryData(string scene, Action<string, string> callback)
        {
            TD.Info(TAG, $"Loading inventory: world={currentWorldKey}, char={currentCharacterId}, scene={scene}", this);

            if (Provider == SaveProviderSelectorSO.SaveProvider.Server)
                StartCoroutine(LoadInventoryFromServer(scene, callback));
            else
            {
                var key = $"{currentCharacterId}_{scene}";
                var uiData = PlayerPrefs.GetString(key, "");
                var sceneData = PlayerPrefs.GetString(key + "_scene", "");
                callback(uiData, sceneData);
            }
        }

        public void SaveQuestData(string activeQuests, string completedQuests, string failedQuests)
        {
            TD.Info(TAG, $"Saving quests: world={currentWorldKey}, char={currentCharacterId}", this);

            if (Provider != SaveProviderSelectorSO.SaveProvider.Server)
            {
                PlayerPrefs.SetString($"{currentCharacterId}.ActiveQuests", activeQuests);
                PlayerPrefs.SetString($"{currentCharacterId}.CompletedQuests", completedQuests);
                PlayerPrefs.SetString($"{currentCharacterId}.FailedQuests", failedQuests);
                var keys = GetQuestKeys();
                if (!keys.Contains(currentCharacterId)) keys.Add(currentCharacterId);
                PlayerPrefs.SetString("QuestSystemSavedKeys", string.Join(";", keys));
                PlayerPrefs.Save();
            }

            if (Provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
                StartCoroutine(SaveQuestToServer(activeQuests, completedQuests, failedQuests));
        }

        public void LoadQuestData(Action<string, string, string> callback)
        {
            TD.Info(TAG, $"Loading quests: world={currentWorldKey}, char={currentCharacterId}", this);

            if (Provider == SaveProviderSelectorSO.SaveProvider.Server)
                StartCoroutine(LoadQuestFromServer(callback));
            else
                callback(
                    PlayerPrefs.GetString($"{currentCharacterId}.ActiveQuests", ""),
                    PlayerPrefs.GetString($"{currentCharacterId}.CompletedQuests", ""),
                    PlayerPrefs.GetString($"{currentCharacterId}.FailedQuests", "")
                );
        }

        public void SaveStatsData(string statsJson)
        {
            TD.Info(TAG, $"Saving stats: world={currentWorldKey}, char={currentCharacterId}", this);

            if (Provider != SaveProviderSelectorSO.SaveProvider.Server)
            {
                PlayerPrefs.SetString($"{currentCharacterId}.Stats", statsJson);
                var keys = GetStatsKeys();
                if (!keys.Contains(currentCharacterId)) keys.Add(currentCharacterId);
                PlayerPrefs.SetString("StatSystemSavedKeys", string.Join(";", keys));
                PlayerPrefs.Save();
            }

            if (Provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
                StartCoroutine(SaveStatsToServer(statsJson));
        }

        public void LoadStatsData(Action<string> callback)
        {
            TD.Info(TAG, $"Loading stats: world={currentWorldKey}, char={currentCharacterId}", this);

            if (Provider == SaveProviderSelectorSO.SaveProvider.Server)
                StartCoroutine(LoadStatsFromServer(callback));
            else
                callback(PlayerPrefs.GetString($"{currentCharacterId}.Stats", ""));
        }

        public void SaveString(string key, string value)
        {
            TD.Info(TAG, $"Saving string: key={key}", this);
            if (Provider != SaveProviderSelectorSO.SaveProvider.Server)
            {
                PlayerPrefs.SetString(key, value);
                PlayerPrefs.Save();
            }
            if (Provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
                StartCoroutine(SaveStringToServer(key, value));
        }

        public void LoadString(string key, string defaultValue, Action<string> callback)
        {
            TD.Info(TAG, $"Loading string: key={key}", this);
            if (Provider == SaveProviderSelectorSO.SaveProvider.Server)
                StartCoroutine(LoadStringFromServer(key, defaultValue, callback));
            else
                callback(PlayerPrefs.GetString(key, defaultValue));
        }

        private IEnumerator SaveInventoryToServer(string scene, string uiData, string sceneData)
        {
            var payload = new { world_key = currentWorldKey, key = currentCharacterId, scene, ui_data = uiData, scene_data = sceneData };
            yield return StartCoroutine(PostToServer("/inventory", payload));
        }

        private IEnumerator LoadInventoryFromServer(string scene, Action<string, string> callback)
        {
            var url = $"{apiRoot}/inventory/{currentWorldKey}/{currentCharacterId}/{scene}";
            yield return StartCoroutine(GetFromServer(url, raw => {
                if (!string.IsNullOrEmpty(raw))
                {
                    try { var r = JsonUtility.FromJson<InventoryResponse>(raw); callback(r.ui_data, r.scene_data); } catch { callback("", ""); }
                }
                else callback("", "");
            }));
        }

        private IEnumerator SaveQuestToServer(string active, string completed, string failed)
        {
            var payload = new { world_key = currentWorldKey, key = currentCharacterId, active_quests = active, completed_quests = completed, failed_quests = failed };
            yield return StartCoroutine(PostToServer("/quests", payload));
        }

        private IEnumerator LoadQuestFromServer(Action<string, string, string> callback)
        {
            var url = $"{apiRoot}/quests/{currentWorldKey}/{currentCharacterId}";
            yield return StartCoroutine(GetFromServer(url, raw => {
                if (!string.IsNullOrEmpty(raw))
                {
                    try { var r = JsonUtility.FromJson<QuestResponse>(raw); callback(r.active_quests, r.completed_quests, r.failed_quests); } catch { callback("", "", ""); }
                }
                else callback("", "", "");
            }));
        }

        private IEnumerator SaveStatsToServer(string statsJson)
        {
            var payload = new { world_key = currentWorldKey, key = currentCharacterId, stats_json = statsJson };
            yield return StartCoroutine(PostToServer("/stats", payload));
        }

        private IEnumerator LoadStatsFromServer(Action<string> callback)
        {
            var url = $"{apiRoot}/stats/{currentWorldKey}/{currentCharacterId}";
            yield return StartCoroutine(GetFromServer(url, raw => {
                if (!string.IsNullOrEmpty(raw))
                {
                    try { callback(JsonUtility.FromJson<StatsResponse>(raw).stats_json); } catch { callback(""); }
                }
                else callback("");
            }));
        }

        private IEnumerator SaveStringToServer(string key, string value)
        {
            var payload = new { key, value };
            yield return StartCoroutine(PostToServer("/sync/string", payload));
        }

        private IEnumerator LoadStringFromServer(string key, string defaultValue, Action<string> callback)
        {
            var url = $"{apiRoot}/sync/string/{key}";
            yield return StartCoroutine(GetFromServer(url, raw => {
                callback(string.IsNullOrEmpty(raw) ? defaultValue : raw);
            }));
        }

        private List<string> GetInventoryKeys() => new List<string>(PlayerPrefs.GetString("InventorySystemSavedKeys", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        private List<string> GetQuestKeys() => new List<string>(PlayerPrefs.GetString("QuestSystemSavedKeys", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        private List<string> GetStatsKeys() => new List<string>(PlayerPrefs.GetString("StatSystemSavedKeys", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

        private IEnumerator PostToServer(string endpoint, object payload)
        {
            var url = apiRoot + endpoint;
            var json = JsonUtility.ToJson(payload);
            TD.Info(TAG, $"Sending to {endpoint}: {json}", this);

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(currentToken))
                    req.SetRequestHeader("Authorization", $"Bearer {currentToken}");

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    TD.Error(TAG, $"Server save failed: {req.error}\nResponse Code: {req.responseCode}\nResponse Body: {req.downloadHandler.text}", this);
                }
                else
                {
                    TD.Info(TAG, $"Successfully saved to server: {endpoint}", this);
                }
            }
        }

        private IEnumerator GetFromServer(string url, Action<string> callback)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(currentToken))
                    req.SetRequestHeader("Authorization", $"Bearer {currentToken}");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string response = req.downloadHandler.text;
                    TD.Info(TAG, $"Server response from {url}: {response}", this);
                    callback(!string.IsNullOrEmpty(response) && response != "null" && response != "{}" && response.Trim() != "" ? response : "");
                }
                else
                {
                    TD.Error(TAG, $"Server load failed: {req.error}\nResponse Code: {req.responseCode}", this);
                    callback("");
                }
            }
        }

        [Serializable] private class InventoryResponse { public string ui_data, scene_data; }
        [Serializable] private class QuestResponse { public string active_quests, completed_quests, failed_quests; }
        [Serializable] private class StatsResponse { public string stats_json; }
    }
}
