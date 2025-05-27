using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DevionGames.CharacterSystem;

/// <summary>
/// DevionGamesAdapter integrated with your existing FlowManager and ServerWorldEvents
/// This preserves all your existing architecture while adding hybrid persistence
/// </summary>
public static class DevionGamesAdapter
{
    private static HybridPersistenceBridge Bridge => HybridPersistenceBridge.Instance;

    // ===================== INVENTORY SYSTEM ADAPTER =====================

    public static void SaveInventoryData(string key, string scene, string uiData, string sceneData = "")
    {
        if (ShouldUseHybridBridge())
        {
            // Use the bridge with proper character context
            SetBridgeContext();

            if (scene == "UI")
            {
                Bridge.SaveInventoryData("UI", uiData, "");
            }
            else if (scene == "Scenes")
            {
                Bridge.SaveInventoryData("Scenes", uiData, "");
            }
            else
            {
                Bridge.SaveInventoryData(scene, uiData, sceneData);
            }
        }
        else
        {
            // Fallback to original PlayerPrefs method - your existing logic
            if (scene == "UI")
            {
                PlayerPrefs.SetString($"{key}.UI", uiData);
            }
            else if (scene == "Scenes")
            {
                PlayerPrefs.SetString($"{key}.Scenes", uiData);
            }
            else
            {
                PlayerPrefs.SetString($"{key}.{scene}", uiData);
                if (!string.IsNullOrEmpty(sceneData))
                    PlayerPrefs.SetString($"{key}.{scene}_scene", sceneData);
            }
            PlayerPrefs.Save();
        }
    }

    public static void LoadInventoryData(string key, string scene, System.Action<string, string> callback)
    {
        if (ShouldUseHybridBridge())
        {
            SetBridgeContext();
            Bridge.LoadInventoryData(scene, callback);
        }
        else
        {
            // Fallback to original PlayerPrefs method
            string uiData = "";
            string sceneData = "";

            if (scene == "UI")
            {
                uiData = PlayerPrefs.GetString($"{key}.UI", "");
            }
            else if (scene == "Scenes")
            {
                uiData = PlayerPrefs.GetString($"{key}.Scenes", "");
            }
            else
            {
                uiData = PlayerPrefs.GetString($"{key}.{scene}", "");
                sceneData = PlayerPrefs.GetString($"{key}.{scene}_scene", "");
            }

            callback(uiData, sceneData);
        }
    }

    public static void LoadInventorySystemKeys(System.Action<List<string>> callback)
    {
        if (ShouldUseHybridBridge())
        {
            Bridge.LoadString("InventorySystemSavedKeys", "", (keyData) => {
                List<string> keys = ParseKeyData(keyData);
                callback(keys);
            });
        }
        else
        {
            string keyData = PlayerPrefs.GetString("InventorySystemSavedKeys", "");
            List<string> keys = ParseKeyData(keyData);
            callback(keys);
        }
    }

    public static void SaveInventorySystemKeys(List<string> keys)
    {
        string keyData = string.Join(";", keys);

        if (ShouldUseHybridBridge())
        {
            Bridge.SaveString("InventorySystemSavedKeys", keyData);
        }
        else
        {
            PlayerPrefs.SetString("InventorySystemSavedKeys", keyData);
            PlayerPrefs.Save();
        }
    }

    // ===================== QUEST SYSTEM ADAPTER =====================

    public static void SaveQuestData(string key, string activeQuests, string completedQuests, string failedQuests)
    {
        if (ShouldUseHybridBridge())
        {
            SetBridgeContext();
            Bridge.SaveQuestData(activeQuests, completedQuests, failedQuests);
        }
        else
        {
            // Original PlayerPrefs method
            PlayerPrefs.SetString($"{key}.ActiveQuests", activeQuests);
            PlayerPrefs.SetString($"{key}.CompletedQuests", completedQuests);
            PlayerPrefs.SetString($"{key}.FailedQuests", failedQuests);
            PlayerPrefs.Save();
        }
    }

    public static void LoadQuestData(string key, System.Action<string, string, string> callback)
    {
        if (ShouldUseHybridBridge())
        {
            SetBridgeContext();
            Bridge.LoadQuestData(callback);
        }
        else
        {
            // Original PlayerPrefs method
            string active = PlayerPrefs.GetString($"{key}.ActiveQuests", "");
            string completed = PlayerPrefs.GetString($"{key}.CompletedQuests", "");
            string failed = PlayerPrefs.GetString($"{key}.FailedQuests", "");
            callback(active, completed, failed);
        }
    }

    public static void LoadQuestSystemKeys(System.Action<List<string>> callback)
    {
        if (ShouldUseHybridBridge())
        {
            Bridge.LoadString("QuestSystemSavedKeys", "", (keyData) => {
                List<string> keys = ParseKeyData(keyData);
                callback(keys);
            });
        }
        else
        {
            string keyData = PlayerPrefs.GetString("QuestSystemSavedKeys", "");
            List<string> keys = ParseKeyData(keyData);
            callback(keys);
        }
    }

    public static void SaveQuestSystemKeys(List<string> keys)
    {
        string keyData = string.Join(";", keys);

        if (ShouldUseHybridBridge())
        {
            Bridge.SaveString("QuestSystemSavedKeys", keyData);
        }
        else
        {
            PlayerPrefs.SetString("QuestSystemSavedKeys", keyData);
            PlayerPrefs.Save();
        }
    }

    // ===================== STATS SYSTEM ADAPTER =====================

    public static void SaveStatsData(string key, string statsJson)
    {
        if (ShouldUseHybridBridge())
        {
            SetBridgeContext();
            Bridge.SaveStatsData(statsJson);
        }
        else
        {
            // Original PlayerPrefs method
            PlayerPrefs.SetString($"{key}.Stats", statsJson);
            PlayerPrefs.Save();
        }
    }

    public static void LoadStatsData(string key, System.Action<string> callback)
    {
        if (ShouldUseHybridBridge())
        {
            SetBridgeContext();
            Bridge.LoadStatsData(callback);
        }
        else
        {
            // Original PlayerPrefs method
            string stats = PlayerPrefs.GetString($"{key}.Stats", "");
            callback(stats);
        }
    }

    public static void LoadStatsSystemKeys(System.Action<List<string>> callback)
    {
        if (ShouldUseHybridBridge())
        {
            Bridge.LoadString("StatSystemSavedKeys", "", (keyData) => {
                List<string> keys = ParseKeyData(keyData);
                callback(keys);
            });
        }
        else
        {
            string keyData = PlayerPrefs.GetString("StatSystemSavedKeys", "");
            List<string> keys = ParseKeyData(keyData);
            callback(keys);
        }
    }

    public static void SaveStatsSystemKeys(List<string> keys)
    {
        string keyData = string.Join(";", keys);

        if (ShouldUseHybridBridge())
        {
            Bridge.SaveString("StatSystemSavedKeys", keyData);
        }
        else
        {
            PlayerPrefs.SetString("StatSystemSavedKeys", keyData);
            PlayerPrefs.Save();
        }
    }

    // ===================== CHARACTER SYSTEM ADAPTER =====================

    public static void SaveCharacterData(string characterJson)
    {
        if (ShouldUseHybridBridge())
        {
            SetBridgeContext();
            Bridge.SaveCharacterData(characterJson);
        }
        else
        {
            // Original PlayerPrefs method
            string accountKey = PlayerPrefs.GetString("Account", "Player");
            PlayerPrefs.SetString(accountKey, characterJson);
            PlayerPrefs.Save();
        }
    }

    public static void LoadCharacterData(System.Action<string> callback)
    {
        if (ShouldUseHybridBridge())
        {
            SetBridgeContext();
            Bridge.LoadCharacterData(callback);
        }
        else
        {
            // Original PlayerPrefs method
            string accountKey = PlayerPrefs.GetString("Account", "Player");
            string data = PlayerPrefs.GetString(accountKey, "");
            callback(data);
        }
    }

    // ===================== INTEGRATION WITH YOUR EXISTING SYSTEMS =====================

    /// <summary>
    /// Sets the bridge context using your existing ServerWorldEvents and character data
    /// </summary>
    private static void SetBridgeContext()
    {
        if (Bridge == null) return;

        // Use your ServerWorldEvents for world context
        string worldKey = "DefaultWorld"; // Default fallback

        // Try to get world key from your systems
        try
        {
            if (ServerWorldEventsInterface.CurrentWorldKey != null)
                worldKey = ServerWorldEventsInterface.CurrentWorldKey;
        }
        catch
        {
            // Fallback if ServerWorldEventsInterface isn't available
            worldKey = PlayerPrefs.GetString("CurrentWorldKey", "DefaultWorld");
        }

        // Get character context
        string characterId = PlayerPrefs.GetString("CurrentCharacterID", "");
        if (string.IsNullOrEmpty(characterId))
        {
            characterId = PlayerPrefs.GetString("Player", "DefaultPlayer");
        }

        Bridge.SetContext(worldKey, characterId);
    }

    /// <summary>
    /// Checks if we should use the hybrid bridge
    /// </summary>
    private static bool ShouldUseHybridBridge()
    {
        return Bridge != null &&
               Bridge.providerAsset != null &&
               Bridge.providerAsset.currentProvider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs;
    }

    /// <summary>
    /// Helper to parse key data strings
    /// </summary>
    private static List<string> ParseKeyData(string keyData)
    {
        List<string> keys = new List<string>();
        if (!string.IsNullOrEmpty(keyData))
        {
            keys.AddRange(keyData.Split(';'));
            keys.RemoveAll(x => string.IsNullOrEmpty(x));
        }
        return keys;
    }

    // ===================== PUBLIC API FOR YOUR FLOW MANAGER =====================

    
    /// <summary>
    /// Set character context with both ID and name explicitly
    /// </summary>
    public static void SetCharacterContext(string characterId, string characterName)
    {

        Debug.Log($"[DevionGamesAdapter] SetCharacterContext called with: ID='{characterId}', Name='{characterName}'");
        if (Bridge != null)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogError("[DevionGamesAdapter] CharacterId is null or empty!");
                return;
            }

            if (string.IsNullOrEmpty(characterName))
            {
                Debug.LogWarning("[DevionGamesAdapter] CharacterName is null or empty, using characterId as fallback");
                characterName = characterId;
            }

            // Store both properly
            PlayerPrefs.SetString("CurrentCharacterID", characterId);
            PlayerPrefs.SetString("CurrentCharacterName", characterName);
            PlayerPrefs.Save();

            // Update bridge context with the ID
            string worldKey = GetCurrentWorldKey();
            Bridge.SetContext(worldKey, characterId);

            Debug.Log($"[DevionGamesAdapter] Character context set: World={worldKey}, CharacterID={characterId}, CharacterName={characterName}");
        }
    }

    /// <summary>
    /// Overload that accepts just one value (backwards compatibility)
    /// </summary>
    public static void SetCharacterContext(string characterIdOrName)
    {
        // For now, use the same value for both (not ideal but works)
        Debug.LogWarning($"[DevionGamesAdapter] Using single value for both ID and name: {characterIdOrName}");
        SetCharacterContext(characterIdOrName, characterIdOrName);
    }

    private static string GetCurrentWorldKey()
    {
        try
        {
            if (ServerWorldEventsInterface.CurrentWorldKey != null)
                return ServerWorldEventsInterface.CurrentWorldKey;
        }
        catch
        {
            // Fallback to PlayerPrefs
        }
        return PlayerPrefs.GetString("CurrentWorldKey", "DefaultWorld");
    }

    /// <summary>
    /// Call this from FlowManager when world is selected
    /// </summary>
    public static void SetWorldContext(string worldKey)
    {
        if (Bridge != null)
        {
            string characterId = PlayerPrefs.GetString("CurrentCharacterID", "");
            if (string.IsNullOrEmpty(characterId))
            {
                characterId = PlayerPrefs.GetString("Player", "DefaultPlayer");
            }

            Bridge.SetContext(worldKey, characterId);

            Debug.Log($"[DevionGamesAdapter] World context set: World={worldKey}, Character={characterId}");
        }
    }

    /// <summary>
    /// Call this from FlowManager after login
    /// </summary>
    public static void SetAuthToken(string token)
    {
        if (Bridge != null)
        {
            Bridge.SetAuthToken(token);
            Debug.Log($"[DevionGamesAdapter] Auth token set for hybrid bridge");
        }
    }

    /// <summary>
    /// Check if the hybrid bridge is available and configured
    /// </summary>
    public static bool IsHybridBridgeAvailable()
    {
        return Bridge != null && Bridge.providerAsset != null;
    }

    /// <summary>
    /// Get the current save provider
    /// </summary>
    public static SaveProviderSelectorSO.SaveProvider GetCurrentProvider()
    {
        if (Bridge != null && Bridge.providerAsset != null)
            return Bridge.providerAsset.currentProvider;
        return SaveProviderSelectorSO.SaveProvider.PlayerPrefs;
    }


    public static void SaveString(string key, string value)
    {
        if (ShouldUseHybridBridge())
        {
            Bridge.SaveString(key, value);
        }
        else
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }
    }

    public static void LoadString(string key, string defaultValue, System.Action<string> callback)
    {
        if (ShouldUseHybridBridge())
        {
            Bridge.LoadString(key, defaultValue, callback);
        }
        else
        {
            string value = PlayerPrefs.GetString(key, defaultValue);
            callback(value);
        }
    }









}