using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// Fixed HybridPersistenceBridge that works with your existing DevionGames systems
/// </summary>
public class HybridPersistenceBridge : MonoBehaviour
{
    [Header("Save Provider ScriptableObject")]
    public SaveProviderSelectorSO providerAsset;

    [Header("API Configuration")]
    public string apiRoot = "http://127.0.0.1:5000";
    [Header("World Context")]
    public string currentWorldKey = "";
    public string currentCharacterId = "";

    public static HybridPersistenceBridge Instance { get; private set; }

    // Current JWT token for authentication
    private string currentToken;

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

        // Get token from PlayerPrefs if available
        currentToken = PlayerPrefs.GetString("jwt_token", "");
    }

    public SaveProviderSelectorSO.SaveProvider Provider
        => providerAsset != null ? providerAsset.currentProvider : SaveProviderSelectorSO.SaveProvider.PlayerPrefs;

    public void SetAuthToken(string token)
    {
        currentToken = token;
        PlayerPrefs.SetString("jwt_token", token);
    }

    public void SetContext(string worldKey, string characterId)
    {
        currentWorldKey = worldKey;
        currentCharacterId = characterId;
    }

    // ===================== DEVION GAMES SPECIFIC METHODS =====================

    /// <summary>
    /// Save inventory data (matches your existing InventorySystem pattern)
    /// </summary>
    public void SaveInventoryData(string scene, string uiData, string sceneData)
    {
        Debug.Log($"[HybridBridge] Saving inventory: world={currentWorldKey}, char={currentCharacterId}, scene={scene}");

        if (Provider != SaveProviderSelectorSO.SaveProvider.Server)
        {
            // Save to local PlayerPrefs (existing format)
            string key = $"{currentCharacterId}_{scene}";
            PlayerPrefs.SetString(key, uiData);
            PlayerPrefs.SetString(key + "_scene", sceneData);

            // Update keys list
            List<string> keys = GetInventoryKeys();
            if (!keys.Contains(currentCharacterId))
                keys.Add(currentCharacterId);
            PlayerPrefs.SetString("InventorySystemSavedKeys", string.Join(";", keys));
            PlayerPrefs.Save();
        }

        if (Provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
        {
            // Save to server
            StartCoroutine(SaveInventoryToServer(scene, uiData, sceneData));
        }
    }

    /// <summary>
    /// Load inventory data (matches your existing InventorySystem pattern)
    /// </summary>
    public void LoadInventoryData(string scene, System.Action<string, string> callback)
    {
        Debug.Log($"[HybridBridge] Loading inventory: world={currentWorldKey}, char={currentCharacterId}, scene={scene}");

        if (Provider == SaveProviderSelectorSO.SaveProvider.Server)
        {
            StartCoroutine(LoadInventoryFromServer(scene, callback));
        }
        else
        {
            string key = $"{currentCharacterId}_{scene}";
            string uiData = PlayerPrefs.GetString(key, "");
            string sceneData = PlayerPrefs.GetString(key + "_scene", "");
            callback(uiData, sceneData);
        }
    }

    /// <summary>
    /// Save quest data (matches your existing QuestSystem pattern)
    /// </summary>
    public void SaveQuestData(string activeQuests, string completedQuests, string failedQuests)
    {
        Debug.Log($"[HybridBridge] Saving quests: world={currentWorldKey}, char={currentCharacterId}");

        if (Provider != SaveProviderSelectorSO.SaveProvider.Server)
        {
            // Save to local PlayerPrefs (existing format)
            PlayerPrefs.SetString($"{currentCharacterId}.ActiveQuests", activeQuests);
            PlayerPrefs.SetString($"{currentCharacterId}.CompletedQuests", completedQuests);
            PlayerPrefs.SetString($"{currentCharacterId}.FailedQuests", failedQuests);

            // Update keys list
            List<string> keys = GetQuestKeys();
            if (!keys.Contains(currentCharacterId))
                keys.Add(currentCharacterId);
            PlayerPrefs.SetString("QuestSystemSavedKeys", string.Join(";", keys));
            PlayerPrefs.Save();
        }

        if (Provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
        {
            // Save to server
            StartCoroutine(SaveQuestToServer(activeQuests, completedQuests, failedQuests));
        }
    }

    /// <summary>
    /// Load quest data (matches your existing QuestSystem pattern)
    /// </summary>
    public void LoadQuestData(System.Action<string, string, string> callback)
    {
        Debug.Log($"[HybridBridge] Loading quests: world={currentWorldKey}, char={currentCharacterId}");

        if (Provider == SaveProviderSelectorSO.SaveProvider.Server)
        {
            StartCoroutine(LoadQuestFromServer(callback));
        }
        else
        {
            string active = PlayerPrefs.GetString($"{currentCharacterId}.ActiveQuests", "");
            string completed = PlayerPrefs.GetString($"{currentCharacterId}.CompletedQuests", "");
            string failed = PlayerPrefs.GetString($"{currentCharacterId}.FailedQuests", "");
            callback(active, completed, failed);
        }
    }

    /// <summary>
    /// Save stats data (matches your existing StatSystem pattern)
    /// </summary>
    public void SaveStatsData(string statsJson)
    {
        Debug.Log($"[HybridBridge] Saving stats: world={currentWorldKey}, char={currentCharacterId}");

        if (Provider != SaveProviderSelectorSO.SaveProvider.Server)
        {
            // Save to local PlayerPrefs (existing format)
            PlayerPrefs.SetString($"{currentCharacterId}.Stats", statsJson);

            // Update keys list
            List<string> keys = GetStatsKeys();
            if (!keys.Contains(currentCharacterId))
                keys.Add(currentCharacterId);
            PlayerPrefs.SetString("StatSystemSavedKeys", string.Join(";", keys));
            PlayerPrefs.Save();
        }

        if (Provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
        {
            // Save to server
            StartCoroutine(SaveStatsToServer(statsJson));
        }
    }

    /// <summary>
    /// Load stats data (matches your existing StatSystem pattern)
    /// </summary>
    public void LoadStatsData(System.Action<string> callback)
    {
        Debug.Log($"[HybridBridge] Loading stats: world={currentWorldKey}, char={currentCharacterId}");

        if (Provider == SaveProviderSelectorSO.SaveProvider.Server)
        {
            StartCoroutine(LoadStatsFromServer(callback));
        }
        else
        {
            string stats = PlayerPrefs.GetString($"{currentCharacterId}.Stats", "");
            callback(stats);
        }
    }

    /// <summary>
    /// Save character data (local only)
    /// </summary>
    public void SaveCharacterData(string characterJson)
    {
        Debug.Log($"[HybridBridge] Saving character locally: world={currentWorldKey}, char={currentCharacterId}");

        string accountKey = PlayerPrefs.GetString("Account", "Player");
        PlayerPrefs.SetString(accountKey, characterJson);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load character data (local only)
    /// </summary>
    public void LoadCharacterData(System.Action<string> callback)
    {
        Debug.Log($"[HybridBridge] Loading character locally: world={currentWorldKey}, char={currentCharacterId}");

        string accountKey = PlayerPrefs.GetString("Account", "Player");
        string data = PlayerPrefs.GetString(accountKey, "");
        callback(data);
    }

    // ===================== GENERIC STRING SAVE/LOAD =====================

    /// <summary>
    /// Save generic string data (for keys lists, etc.)
    /// </summary>
    public void SaveString(string key, string value)
    {
        Debug.Log($"[HybridBridge] Saving string: key={key}");

        if (Provider != SaveProviderSelectorSO.SaveProvider.Server)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        if (Provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
        {
            StartCoroutine(SaveStringToServer(key, value));
        }
    }

    /// <summary>
    /// Load generic string data (for keys lists, etc.)
    /// </summary>
    public void LoadString(string key, string defaultValue, System.Action<string> callback)
    {
        Debug.Log($"[HybridBridge] Loading string: key={key}");

        if (Provider == SaveProviderSelectorSO.SaveProvider.Server)
        {
            StartCoroutine(LoadStringFromServer(key, defaultValue, callback));
        }
        else
        {
            string value = PlayerPrefs.GetString(key, defaultValue);
            callback(value);
        }
    }

    // ===================== HELPER METHODS =====================

    private List<string> GetInventoryKeys()
    {
        string keyData = PlayerPrefs.GetString("InventorySystemSavedKeys", "");
        var keys = new List<string>();
        if (!string.IsNullOrEmpty(keyData))
        {
            keys.AddRange(keyData.Split(';'));
            keys.RemoveAll(x => string.IsNullOrEmpty(x));
        }
        return keys;
    }

    private List<string> GetQuestKeys()
    {
        string keyData = PlayerPrefs.GetString("QuestSystemSavedKeys", "");
        var keys = new List<string>();
        if (!string.IsNullOrEmpty(keyData))
        {
            keys.AddRange(keyData.Split(';'));
            keys.RemoveAll(x => string.IsNullOrEmpty(x));
        }
        return keys;
    }

    private List<string> GetStatsKeys()
    {
        string keyData = PlayerPrefs.GetString("StatSystemSavedKeys", "");
        var keys = new List<string>();
        if (!string.IsNullOrEmpty(keyData))
        {
            keys.AddRange(keyData.Split(';'));
            keys.RemoveAll(x => string.IsNullOrEmpty(x));
        }
        return keys;
    }

    // ===================== SERVER COMMUNICATION =====================

    private IEnumerator SaveInventoryToServer(string scene, string uiData, string sceneData)
    {
        var payload = new
        {
            world_key = currentWorldKey,
            key = currentCharacterId,
            scene = scene,
            ui_data = uiData,
            scene_data = sceneData
        };

        yield return StartCoroutine(PostToServer("/inventory", payload));
    }

    private IEnumerator LoadInventoryFromServer(string scene, System.Action<string, string> callback)
    {
        string url = $"{apiRoot}/inventory/{currentWorldKey}/{currentCharacterId}/{scene}";
        yield return StartCoroutine(GetFromServer(url, (response) => {
            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    var data = JsonUtility.FromJson<InventoryResponse>(response);
                    callback(data.ui_data, data.scene_data);
                }
                catch
                {
                    callback("", "");
                }
            }
            else
            {
                callback("", "");
            }
        }));
    }

    private IEnumerator SaveQuestToServer(string active, string completed, string failed)
    {
        var payload = new
        {
            world_key = currentWorldKey,
            key = currentCharacterId,
            active_quests = active,
            completed_quests = completed,
            failed_quests = failed
        };

        yield return StartCoroutine(PostToServer("/quests", payload));
    }

    private IEnumerator LoadQuestFromServer(System.Action<string, string, string> callback)
    {
        string url = $"{apiRoot}/quests/{currentWorldKey}/{currentCharacterId}";
        yield return StartCoroutine(GetFromServer(url, (response) => {
            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    var data = JsonUtility.FromJson<QuestResponse>(response);
                    callback(data.active_quests, data.completed_quests, data.failed_quests);
                }
                catch
                {
                    callback("", "", "");
                }
            }
            else
            {
                callback("", "", "");
            }
        }));
    }

    private IEnumerator SaveStatsToServer(string statsJson)
    {
        var payload = new
        {
            world_key = currentWorldKey,
            key = currentCharacterId,
            stats_json = statsJson
        };

        yield return StartCoroutine(PostToServer("/stats", payload));
    }

    private IEnumerator LoadStatsFromServer(System.Action<string> callback)
    {
        string url = $"{apiRoot}/stats/{currentWorldKey}/{currentCharacterId}";
        yield return StartCoroutine(GetFromServer(url, (response) => {
            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    var data = JsonUtility.FromJson<StatsResponse>(response);
                    callback(data.stats_json);
                }
                catch
                {
                    callback("");
                }
            }
            else
            {
                callback("");
            }
        }));
    }

   

    private IEnumerator SaveStringToServer(string key, string value)
    {
        var payload = new
        {
            key = key,
            value = value
        };

        yield return StartCoroutine(PostToServer("/sync/string", payload));
    }

    private IEnumerator LoadStringFromServer(string key, string defaultValue, System.Action<string> callback)
    {
        string url = $"{apiRoot}/sync/string/{key}";
        yield return StartCoroutine(GetFromServer(url, (response) => {
            if (!string.IsNullOrEmpty(response))
            {
                callback(response);
            }
            else
            {
                callback(defaultValue);
            }
        }));
    }

    // ===================== NETWORK HELPERS =====================

    private IEnumerator PostToServer(string endpoint, object payload)
    {
        string url = apiRoot + endpoint;
        string json = JsonUtility.ToJson(payload);

        using (UnityEngine.Networking.UnityWebRequest req = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(currentToken))
                req.SetRequestHeader("Authorization", $"Bearer {currentToken}");

            yield return req.SendWebRequest();

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[HybridBridge] Server save failed: {req.error}");
            }
            else
            {
                Debug.Log($"[HybridBridge] Successfully saved to server: {endpoint}");
            }
        }
    }

    private IEnumerator GetFromServer(string url, System.Action<string> callback)
    {
        using (UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(currentToken))
                req.SetRequestHeader("Authorization", $"Bearer {currentToken}");

            yield return req.SendWebRequest();

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"[HybridBridge] Successfully loaded from server: {url}");
                callback(req.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"[HybridBridge] Server load failed: {req.error}");
                callback("");
            }
        }
    }

    // ===================== RESPONSE CLASSES =====================

    [System.Serializable]
    private class InventoryResponse
    {
        public string ui_data;
        public string scene_data;
    }

    [System.Serializable]
    private class QuestResponse
    {
        public string active_quests;
        public string completed_quests;
        public string failed_quests;
    }

    [System.Serializable]
    private class StatsResponse
    {
        public string stats_json;
    }
}