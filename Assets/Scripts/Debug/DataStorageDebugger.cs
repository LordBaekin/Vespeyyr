using UnityEngine;
using TagDebugSystem;
using DevionGames.CharacterSystem;

/// <summary>
/// Add this script to a GameObject to debug data storage issues
/// </summary>
public class DataStorageDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    public bool logOnStart = true;
    public bool logContinuously = false;
    public float logInterval = 5f;

    private float lastLogTime;

    void Start()
    {
        if (logOnStart)
        {
            LogCurrentState();
        }
    }

    void Update()
    {
        if (logContinuously && Time.time - lastLogTime > logInterval)
        {
            LogCurrentState();
            lastLogTime = Time.time;
        }
    }

    [ContextMenu("Log Current State")]
    public void LogCurrentState()
    {
        TD.Info("DataStorageDebugger", "=== DATA STORAGE DEBUG ===");

        // 1. JWT Token Check
        string jwtToken = PlayerPrefs.GetString("jwt_token", "");
        TD.Info("DataStorageDebugger", $"JWT Token: {(string.IsNullOrEmpty(jwtToken) ? "❌ MISSING" : "✅ Present (" + jwtToken.Length + " chars)")}");

        // 2. World Context Check  
        string currentWorldKey = ServerWorldEvents.CurrentWorldKey;
        string playerPrefsWorldKey = PlayerPrefs.GetString("CurrentWorldKey", "");
        string selectedServer = PlayerPrefs.GetString("selected_server", "");

        TD.Info("DataStorageDebugger", $"ServerWorldEvents.CurrentWorldKey: {(string.IsNullOrEmpty(currentWorldKey) ? "❌ NULL" : "✅ " + currentWorldKey)}");
        TD.Info("DataStorageDebugger", $"PlayerPrefs CurrentWorldKey: {(string.IsNullOrEmpty(playerPrefsWorldKey) ? "❌ NOT SET" : "✅ " + playerPrefsWorldKey)}");
        TD.Info("DataStorageDebugger", $"PlayerPrefs selected_server: {(string.IsNullOrEmpty(selectedServer) ? "❌ NOT SET" : "✅ " + selectedServer)}");

        // 3. Character Context Check
        string currentCharacterName = PlayerPrefs.GetString("CurrentCharacterName", "");
        string currentCharacterID = PlayerPrefs.GetString("CurrentCharacterID", "");
        string playerName = PlayerPrefs.GetString("Player", "");

        TD.Info("DataStorageDebugger", $"CurrentCharacterName: {(string.IsNullOrEmpty(currentCharacterName) ? "❌ NOT SET" : "✅ " + currentCharacterName)}");
        TD.Info("DataStorageDebugger", $"CurrentCharacterID: {(string.IsNullOrEmpty(currentCharacterID) ? "❌ NOT SET" : "✅ " + currentCharacterID)}");
        TD.Info("DataStorageDebugger", $"Player (legacy): {(string.IsNullOrEmpty(playerName) ? "❌ NOT SET" : "✅ " + playerName)}");

        // 4. HybridPersistenceBridge Check
        var bridge = HybridPersistenceBridge.Instance;
        if (bridge == null)
        {
            TD.Error("DataStorageDebugger", "❌ HybridPersistenceBridge.Instance is NULL");
        }
        else
        {
            TD.Info("DataStorageDebugger", "✅ HybridPersistenceBridge found");
            TD.Info("DataStorageDebugger", $"  - Current World Key: {bridge.currentWorldKey}");
            TD.Info("DataStorageDebugger", $"  - Current Character ID: {bridge.currentCharacterId}");

            if (bridge.providerAsset == null)
            {
                TD.Error("DataStorageDebugger", "  ❌ SaveProviderSelectorSO is NULL - assign it!");
            }
            else
            {
                TD.Info("DataStorageDebugger", $"  ✅ SaveProvider: {bridge.providerAsset.currentProvider}");
            }
        }

        // 5. DevionGamesAdapter Check
        bool isHybridAvailable = DevionGamesAdapter.IsHybridBridgeAvailable();
        var currentProvider = DevionGamesAdapter.GetCurrentProvider();

        TD.Info("DataStorageDebugger", $"DevionGamesAdapter.IsHybridBridgeAvailable: {(isHybridAvailable ? "✅ YES" : "❌ NO")}");
        TD.Info("DataStorageDebugger", $"DevionGamesAdapter.GetCurrentProvider: {currentProvider}");

        TD.Info("DataStorageDebugger", "=== END DEBUG ===");
    }

    [ContextMenu("Test Save/Load")]
    public void TestSaveLoad()
    {
        TD.Info("DataStorageDebugger", "Testing save/load functionality...");

        // Test string save/load
        string testKey = "debug_test_key";
        string testValue = "test_value_" + System.DateTime.Now.Ticks;

        DevionGamesAdapter.SaveString(testKey, testValue);
        TD.Info("DataStorageDebugger", $"Saved test value: {testValue}");

        DevionGamesAdapter.LoadString(testKey, "", (loadedValue) =>
        {
            if (loadedValue == testValue)
            {
                TD.Info("DataStorageDebugger", "✅ Save/Load test PASSED");
            }
            else
            {
                TD.Error("DataStorageDebugger", $"❌ Save/Load test FAILED. Expected: {testValue}, Got: {loadedValue}");
            }
        });
    }
}

