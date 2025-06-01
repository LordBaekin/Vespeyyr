// DevionGamesAdapter.cs with full TD logging added, all methods preserved
using System;
using System.Collections.Generic;
using UnityEngine;
using TagDebugSystem;

namespace DevionGames.CharacterSystem
{
    public static class DevionGamesAdapter
    {
        private static HybridPersistenceBridge Bridge => HybridPersistenceBridge.Instance;
        private const string TAG = "DevionGamesAdapter";

        public static string CurrentCharacterId =>
            Bridge != null ? Bridge.currentCharacterId : PlayerPrefs.GetString("CurrentCharacterID", "");

        public static string CurrentWorldKey =>
            Bridge != null ? Bridge.currentWorldKey : PlayerPrefs.GetString("CurrentWorldKey", "DefaultWorld");

        public static void SaveInventoryData(string key, string scene, string uiData, string sceneData = "")
        {
            TD.Info(TAG, $"Saving inventory for key={key}, scene={scene}");
            if (ShouldUseHybridBridge())
            {
                SetBridgeContext();
                if (scene == "UI") Bridge.SaveInventoryData("UI", uiData, "");
                else if (scene == "Scenes") Bridge.SaveInventoryData("Scenes", uiData, "");
                else Bridge.SaveInventoryData(scene, uiData, sceneData);
            }
            else
            {
                if (scene == "UI") PlayerPrefs.SetString($"{key}.UI", uiData);
                else if (scene == "Scenes") PlayerPrefs.SetString($"{key}.Scenes", uiData);
                else
                {
                    PlayerPrefs.SetString($"{key}.{scene}", uiData);
                    if (!string.IsNullOrEmpty(sceneData))
                        PlayerPrefs.SetString($"{key}.{scene}_scene", sceneData);
                }
                PlayerPrefs.Save();
            }
        }


        public static void ClearCharacterContext()
        {
            TD.Info("DevionGamesAdapter", "[ClearCharacterContext] Clearing character-related session data.");
            PlayerPrefs.DeleteKey("selected_character");
            // Clear any custom cached character keys if needed
        }

        public static void ClearWorldContext()
        {
            TD.Info("DevionGamesAdapter", "[ClearWorldContext] Clearing world context.");
            PlayerPrefs.DeleteKey("selected_server");
            PlayerPrefs.DeleteKey("selected_server_name");
            ServerWorldEvents.SetCurrentWorld("", "");
        }



        public static void LoadInventoryData(string key, string scene, Action<string, string> callback)
        {
            TD.Info(TAG, $"Loading inventory for key={key}, scene={scene}");
            if (ShouldUseHybridBridge())
            {
                SetBridgeContext();
                Bridge.LoadInventoryData(scene, callback);
            }
            else
            {
                string uiData = "", sceneData = "";
                if (scene == "UI") uiData = PlayerPrefs.GetString($"{key}.UI", "");
                else if (scene == "Scenes") uiData = PlayerPrefs.GetString($"{key}.Scenes", "");
                else
                {
                    uiData = PlayerPrefs.GetString($"{key}.{scene}", "");
                    sceneData = PlayerPrefs.GetString($"{key}.{scene}_scene", "");
                }
                callback(uiData, sceneData);
            }
        }

        public static void LoadInventorySystemKeys(Action<List<string>> callback)
        {
            if (ShouldUseHybridBridge())
                Bridge.LoadString("InventorySystemSavedKeys", "", data => callback(ParseKeyData(data)));
            else
                callback(ParseKeyData(PlayerPrefs.GetString("InventorySystemSavedKeys", "")));
        }

        public static void SaveInventorySystemKeys(List<string> keys)
        {
            var data = string.Join(";", keys);
            if (ShouldUseHybridBridge()) Bridge.SaveString("InventorySystemSavedKeys", data);
            else { PlayerPrefs.SetString("InventorySystemSavedKeys", data); PlayerPrefs.Save(); }
        }

        public static void SaveString(string key, string value)
        {
            TD.Info(TAG, $"Saving string key={key}");
            if (ShouldUseHybridBridge()) { SetBridgeContext(); Bridge.SaveString(key, value); }
            else { PlayerPrefs.SetString(key, value); PlayerPrefs.Save(); }
        }

        public static void LoadString(string key, string defaultValue, Action<string> callback)
        {
            TD.Info(TAG, $"Loading string key={key}");
            if (ShouldUseHybridBridge()) { SetBridgeContext(); Bridge.LoadString(key, defaultValue, callback); }
            else callback(PlayerPrefs.GetString(key, defaultValue));
        }

        public static void SaveQuestData(string key, string activeQuests, string completedQuests, string failedQuests)
        {
            TD.Info(TAG, $"Saving quests for key={key}");
            if (ShouldUseHybridBridge()) { SetBridgeContext(); Bridge.SaveQuestData(activeQuests, completedQuests, failedQuests); }
            else
            {
                PlayerPrefs.SetString($"{key}.ActiveQuests", activeQuests);
                PlayerPrefs.SetString($"{key}.CompletedQuests", completedQuests);
                PlayerPrefs.SetString($"{key}.FailedQuests", failedQuests);
                PlayerPrefs.Save();
            }
        }

        public static void LoadQuestData(string key, Action<string, string, string> callback)
        {
            TD.Info(TAG, $"Loading quests for key={key}");
            if (ShouldUseHybridBridge()) { SetBridgeContext(); Bridge.LoadQuestData(callback); }
            else
            {
                var a = PlayerPrefs.GetString($"{key}.ActiveQuests", "");
                var c = PlayerPrefs.GetString($"{key}.CompletedQuests", "");
                var f = PlayerPrefs.GetString($"{key}.FailedQuests", "");
                callback(a, c, f);
            }
        }

        public static void LoadQuestSystemKeys(Action<List<string>> callback)
        {
            if (ShouldUseHybridBridge()) Bridge.LoadString("QuestSystemSavedKeys", "", data => callback(ParseKeyData(data)));
            else callback(ParseKeyData(PlayerPrefs.GetString("QuestSystemSavedKeys", "")));
        }

        public static void SaveQuestSystemKeys(List<string> keys)
        {
            var data = string.Join(";", keys);
            if (ShouldUseHybridBridge()) Bridge.SaveString("QuestSystemSavedKeys", data);
            else { PlayerPrefs.SetString("QuestSystemSavedKeys", data); PlayerPrefs.Save(); }
        }

        public static void SaveStatsData(string key, string statsJson)
        {
            TD.Info(TAG, $"Saving stats for key={key}");
            if (ShouldUseHybridBridge()) { SetBridgeContext(); Bridge.SaveStatsData(statsJson); }
            else { PlayerPrefs.SetString($"{key}.Stats", statsJson); PlayerPrefs.Save(); }
        }

        public static void LoadStatsData(string key, Action<string> callback)
        {
            TD.Info(TAG, $"Loading stats for key={key}");
            if (ShouldUseHybridBridge()) { SetBridgeContext(); Bridge.LoadStatsData(callback); }
            else callback(PlayerPrefs.GetString($"{key}.Stats", ""));
        }

        public static void LoadStatsSystemKeys(Action<List<string>> callback)
        {
            if (ShouldUseHybridBridge()) Bridge.LoadString("StatSystemSavedKeys", "", data => callback(ParseKeyData(data)));
            else callback(ParseKeyData(PlayerPrefs.GetString("StatSystemSavedKeys", "")));
        }

        public static void SaveStatsSystemKeys(List<string> keys)
        {
            var data = string.Join(";", keys);
            if (ShouldUseHybridBridge()) Bridge.SaveString("StatSystemSavedKeys", data);
            else { PlayerPrefs.SetString("StatSystemSavedKeys", data); PlayerPrefs.Save(); }
        }

        public static void SaveCharacterData(string characterJson)
        {
            TD.Info(TAG, $"Saving character data");
            if (ShouldUseHybridBridge()) { SetBridgeContext(); Bridge.SaveCharacterData(characterJson); }
            else
            {
                var account = PlayerPrefs.GetString("Account", "Player");
                PlayerPrefs.SetString(account, characterJson);
                PlayerPrefs.Save();
            }
        }

        public static void LoadCharacterData(Action<string> callback)
        {
            TD.Info(TAG, $"Loading character data");
            if (ShouldUseHybridBridge()) { SetBridgeContext(); Bridge.LoadCharacterData(callback); }
            else
            {
                var account = PlayerPrefs.GetString("Account", "Player");
                callback(PlayerPrefs.GetString(account, ""));
            }
        }

        private static void SetBridgeContext()
        {
            if (Bridge == null) return;
            Bridge.SetContext(CurrentWorldKey, CurrentCharacterId);
        }

        private static bool ShouldUseHybridBridge()
        {
            return Bridge != null && Bridge.providerAsset != null &&
                   Bridge.providerAsset.currentProvider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs;
        }

        private static List<string> ParseKeyData(string keyData)
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(keyData))
            {
                list.AddRange(keyData.Split(';'));
                list.RemoveAll(string.IsNullOrEmpty);
            }
            return list;
        }

        public static void SetCharacterContext(string characterId, string characterName)
        {
            TD.Info(TAG, $"SetCharacterContext(ID='{characterId}', Name='{characterName}')");
            if (Bridge == null) return;

            if (string.IsNullOrEmpty(characterId)) { TD.Error(TAG, "CharacterId is null or empty!"); return; }
            if (string.IsNullOrEmpty(characterName))
            {
                TD.Warning(TAG, "CharacterName empty, using ID fallback");
                characterName = characterId;
            }

            PlayerPrefs.SetString("CurrentCharacterID", characterId);
            PlayerPrefs.SetString("CurrentCharacterName", characterName);
            PlayerPrefs.Save();

            Bridge.SetContext(CurrentWorldKey, characterId);
            TD.Info(TAG, $"Character context set: World={CurrentWorldKey}, CharacterID={characterId}, CharacterName={characterName}");
        }

        public static void SetCharacterContext(string characterIdOrName)
        {
            TD.Warning(TAG, $"Using single value for both ID and Name: {characterIdOrName}");
            SetCharacterContext(characterIdOrName, characterIdOrName);
        }

        public static void SetWorldContext(string worldKey)
        {
            if (Bridge == null) return;
            PlayerPrefs.SetString("CurrentWorldKey", worldKey);
            PlayerPrefs.Save();
            Bridge.SetContext(worldKey, CurrentCharacterId);
            TD.Info(TAG, $"World context set: World={worldKey}, Character={CurrentCharacterId}");
        }

        public static void SetAuthToken(string token)
        {
            Bridge?.SetAuthToken(token);
            TD.Info(TAG, "Auth token set for hybrid bridge");
        }

        public static bool IsHybridBridgeAvailable() => Bridge != null && Bridge.providerAsset != null;

        public static SaveProviderSelectorSO.SaveProvider GetCurrentProvider() =>
            Bridge?.providerAsset?.currentProvider ?? SaveProviderSelectorSO.SaveProvider.PlayerPrefs;
    }
}
