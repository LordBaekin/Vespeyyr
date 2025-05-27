using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;
using System;
using System.Text;
using TagDebugSystem;

// Alias DevionGames.EventHandler to avoid ambiguity with System.EventHandler
using EventHandler = DevionGames.EventHandler;
using DevionGames.CharacterSystem;  // For Character type

/// <summary>
/// Base class for all managers that need to be aware of server authentication,
/// world selection, two-phase data loading, and user preferences.
/// </summary>
public abstract class ServerAwareManager : MonoBehaviour
{
    // ---- Singleton ----
    public static ServerAwareManager Instance { get; private set; }

    [Header("Persistence")]
    [Tooltip("Automatically set to DontDestroyOnLoad if true")]
    [SerializeField] protected bool dontDestroyOnLoad = true;

    // Base URL for API endpoints, can be overridden via PlayerPrefs
    protected static string serverBaseUrl = "http://localhost:5000/";

    // State tracking
    protected bool isLoggedIn = false;
    protected bool isWorldSelected = false;
    protected bool isCharacterSelected = false;
    protected string currentWorldKey = null;
    protected string currentCharacterName = null;

    // Loading mode for different phases
    protected enum DataLoadingMode { None, PreviewOnly, Complete }
    protected DataLoadingMode currentLoadingMode = DataLoadingMode.None;

    // Flag to track if server persistence is enabled
    protected bool useServerPersistence = false;

    // Flag to track if auto-save is running
    protected bool IsAutoSaveRunning { get; set; } = false;

    // Instance‐level tag (for instance methods)
    protected virtual string TAG => "ServerAwareManager";

    // Static tag (for static methods)
    private const string STATIC_TAG = "ServerAwareManager";

    #region Unity Lifecycle & Singleton

    protected virtual void Awake()
    {
        // Enforce singleton
        if (Instance == null)
        {
            Instance = this;
            if (dontDestroyOnLoad)
            {
                if (transform.parent != null)
                {
                    TD.Verbose(TAG, "Unparenting for DontDestroyOnLoad.", this);
                    transform.parent = null;
                }
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Load any overridden base-URL
        string savedUrl = PlayerPrefs.GetString("ServerBaseUrl", null);
        if (!string.IsNullOrEmpty(savedUrl))
        {
            serverBaseUrl = savedUrl;
            TD.Verbose(TAG, $"Server URL from PlayerPrefs: {serverBaseUrl}", this);
        }

        // Initialize state
        isLoggedIn = ServerWorldEvents.IsAuthenticated;
        currentWorldKey = ServerWorldEvents.CurrentWorldKey;
        isWorldSelected = !string.IsNullOrEmpty(currentWorldKey);
        useServerPersistence = !string.IsNullOrEmpty(GetAuthToken());

        RegisterEventHandlers();
        TD.Info(TAG, $"Initialized. Auth={isLoggedIn}, World={currentWorldKey}, Persistence={useServerPersistence}", this);
        if (isLoggedIn && isWorldSelected)
            LoadForWorld(currentWorldKey, DataLoadingMode.PreviewOnly);
    }

    protected virtual void OnDestroy()
    {
        UnregisterEventHandlers();
    }

    #endregion

    #region Event Wiring

    protected virtual void RegisterEventHandlers()
    {
        EventHandler.Register("OnLogin", OnLoginReady);
        EventHandler.Register<string>("OnWorldSelected", OnWorldSelected);
        EventHandler.Register<Character>("OnCharacterSelected", OnCharacterSelected);
        EventHandler.Register("OnSessionExpired", OnSessionExpired);
        TD.Verbose(TAG, "Registered lifecycle events", this);
    }

    protected virtual void UnregisterEventHandlers()
    {
        EventHandler.Unregister("OnLogin", OnLoginReady);
        EventHandler.Unregister<string>("OnWorldSelected", OnWorldSelected);
        EventHandler.Unregister<Character>("OnCharacterSelected", OnCharacterSelected);
        EventHandler.Unregister("OnSessionExpired", OnSessionExpired);
        TD.Verbose(TAG, "Unregistered lifecycle events", this);
    }

    protected virtual void OnLoginReady()
    {
        TD.Info(TAG, "OnLoginReady: authentication complete", this);
        isLoggedIn = true;
        useServerPersistence = true;
        if (isWorldSelected)
            LoadForWorld(currentWorldKey, DataLoadingMode.PreviewOnly);
    }

    protected virtual void OnWorldSelected(string worldKey)
    {
        currentWorldKey = worldKey;
        isWorldSelected = true;
        TD.Info(TAG, $"World selected: {worldKey}", this);
        if (isLoggedIn)
            LoadForWorld(worldKey, DataLoadingMode.PreviewOnly);
    }

    protected virtual void OnCharacterSelected(Character character)
    {
        if (character == null) return;
        currentCharacterName = character.CharacterName;
        isCharacterSelected = true;
        TD.Info(TAG, $"Character selected: {character.CharacterName}", this);
        if (isLoggedIn && isWorldSelected)
            LoadForCharacter(character, DataLoadingMode.Complete);
    }

    protected virtual void OnSessionExpired()
    {
        useServerPersistence = false;
        TD.Warning(TAG, "Session expired; switching to local storage", this);
    }

    #endregion

    #region Auth Helpers

    protected static string GetAuthToken()
        => PlayerPrefs.GetString("jwt_token", null);

    protected static void SetAuthHeader(UnityWebRequest request)
    {
        var token = GetAuthToken();
        if (!string.IsNullOrEmpty(token))
            request.SetRequestHeader("Authorization", "Bearer " + token);
    }

    #endregion

    #region Preferences API Integration

    [Serializable]
    public class UserPreferences
    {
        [SerializeField] private bool rememberMe;
        [SerializeField] private bool autoLoginServer;
        [SerializeField] private bool autoLoginCharacter;
        [SerializeField] private string lastServerId;
        [SerializeField] private string lastCharacterName;

        // Public Pascal-case accessors
        public bool RememberMe { get => rememberMe; set => rememberMe = value; }
        public bool AutoLoginServer { get => autoLoginServer; set => autoLoginServer = value; }
        public bool AutoLoginCharacter { get => autoLoginCharacter; set => autoLoginCharacter = value; }
        public string LastServerId { get => lastServerId; set => lastServerId = value; }
        public string LastCharacterName { get => lastCharacterName; set => lastCharacterName = value; }
    }

    /// <summary>GET /api/user/preferences</summary>
    public async Task<UserPreferences> LoadPreferencesAsync()
    {
        var url = serverBaseUrl + "api/user/preferences";
        using (var req = UnityWebRequest.Get(url))
        {
            SetAuthHeader(req);
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            if (req.result != UnityWebRequest.Result.Success)
            {
                TD.Error(TAG, $"LoadPreferences failed: {req.error}", this);
                throw new Exception(req.error);
            }
            return JsonUtility.FromJson<UserPreferences>(req.downloadHandler.text);
        }
    }

    /// <summary>PUT /api/user/preferences</summary>
    public async Task UpdatePreferencesAsync(UserPreferences prefs)
    {
        var url = serverBaseUrl + "api/user/preferences";
        var body = JsonUtility.ToJson(prefs);
        using (var req = new UnityWebRequest(url, "PUT"))
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            SetAuthHeader(req);

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            if (req.result != UnityWebRequest.Result.Success)
            {
                TD.Error(TAG, $"UpdatePreferences failed: {req.error}", this);
                throw new Exception(req.error);
            }
        }
    }

    /// <summary>POST /api/user/preferences/last (lastServerId)</summary>
    public async Task UpdateLastServerAsync(string serverId)
    {
        var url = serverBaseUrl + "api/user/preferences/last";
        var body = JsonUtility.ToJson(new { lastServerId = serverId });
        using (var req = new UnityWebRequest(url, "POST"))
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            SetAuthHeader(req);

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            if (req.result != UnityWebRequest.Result.Success)
            {
                TD.Error(TAG, $"UpdateLastServer failed: {req.error}", this);
                throw new Exception(req.error);
            }
        }
    }

    /// <summary>POST /api/user/preferences/last (lastCharacterName)</summary>
    public async Task UpdateLastCharacterAsync(string characterName)
    {
        var url = serverBaseUrl + "api/user/preferences/last";
        var body = JsonUtility.ToJson(new { lastCharacterName = characterName });
        using (var req = new UnityWebRequest(url, "POST"))
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            SetAuthHeader(req);

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            if (req.result != UnityWebRequest.Result.Success)
            {
                TD.Error(TAG, $"UpdateLastCharacter failed: {req.error}", this);
                throw new Exception(req.error);
            }
        }
    }

    #endregion

    #region Abstract Data Loading

    /// <summary>Load minimal preview data for a world.</summary>
    protected abstract void LoadForWorld(string worldKey, DataLoadingMode mode);

    /// <summary>Load full data for a character.</summary>
    protected abstract void LoadForCharacter(Character character, DataLoadingMode mode);

    #endregion

    #region Auth Error Handling

    /// <summary>
    /// If you get a 401, fire the OnAuthTokenExpired event and retry once.
    /// </summary>
    protected static IEnumerator HandleAuthError(UnityWebRequest www, Action retryAction)
    {
        if (www.responseCode == 401)
        {
            TD.Warning(STATIC_TAG, "Token expired (401). Attempting refresh.");
            EventHandler.Execute("OnAuthTokenExpired");
            float timer = 0f, timeout = 5f;
            bool refreshed = false;
            while (timer < timeout && !refreshed)
            {
                if (PlayerPrefs.GetString("jwt_token_refreshed", "false") == "true")
                {
                    PlayerPrefs.SetString("jwt_token_refreshed", "false");
                    refreshed = true;
                    TD.Info(STATIC_TAG, "Token was refreshed.");
                }
                timer += Time.deltaTime;
                yield return null;
            }
            if (refreshed && retryAction != null)
            {
                TD.Info(STATIC_TAG, "Retrying request with refreshed token.");
                retryAction();
            }
            else
            {
                TD.Warning(STATIC_TAG, "Refresh failed or timed out.");
                EventHandler.Execute("OnSessionExpired");
            }
        }
    }

    #endregion
}
