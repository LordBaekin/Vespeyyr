using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DevionGames.CharacterSystem;
using DevionGames.InventorySystem;
using DevionGames.QuestSystem;
using DevionGames.StatSystem;
using TagDebugSystem;

/// <summary>
/// Unified interceptor that handles server persistence for all DevionGames systems
/// Use this INSTEAD of the three separate interceptors to avoid conflicts
/// </summary>
public class UnifiedDataInterceptor : MonoBehaviour
{
    [Header("System Settings")]
    public bool enableInventory = true;
    public bool enableQuests = true;
    public bool enableStats = true;

    [Header("Auto-Save Settings")]
    public bool autoSaveOnSceneChange = true;
    public bool autoSaveOnQuit = true;
    public float initialLoadDelay = 2f;
    public float autoSaveThrottle = 30f;
    private float lastAutoSaveTime = 0f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private bool isLoading = false;
    private bool isSaving = false;
    private const string TAG = "UnifiedData";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (autoSaveOnSceneChange)
            SceneManager.activeSceneChanged += OnSceneChanged;

        if (autoSaveOnQuit)
            Application.quitting += OnApplicationQuitting;

        if (enableDebugLogs)
            TD.Info(TAG, $"Initialized with Inventory={enableInventory}, Quests={enableQuests}, Stats={enableStats}", this);
    }

    private void OnDestroy()
    {
        if (autoSaveOnSceneChange)
            SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void Start()
    {
        StartCoroutine(DelayedInitialLoad());
    }

    private IEnumerator DelayedInitialLoad()
    {
        yield return new WaitForSeconds(initialLoadDelay);

        string characterId = DevionGamesAdapter.CurrentCharacterId;
        if (string.IsNullOrEmpty(characterId))
        {
            if (enableDebugLogs)
                TD.Warning(TAG, "No character ID available during initial load, retrying...", this);

            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(1f);
                characterId = DevionGamesAdapter.CurrentCharacterId;
                if (!string.IsNullOrEmpty(characterId))
                    break;
            }
        }

        if (!string.IsNullOrEmpty(characterId))
        {
            yield return StartCoroutine(LoadAllData());
        }
        else
        {
            TD.Error(TAG, "Unable to get character ID for initial load", this);
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (autoSaveOnSceneChange && !isSaving && Time.time - lastAutoSaveTime > autoSaveThrottle)
        {
            lastAutoSaveTime = Time.time;
            StartCoroutine(SaveAllData());
        }
    }

    private void OnApplicationQuitting()
    {
        if (autoSaveOnQuit && !isSaving)
            StartCoroutine(SaveAllData());
    }

    [ContextMenu("Force Load All Data")]
    public void ForceLoadAll()
    {
        if (!isLoading)
            StartCoroutine(LoadAllData());
    }

    [ContextMenu("Force Save All Data")]
    public void ForceSaveAll()
    {
        if (!isSaving)
            StartCoroutine(SaveAllData());
    }

    private IEnumerator LoadAllData()
    {
        if (isLoading) yield break;
        isLoading = true;

        string characterId = DevionGamesAdapter.CurrentCharacterId;
        if (string.IsNullOrEmpty(characterId))
        {
            TD.Warning(TAG, "No character ID available for loading", this);
            isLoading = false;
            yield break;
        }

        if (enableDebugLogs)
            TD.Info(TAG, $"Loading all data for character {characterId}", this);

        if (enableStats) yield return StartCoroutine(LoadStatsData(characterId));
        if (enableQuests) yield return StartCoroutine(LoadQuestData(characterId));
        if (enableInventory) yield return StartCoroutine(LoadInventoryData(characterId));

        if (enableDebugLogs)
            TD.Info(TAG, "All data loading completed", this);

        isLoading = false;
    }

    private IEnumerator SaveAllData()
    {
        if (isSaving) yield break;
        isSaving = true;

        string characterId = DevionGamesAdapter.CurrentCharacterId;
        if (string.IsNullOrEmpty(characterId))
        {
            TD.Warning(TAG, "No character ID available for saving", this);
            isSaving = false;
            yield break;
        }

        if (enableDebugLogs)
            TD.Info(TAG, $"Saving all data for character {characterId}", this);

        if (enableStats) yield return StartCoroutine(SaveStatsData(characterId));
        if (enableQuests) yield return StartCoroutine(SaveQuestData(characterId));
        if (enableInventory) yield return StartCoroutine(SaveInventoryData(characterId));

        if (enableDebugLogs)
            TD.Info(TAG, "All data saving completed", this);

        isSaving = false;
    }

    private IEnumerator LoadStatsData(string characterId)
    {
        string statsJson = null;
        bool finished = false;
        bool hasError = false;

        try
        {
            DevionGamesAdapter.LoadStatsData(characterId, (data) => {
                statsJson = data;
                finished = true;
            });
        }
        catch (System.Exception ex)
        {
            TD.Error(TAG, $"Stats load request failed: {ex.Message}", this);
            hasError = true;
        }

        if (!hasError)
        {
            while (!finished) yield return null;

            try
            {
                if (!string.IsNullOrEmpty(statsJson))
                {
                    string key = PlayerPrefs.GetString(StatsManager.SavingLoading.savingKey, StatsManager.SavingLoading.savingKey);
                    PlayerPrefs.SetString(key + ".Stats", statsJson);
                    PlayerPrefs.Save();
                    StatsManager.Load(key);

                    if (enableDebugLogs)
                        TD.Info(TAG, "Stats loaded successfully", this);
                }
                else
                {
                    if (enableDebugLogs)
                        TD.Info(TAG, "No stats data found on server", this);
                }
            }
            catch (System.Exception ex)
            {
                TD.Error(TAG, $"Stats processing failed: {ex.Message}", this);
            }
        }
    }

    private IEnumerator SaveStatsData(string characterId)
    {
        try
        {
            StatsManager.Save();
            string key = PlayerPrefs.GetString(StatsManager.SavingLoading.savingKey, StatsManager.SavingLoading.savingKey);
            string statsJson = PlayerPrefs.GetString(key + ".Stats", "");
            DevionGamesAdapter.SaveStatsData(characterId, statsJson);

            if (enableDebugLogs)
                TD.Info(TAG, "Stats saved successfully", this);
        }
        catch (System.Exception ex)
        {
            TD.Error(TAG, $"Stats save failed: {ex.Message}", this);
        }
        yield break;
    }

    private IEnumerator LoadQuestData(string characterId)
    {
        string activeJson = null, completedJson = null, failedJson = null;
        bool finished = false;
        bool hasError = false;

        try
        {
            DevionGamesAdapter.LoadQuestData(characterId, (active, completed, failed) => {
                activeJson = active;
                completedJson = completed;
                failedJson = failed;
                finished = true;
            });
        }
        catch (System.Exception ex)
        {
            TD.Error(TAG, $"Quest load request failed: {ex.Message}", this);
            hasError = true;
        }

        if (!hasError)
        {
            while (!finished) yield return null;

            try
            {
                if (!string.IsNullOrEmpty(activeJson) || !string.IsNullOrEmpty(completedJson) || !string.IsNullOrEmpty(failedJson))
                {
                    string key = PlayerPrefs.GetString(QuestManager.SavingLoading.savingKey, QuestManager.SavingLoading.savingKey);
                    PlayerPrefs.SetString(key + ".ActiveQuests", activeJson ?? "");
                    PlayerPrefs.SetString(key + ".CompletedQuests", completedJson ?? "");
                    PlayerPrefs.SetString(key + ".FailedQuests", failedJson ?? "");
                    PlayerPrefs.Save();
                    QuestManager.Load(key);

                    if (enableDebugLogs)
                        TD.Info(TAG, "Quests loaded successfully", this);
                }
                else
                {
                    if (enableDebugLogs)
                        TD.Info(TAG, "No quest data found on server", this);
                }
            }
            catch (System.Exception ex)
            {
                TD.Error(TAG, $"Quest processing failed: {ex.Message}", this);
            }
        }
    }

    private IEnumerator SaveQuestData(string characterId)
    {
        try
        {
            QuestManager.Save();
            string key = PlayerPrefs.GetString(QuestManager.SavingLoading.savingKey, QuestManager.SavingLoading.savingKey);
            string activeJson = PlayerPrefs.GetString(key + ".ActiveQuests", "");
            string completedJson = PlayerPrefs.GetString(key + ".CompletedQuests", "");
            string failedJson = PlayerPrefs.GetString(key + ".FailedQuests", "");
            DevionGamesAdapter.SaveQuestData(characterId, activeJson, completedJson, failedJson);

            if (enableDebugLogs)
                TD.Info(TAG, "Quests saved successfully", this);
        }
        catch (System.Exception ex)
        {
            TD.Error(TAG, $"Quest save failed: {ex.Message}", this);
        }
        yield break;
    }

    private IEnumerator LoadInventoryData(string characterId)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string uiJson = null, sceneJson = null;
        bool uiLoaded = false, sceneLoaded = false;
        bool hasError = false;
        float timeout = 10f;
        float startTime = Time.time;

        if (enableDebugLogs)
            TD.Info(TAG, $"Starting inventory load for character {characterId}, scene {sceneName}", this);

        try
        {
            DevionGamesAdapter.LoadInventoryData(characterId, "UI", (ui, _) => {
                uiJson = ui;
                uiLoaded = true;
                if (enableDebugLogs)
                    TD.Info(TAG, $"UI inventory data loaded: {(string.IsNullOrEmpty(ui) ? "EMPTY" : "SUCCESS")}", this);
            });

            DevionGamesAdapter.LoadInventoryData(characterId, sceneName, (_, scene) => {
                sceneJson = scene;
                sceneLoaded = true;
                if (enableDebugLogs)
                    TD.Info(TAG, $"Scene inventory data loaded: {(string.IsNullOrEmpty(scene) ? "EMPTY" : "SUCCESS")}", this);
            });
        }
        catch (System.Exception ex)
        {
            TD.Error(TAG, $"Inventory load request failed: {ex.Message}", this);
            hasError = true;
        }

        if (!hasError)
        {
            while ((!uiLoaded || !sceneLoaded) && (Time.time - startTime < timeout)) yield return null;

            if (Time.time - startTime >= timeout)
            {
                TD.Error(TAG, $"Inventory load timed out after {timeout} seconds", this);
                yield break;
            }

            try
            {
                string key = PlayerPrefs.GetString(InventoryManager.SavingLoading.savingKey, InventoryManager.SavingLoading.savingKey);
                PlayerPrefs.SetString(key + ".UI", uiJson ?? "");
                PlayerPrefs.SetString(key + "." + sceneName, sceneJson ?? "");

                var scenesList = PlayerPrefs.GetString(key + ".Scenes", "").Split(';');
                var scenesSet = new System.Collections.Generic.HashSet<string>(scenesList);
                scenesSet.Add(sceneName);
                scenesSet.Remove("");
                PlayerPrefs.SetString(key + ".Scenes", string.Join(";", scenesSet));
                PlayerPrefs.Save();

                InventoryManager.Load(key);

                if (enableDebugLogs)
                    TD.Info(TAG, "Inventory loaded successfully", this);
            }
            catch (System.Exception ex)
            {
                TD.Error(TAG, $"Inventory processing failed: {ex.Message}", this);
                TD.Error(TAG, $"Stack trace: {ex.StackTrace}", this);
            }
        }
    }

    private IEnumerator SaveInventoryData(string characterId)
    {
        try
        {
            string sceneName = SceneManager.GetActiveScene().name;
            InventoryManager.Save();
            string key = PlayerPrefs.GetString(InventoryManager.SavingLoading.savingKey, InventoryManager.SavingLoading.savingKey);
            string uiJson = PlayerPrefs.GetString(key + ".UI", "");
            string sceneJson = PlayerPrefs.GetString(key + "." + sceneName, "");
            DevionGamesAdapter.SaveInventoryData(characterId, "UI", uiJson, "");
            DevionGamesAdapter.SaveInventoryData(characterId, sceneName, "", sceneJson);

            if (enableDebugLogs)
                TD.Info(TAG, "Inventory saved successfully", this);
        }
        catch (System.Exception ex)
        {
            TD.Error(TAG, $"Inventory save failed: {ex.Message}", this);
        }
        yield break;
    }
}
