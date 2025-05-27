using UnityEngine;
using TagDebugSystem;
using DevionGames; // For EventHandler

/// <summary>
/// Static class that provides central tracking of server/world selection state
/// and broadcasts events when these change.
/// </summary>
public static class ServerWorldEvents
{
    private const string TAG = Tags.ServerWorld;

    // Static state tracking
    private static bool _isAuthenticated = false;
    private static string _currentWorldKey = null;
    private static string _currentWorldName = null;
    private static string _currentWorldId = null;

    /// <summary>
    /// Whether the user is currently authenticated with the server
    /// </summary>
    public static bool IsAuthenticated
    {
        get { return _isAuthenticated; }
        private set { _isAuthenticated = value; }
    }

    /// <summary>
    /// The key of the currently selected world (used as save key)
    /// </summary>
    public static string CurrentWorldKey
    {
        get { return _currentWorldKey; }
        private set { _currentWorldKey = value; }
    }

    /// <summary>
    /// The display name of the currently selected world
    /// </summary>
    public static string CurrentWorldName
    {
        get { return _currentWorldName; }
        private set { _currentWorldName = value; }
    }

    /// <summary>
    /// The unique ID of the currently selected world
    /// </summary>
    public static string CurrentWorldId
    {
        get { return _currentWorldId; }
        private set { _currentWorldId = value; }
    }

    /// <summary>
    /// Register event handlers to track authentication and world selection
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Register for auth events
        EventHandler.Register("OnLogin", OnLoginSuccess);
        EventHandler.Register("OnSessionExpired", OnSessionExpired);
        EventHandler.Register("OnLogout", OnLogout);

        // Register for world selection
        EventHandler.Register<string, string>("OnServerSelected", OnServerSelected);

        // Check if we already have a token (maybe persistent from previous run)
        string token = PlayerPrefs.GetString("jwt_token", null);
        _isAuthenticated = !string.IsNullOrEmpty(token);

        // Check if we already have a world key in PlayerPrefs
        _currentWorldKey = PlayerPrefs.GetString("CurrentWorldKey", null);
        _currentWorldName = PlayerPrefs.GetString("CurrentWorldName", null);
        _currentWorldId = PlayerPrefs.GetString("CurrentWorldId", null);

        TD.Info(TAG, $"ServerWorldEvents initialized: " +
                $"Auth={_isAuthenticated}, " +
                $"WorldKey={_currentWorldKey ?? "<none>"}");
    }

    /// <summary>
    /// Called when login is successful
    /// </summary>
    private static void OnLoginSuccess()
    {
        _isAuthenticated = true;
        TD.Info(TAG, "User authenticated successfully");
    }

    /// <summary>
    /// Called when the session expires
    /// </summary>
    private static void OnSessionExpired()
    {
        _isAuthenticated = false;
        TD.Warning(TAG, "Session expired, authentication lost");
    }

    /// <summary>
    /// Called when the user logs out
    /// </summary>
    private static void OnLogout()
    {
        _isAuthenticated = false;
        TD.Info(TAG, "User logged out");
    }

    /// <summary>
    /// Called when a server/world is selected
    /// </summary>
    private static void OnServerSelected(string serverId, string serverName)
    {
        if (_currentWorldId == serverId)
        {
            TD.Verbose(TAG, $"Server selection unchanged: {serverName} ({serverId})");
            return;
        }

        TD.Info(TAG, $"Server selected: {serverName} ({serverId})");

        // Update current values
        _currentWorldId = serverId;
        _currentWorldName = serverName;

        // Use server ID as the world key for saving/loading
        _currentWorldKey = serverId;

        // Save to PlayerPrefs for persistence
        PlayerPrefs.SetString("CurrentWorldKey", _currentWorldKey);
        PlayerPrefs.SetString("CurrentWorldName", _currentWorldName);
        PlayerPrefs.SetString("CurrentWorldId", _currentWorldId);

        // Broadcast the world selection event
        TD.Info(TAG, $"Broadcasting OnWorldSelected({_currentWorldKey})");
        EventHandler.Execute("OnWorldSelected", _currentWorldKey);
    }

    /// <summary>
    /// Called when the user is joining a world. Same behavior as OnServerSelected
    /// but exists to support the FlowManager.cs implementation.
    /// </summary>
    /// <param name="worldName">The name of the world being joined</param>
    public static void OnWorldJoining(string worldName)
    {
        // If we don't have a world ID yet, use the name as both ID and name
        if (string.IsNullOrEmpty(_currentWorldId))
        {
            OnServerSelected(worldName, worldName);
        }
        else if (_currentWorldName != worldName)
        {
            // If we have a world ID but the name doesn't match, update the name
            // but keep the same ID
            TD.Info(TAG, $"Updating world name from '{_currentWorldName}' to '{worldName}'");
            _currentWorldName = worldName;
            PlayerPrefs.SetString("CurrentWorldName", _currentWorldName);

            // Re-broadcast the world selection event
            TD.Info(TAG, $"Broadcasting OnWorldSelected({_currentWorldKey}) after name change");
            EventHandler.Execute("OnWorldSelected", _currentWorldKey);
        }
    }

    /// <summary>
    /// Public method to manually set the current world (for testing/debugging)
    /// </summary>
    public static void SetCurrentWorld(string worldId, string worldName)
    {
        OnServerSelected(worldId, worldName);
    }
}